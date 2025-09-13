using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using MySqlMcpServer.Models;

namespace MySqlMcpServer.Services;

public interface IMySqlIntrospectionService
{
    Task<IEnumerable<SchemaInfo>> ListSchemasAsync();
    Task<SchemaInfo?> DescribeSchemaAsync(string schemaName);
    Task<IEnumerable<TableInfo>> ListTablesAsync(string? schemaName = null);
    Task<TableInfo?> DescribeTableAsync(string tableName, string? schemaName = null);
    Task<IEnumerable<ColumnInfo>> ListColumnsAsync(string tableName, string? schemaName = null);
    Task<IEnumerable<IndexInfo>> ListIndexesAsync(string tableName, string? schemaName = null);
    Task<IEnumerable<ForeignKeyInfo>> ListForeignKeysAsync(string? tableName = null, string? schemaName = null);
    Task<IEnumerable<ViewInfo>> ListViewsAsync(string? schemaName = null);
}

public class MySqlIntrospectionService : IMySqlIntrospectionService
{
    private readonly string _connectionString;
    private readonly ILogger<MySqlIntrospectionService> _logger;

    public MySqlIntrospectionService(IConfiguration configuration, ILogger<MySqlIntrospectionService> logger)
    {
        _logger = logger;

        // Try environment variable first, then appsettings.json
        _connectionString = Environment.GetEnvironmentVariable("MCP_MySQL_ConnectionString")
                          ?? configuration.GetConnectionString("DefaultConnection")
                          ?? throw new InvalidOperationException("MySQL connection string not configured");
    }

    public async Task<IEnumerable<SchemaInfo>> ListSchemasAsync()
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var schemas = await connection.QueryAsync<dynamic>(@"
            SELECT
                SCHEMA_NAME as Name,
                CASE WHEN SCHEMA_NAME IN ('information_schema', 'mysql', 'performance_schema', 'sys')
                     THEN 1 ELSE 0 END as IsSystem
            FROM INFORMATION_SCHEMA.SCHEMATA
            ORDER BY SCHEMA_NAME");

        var result = new List<SchemaInfo>();
        foreach (var schema in schemas)
        {
            var schemaName = (string)schema.Name;
            var isSystem = Convert.ToBoolean(schema.IsSystem);

            if (!isSystem)
            {
                var tableCount = await connection.QuerySingleAsync<int>(@"
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_SCHEMA = @schemaName AND TABLE_TYPE = 'BASE TABLE'",
                    new { schemaName });

                var viewCount = await connection.QuerySingleAsync<int>(@"
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.VIEWS
                    WHERE TABLE_SCHEMA = @schemaName",
                    new { schemaName });

                result.Add(new SchemaInfo(schemaName, isSystem, tableCount, viewCount));
            }
            else
            {
                result.Add(new SchemaInfo(schemaName, isSystem));
            }
        }

        return result;
    }

    public async Task<SchemaInfo?> DescribeSchemaAsync(string schemaName)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var schemaExists = await connection.QuerySingleOrDefaultAsync<int?>(@"
            SELECT 1 FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = @schemaName",
            new { schemaName });

        if (schemaExists == null) return null;

        var isSystem = new[] { "information_schema", "mysql", "performance_schema", "sys" }
            .Contains(schemaName, StringComparer.OrdinalIgnoreCase);

        if (isSystem)
        {
            return new SchemaInfo(schemaName, true);
        }

        var tableCount = await connection.QuerySingleAsync<int>(@"
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = @schemaName AND TABLE_TYPE = 'BASE TABLE'",
            new { schemaName });

        var viewCount = await connection.QuerySingleAsync<int>(@"
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.VIEWS
            WHERE TABLE_SCHEMA = @schemaName",
            new { schemaName });

        return new SchemaInfo(schemaName, false, tableCount, viewCount);
    }

    public async Task<IEnumerable<TableInfo>> ListTablesAsync(string? schemaName = null)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var currentSchema = schemaName ?? await connection.QuerySingleAsync<string>("SELECT DATABASE()");

        var tables = await connection.QueryAsync<dynamic>(@"
            SELECT
                TABLE_SCHEMA as Schema,
                TABLE_NAME as Name,
                TABLE_TYPE as Type
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = @currentSchema
            ORDER BY TABLE_NAME",
            new { currentSchema });

        var result = new List<TableInfo>();
        foreach (var table in tables)
        {
            var tableInfo = new TableInfo(
                (string)table.Schema,
                (string)table.Name,
                (string)table.Type,
                new List<ColumnInfo>(),
                new List<IndexInfo>(),
                new List<ForeignKeyInfo>()
            );
            result.Add(tableInfo);
        }

        return result;
    }

    public async Task<TableInfo?> DescribeTableAsync(string tableName, string? schemaName = null)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var currentSchema = schemaName ?? await connection.QuerySingleAsync<string>("SELECT DATABASE()");

        var tableExists = await connection.QuerySingleOrDefaultAsync<string>(@"
            SELECT TABLE_TYPE FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = @currentSchema AND TABLE_NAME = @tableName",
            new { currentSchema, tableName });

        if (tableExists == null) return null;

        var columns = await ListColumnsAsync(tableName, currentSchema);
        var indexes = await ListIndexesAsync(tableName, currentSchema);
        var foreignKeys = await ListForeignKeysAsync(tableName, currentSchema);

        return new TableInfo(
            currentSchema,
            tableName,
            tableExists,
            columns.ToList(),
            indexes.ToList(),
            foreignKeys.ToList()
        );
    }

    public async Task<IEnumerable<ColumnInfo>> ListColumnsAsync(string tableName, string? schemaName = null)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var currentSchema = schemaName ?? await connection.QuerySingleAsync<string>("SELECT DATABASE()");

        var columns = await connection.QueryAsync<dynamic>(@"
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
            WHERE c.TABLE_SCHEMA = @currentSchema AND c.TABLE_NAME = @tableName
            ORDER BY c.ORDINAL_POSITION",
            new { currentSchema, tableName });

        return columns.Select(c => new ColumnInfo(
            (string)c.Name,
            (string)c.DataType,
            ((string)c.IsNullable).Equals("YES", StringComparison.OrdinalIgnoreCase),
            Convert.ToBoolean(c.IsPrimaryKey),
            Convert.ToBoolean(c.IsAutoIncrement),
            Convert.ToBoolean(c.HasIndex),
            Convert.ToBoolean(c.IsUnique),
            c.DefaultValue?.ToString()
        ));
    }

    public async Task<IEnumerable<IndexInfo>> ListIndexesAsync(string tableName, string? schemaName = null)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var currentSchema = schemaName ?? await connection.QuerySingleAsync<string>("SELECT DATABASE()");

        var indexes = await connection.QueryAsync<dynamic>(@"
            SELECT
                INDEX_NAME as Name,
                INDEX_TYPE as Type,
                COLUMN_NAME as ColumnName,
                CASE WHEN NON_UNIQUE = 0 THEN 1 ELSE 0 END as IsUnique
            FROM INFORMATION_SCHEMA.STATISTICS
            WHERE TABLE_SCHEMA = @currentSchema AND TABLE_NAME = @tableName
            ORDER BY INDEX_NAME, SEQ_IN_INDEX",
            new { currentSchema, tableName });

        return indexes
            .GroupBy(i => new { Name = (string)i.Name, Type = (string)i.Type, IsUnique = Convert.ToBoolean(i.IsUnique) })
            .Select(g => new IndexInfo(
                g.Key.Name,
                g.Key.Type,
                g.Select(i => (string)i.ColumnName).ToList(),
                g.Key.IsUnique
            ));
    }

    public async Task<IEnumerable<ForeignKeyInfo>> ListForeignKeysAsync(string? tableName = null, string? schemaName = null)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var currentSchema = schemaName ?? await connection.QuerySingleAsync<string>("SELECT DATABASE()");

        var whereClause = tableName != null ? "AND kcu.TABLE_NAME = @tableName" : "";

        var foreignKeys = await connection.QueryAsync<dynamic>($@"
            SELECT
                kcu.CONSTRAINT_NAME as Name,
                kcu.COLUMN_NAME as LocalColumn,
                kcu.REFERENCED_TABLE_SCHEMA as ReferencedSchema,
                kcu.REFERENCED_TABLE_NAME as ReferencedTable,
                kcu.REFERENCED_COLUMN_NAME as ReferencedColumn
            FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
            WHERE kcu.TABLE_SCHEMA = @currentSchema
              AND kcu.REFERENCED_TABLE_NAME IS NOT NULL
              {whereClause}
            ORDER BY kcu.CONSTRAINT_NAME, kcu.ORDINAL_POSITION",
            new { currentSchema, tableName });

        return foreignKeys
            .GroupBy(fk => new { Name = (string)fk.Name, ReferencedSchema = (string)fk.ReferencedSchema, ReferencedTable = (string)fk.ReferencedTable })
            .Select(g => new ForeignKeyInfo(
                g.Key.Name,
                g.Select(fk => (string)fk.LocalColumn).ToList(),
                g.Key.ReferencedSchema,
                g.Key.ReferencedTable,
                g.Select(fk => (string)fk.ReferencedColumn).ToList()
            ));
    }

    public async Task<IEnumerable<ViewInfo>> ListViewsAsync(string? schemaName = null)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var currentSchema = schemaName ?? await connection.QuerySingleAsync<string>("SELECT DATABASE()");

        var views = await connection.QueryAsync<dynamic>(@"
            SELECT
                TABLE_SCHEMA as Schema,
                TABLE_NAME as Name
            FROM INFORMATION_SCHEMA.VIEWS
            WHERE TABLE_SCHEMA = @currentSchema
            ORDER BY TABLE_NAME",
            new { currentSchema });

        return views.Select(v => new ViewInfo((string)v.Schema, (string)v.Name));
    }
}