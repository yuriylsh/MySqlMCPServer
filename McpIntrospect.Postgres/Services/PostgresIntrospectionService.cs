using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using McpIntrospect.Core.Models;
using McpIntrospect.Core.Services;

namespace McpIntrospect.Postgres.Services;

public class PostgresIntrospectionService(IConfiguration configuration, ILogger<PostgresIntrospectionService> logger) : IMcpIntrospectionService
{
    private readonly ILogger<PostgresIntrospectionService> _logger = logger;
    private readonly IConfiguration _configuration = configuration;

    private async Task<NpgsqlConnection> GetConnectionAsync()
    {
        try {
            var connectionString = _configuration["MCP_Postgres_ConnectionString"];
            if (string.IsNullOrWhiteSpace(connectionString)) throw new Exception();
            var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await connection.ExecuteAsync("SET default_transaction_read_only = on");
            return connection;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Failed to create PostgreSQL connection with the provided connection string.");
            throw new InvalidOperationException("PostgreSQL connection string not configured. Set it as `MCP_Postgres_ConnectionString` environment variable or appsettings.json value.");
        }
    }

    public async Task<IEnumerable<SchemaInfo>> ListSchemasAsync()
    {
        await using var connection = await GetConnectionAsync();

        var schemas = await connection.QueryAsync<SchemaInfo>("""
                  SELECT
                      s.schema_name as Name,
                      CASE WHEN s.schema_name IN ('information_schema', 'pg_catalog', 'pg_toast')
                       THEN 1 ELSE 0 END as IsSystem,
                      COUNT(CASE WHEN t.table_type = 'BASE TABLE' THEN 1 END) AS TableCount,
                      COUNT(CASE WHEN t.table_type = 'VIEW' THEN 1 END) AS ViewCount
                  FROM
                      information_schema.schemata s
                  LEFT JOIN
                      information_schema.tables t ON s.schema_name = t.table_schema
                  GROUP BY
                      s.schema_name
                  ORDER BY
                      s.schema_name;
                  """);
        return schemas.AsList();
    }

    public async Task<IEnumerable<TableInfo>> ListTablesAsync(string schemaName)
    {
        await using var connection = await GetConnectionAsync();
        var tables = await connection.QueryAsync<TableInfo>("""
                SELECT
                    table_name as Name,
                    table_type as Type
                FROM information_schema.tables
                WHERE table_schema = @schemaName
                ORDER BY table_name
                """,
            new { schemaName });
        return tables.AsList();
    }

    public async Task<TableInfoExtended?> DescribeTableAsync(string tableName, string schemaName)
    {
        await using var connection = await GetConnectionAsync();

        // Query 1: Table information
        var tableInfo = await connection.QueryFirstOrDefaultAsync<TableInfo>("""
            SELECT
                table_name as Name,
                table_type as Type
            FROM information_schema.tables
            WHERE table_schema = @schemaName AND table_name = @tableName
            """, new { schemaName, tableName });

        if (tableInfo is null) return null;

        // Query 2: Column information
        var columns = await connection.QueryAsync<ColumnInfo>("""
            SELECT
                c.column_name as Name,
                c.data_type as DataType,
                c.is_nullable as IsNullable,
                CASE
                    WHEN tc.constraint_type = 'PRIMARY KEY' THEN 'PRI'
                    WHEN tc.constraint_type = 'UNIQUE' THEN 'UNI'
                    WHEN ix.indexname IS NOT NULL THEN 'MUL'
                    ELSE ''
                END as ColumnKey,
                CASE
                    WHEN c.column_default LIKE 'nextval%' THEN 'auto_increment'
                    WHEN c.is_identity = 'YES' THEN 'auto_increment'
                    ELSE ''
                END as Extra,
                COALESCE(c.column_default, '') as DefaultValue,
                CASE WHEN tc.constraint_type = 'PRIMARY KEY' THEN 1 ELSE 0 END as IsPrimaryKey,
                CASE WHEN c.column_default LIKE 'nextval%' OR c.is_identity = 'YES' THEN 1 ELSE 0 END as IsAutoIncrement,
                CASE WHEN ix.indexname IS NOT NULL THEN 1 ELSE 0 END as HasIndex,
                CASE WHEN tc.constraint_type = 'UNIQUE' THEN 1 ELSE 0 END as IsUnique
            FROM information_schema.columns c
            LEFT JOIN (
                SELECT ccu.column_name, tc.constraint_type
                FROM information_schema.table_constraints tc
                JOIN information_schema.constraint_column_usage ccu
                    ON tc.constraint_name = ccu.constraint_name
                    AND tc.table_schema = ccu.table_schema
                WHERE tc.table_schema = @schemaName
                  AND tc.table_name = @tableName
                  AND tc.constraint_type IN ('PRIMARY KEY', 'UNIQUE')
            ) tc ON c.column_name = tc.column_name
            LEFT JOIN (
                SELECT DISTINCT a.attname as column_name, i.indexname
                FROM pg_catalog.pg_index ix
                JOIN pg_catalog.pg_class t ON t.oid = ix.indrelid
                JOIN pg_catalog.pg_namespace n ON n.oid = t.relnamespace
                JOIN pg_catalog.pg_attribute a ON a.attrelid = t.oid AND a.attnum = ANY(ix.indkey)
                JOIN pg_indexes i ON i.tablename = t.relname AND i.schemaname = n.nspname
                WHERE n.nspname = @schemaName AND t.relname = @tableName
            ) ix ON c.column_name = ix.column_name
            WHERE c.table_schema = @schemaName AND c.table_name = @tableName
            ORDER BY c.ordinal_position
            """, new { schemaName, tableName });

        // Query 3: Index information
        var indexes = await connection.QueryAsync<IndexInfo>("""
            SELECT
                i.indexname as Name,
                am.amname as Type,
                a.attname as ColumnName,
                CASE WHEN ix.indisunique THEN 1 ELSE 0 END as IsUnique
            FROM pg_indexes i
            JOIN pg_catalog.pg_class c ON c.relname = i.indexname
            JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace AND n.nspname = i.schemaname
            JOIN pg_catalog.pg_index ix ON ix.indexrelid = c.oid
            JOIN pg_catalog.pg_am am ON am.oid = c.relam
            CROSS JOIN LATERAL unnest(ix.indkey) WITH ORDINALITY AS k(attnum, ord)
            JOIN pg_catalog.pg_attribute a ON a.attrelid = ix.indrelid AND a.attnum = k.attnum
            WHERE i.schemaname = @schemaName AND i.tablename = @tableName
            ORDER BY i.indexname, k.ord
            """, new { schemaName, tableName });

        // Query 4: Foreign key information
        var foreignKeys = await connection.QueryAsync<ForeignKeyInfo>("""
            SELECT
                tc.constraint_name as Name,
                kcu.column_name as LocalColumn,
                ccu.table_schema as ReferencedSchema,
                ccu.table_name as ReferencedTable,
                ccu.column_name as ReferencedColumn
            FROM information_schema.table_constraints tc
            JOIN information_schema.key_column_usage kcu
                ON tc.constraint_name = kcu.constraint_name
                AND tc.table_schema = kcu.table_schema
            JOIN information_schema.constraint_column_usage ccu
                ON tc.constraint_name = ccu.constraint_name
                AND tc.table_schema = ccu.table_schema
            WHERE tc.constraint_type = 'FOREIGN KEY'
              AND tc.table_schema = @schemaName
              AND tc.table_name = @tableName
            ORDER BY tc.constraint_name, kcu.ordinal_position
            """, new { schemaName, tableName });

        return new TableInfoExtended(
            tableName,
            tableInfo.Type,
            columns.AsList(),
            indexes.AsList(),
            foreignKeys.AsList()
        );
    }

    public async Task<IEnumerable<TableReferenceInfo>> FindReferencesAsync(string tableName, string schemaName)
    {
        await using var connection = await GetConnectionAsync();
        var references = await connection.QueryAsync<TableReferenceInfo>("""
            SELECT
                tc.table_schema as ReferencingSchema,
                tc.table_name as ReferencingTable,
                kcu.column_name as ReferencingColumn,
                tc.constraint_name as ConstraintName,
                ccu.column_name as ReferencedColumn
            FROM information_schema.table_constraints tc
            JOIN information_schema.key_column_usage kcu
                ON tc.constraint_name = kcu.constraint_name
                AND tc.table_schema = kcu.table_schema
            JOIN information_schema.constraint_column_usage ccu
                ON tc.constraint_name = ccu.constraint_name
                AND tc.table_schema = ccu.table_schema
            WHERE tc.constraint_type = 'FOREIGN KEY'
              AND ccu.table_schema = @schemaName
              AND ccu.table_name = @tableName
            ORDER BY tc.table_name, tc.constraint_name, kcu.ordinal_position
            """, new { schemaName, tableName });
        return references.AsList();
    }

    public async Task<IEnumerable<dynamic>> ExecuteQueryAsync(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("SQL query cannot be empty.", nameof(sql));
        if (sql.Contains("SET ", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("SET statements are not allowed.", nameof(sql));

        await using var connection = await GetConnectionAsync();
        var results = await connection.QueryAsync<dynamic>(sql);
        return results.AsList();
    }
}
