using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using MySqlMcpServer.Models;

namespace MySqlMcpServer.Services;

public interface IMySqlIntrospectionService
{
    Task<IEnumerable<SchemaInfo>> ListSchemasAsync();   
    Task<IEnumerable<TableInfo>> ListTablesAsync(string schemaName);
    Task<TableInfoExtended?> DescribeTableAsync(string tableName, string schemaName);
}

public class MySqlIntrospectionService(IConfiguration configuration, ILogger<MySqlIntrospectionService> logger) : IMySqlIntrospectionService
{
    private readonly ILogger<MySqlIntrospectionService> _logger = logger;
    private readonly IConfiguration _configuration = configuration;

    private MySqlConnection GetConnection()
    {        
        try {
            var connectionString = _configuration.GetValue<string?>("MCP_MySQL_ConnectionString");
            if (string.IsNullOrWhiteSpace(connectionString)) throw new Exception();
            return new MySqlConnection(connectionString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create MySQL connection with the provided connection string.");
            throw new InvalidOperationException("MySQL connection string not configured. Set it as `MCP_MySQL_ConnectionString` environment variable or appsettings.json value.");
        }
    }

    public async Task<IEnumerable<SchemaInfo>> ListSchemasAsync()
    {
        await using var connection = GetConnection();

        var schemas = await connection.QueryAsync<SchemaInfo>("""
                  SELECT
                      s.SCHEMA_NAME as Name,
                      CASE WHEN SCHEMA_NAME IN ('information_schema', 'mysql', 'performance_schema', 'sys')
                       THEN 1 ELSE 0 END as IsSystem,
                      COUNT(CASE WHEN t.TABLE_TYPE = 'BASE TABLE' THEN 1 END) AS TableCount,
                      COUNT(CASE WHEN t.TABLE_TYPE = 'VIEW' THEN 1 END) AS ViewCount
                  FROM
                      information_schema.SCHEMATA s
                  LEFT JOIN
                      information_schema.TABLES t ON s.SCHEMA_NAME = t.TABLE_SCHEMA
                  GROUP BY
                      s.SCHEMA_NAME
                  ORDER BY
                      s.SCHEMA_NAME;
                  """);
        return schemas.AsList();
    }

    public async Task<IEnumerable<TableInfo>> ListTablesAsync(string schemaName)
    {
        await using var connection = GetConnection();
        var tables = await connection.QueryAsync<TableInfo>("""
                SELECT
                    TABLE_NAME as Name,
                    TABLE_TYPE as Type
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_SCHEMA = @schemaName
                ORDER BY TABLE_NAME
                """,
            new { schemaName });
        return tables.AsList();
    }

    public async Task<TableInfoExtended?> DescribeTableAsync(string tableName, string schemaName)
    {
        await using var connection = GetConnection();

        const string sql = """
		-- Query 1: Table information
		SELECT
		    TABLE_NAME as Name,
		    TABLE_TYPE as Type
		FROM INFORMATION_SCHEMA.TABLES
		WHERE TABLE_SCHEMA = @schemaName AND TABLE_NAME = @tableName;

		-- Query 2: Column information
		SELECT
		    c.COLUMN_NAME as Name,
		    c.DATA_TYPE as DataType,
		    c.IS_NULLABLE as IsNullable,
		    c.COLUMN_KEY as ColumnKey,
		    c.EXTRA as Extra,
		    c.COLUMN_DEFAULT as DefaultValue,
		    CASE WHEN c.COLUMN_KEY = 'PRI' THEN 1 ELSE 0 END as IsPrimaryKey,
		    CASE WHEN c.EXTRA LIKE '%auto_increment%' THEN 1 ELSE 0 END as IsAutoIncrement,
		    CASE WHEN c.COLUMN_KEY IN ('MUL', 'UNI') THEN 1 ELSE 0 END as HasIndex,
		    CASE WHEN c.COLUMN_KEY = 'UNI' THEN 1 ELSE 0 END as IsUnique
		FROM INFORMATION_SCHEMA.COLUMNS c
		WHERE c.TABLE_SCHEMA = @schemaName AND c.TABLE_NAME = @tableName
		ORDER BY c.ORDINAL_POSITION;

		-- Query 3: Index information
		SELECT
		    INDEX_NAME as Name,
		    INDEX_TYPE as Type,
		    COLUMN_NAME as ColumnName,
		    CASE WHEN NON_UNIQUE = 0 THEN 1 ELSE 0 END as IsUnique
		FROM INFORMATION_SCHEMA.STATISTICS
		WHERE TABLE_SCHEMA = @schemaName AND TABLE_NAME = @tableName
		ORDER BY INDEX_NAME, SEQ_IN_INDEX;

		-- Query 4: Foreign key information
		SELECT
		    kcu.CONSTRAINT_NAME as Name,
		    kcu.COLUMN_NAME as LocalColumn,
		    kcu.REFERENCED_TABLE_SCHEMA as ReferencedSchema,
		    kcu.REFERENCED_TABLE_NAME as ReferencedTable,
		    kcu.REFERENCED_COLUMN_NAME as ReferencedColumn
		FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
		WHERE kcu.TABLE_SCHEMA = @schemaName
		  AND kcu.REFERENCED_TABLE_NAME IS NOT NULL
		  AND kcu.TABLE_NAME = @tableName
		ORDER BY kcu.CONSTRAINT_NAME, kcu.ORDINAL_POSITION;
		""";
        await using var multi = await connection.QueryMultipleAsync(sql, new { schemaName, tableName });

        // Read each result set
        var tableInfo = await multi.ReadFirstOrDefaultAsync<TableInfo>();
        if (tableInfo is null) return null;
        var columns = await multi.ReadAsync<ColumnInfo>();
        var indexes = await multi.ReadAsync<IndexInfo>();
        var foreignKeys = await multi.ReadAsync<ForeignKeyInfo>();


        return new TableInfoExtended(
            tableName,
            tableInfo.Type,
            columns.AsList(),
            indexes.AsList(),
            foreignKeys.AsList()
        );
    }
}