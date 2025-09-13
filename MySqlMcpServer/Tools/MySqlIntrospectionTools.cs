using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using MySqlMcpServer.Services;

namespace MySqlMcpServer.Tools;

[McpServerToolType]
public class MySqlIntrospectionTools
{
    private readonly IMySqlIntrospectionService _introspectionService;
    private readonly ILogger<MySqlIntrospectionTools> _logger;

    public MySqlIntrospectionTools(IMySqlIntrospectionService introspectionService, ILogger<MySqlIntrospectionTools> logger)
    {
        _introspectionService = introspectionService;
        _logger = logger;
    }

    [McpServerTool(Name = "list_schemas"),
     Description("Lists all accessible database schemas with basic metadata")]
    public async Task<object> ListSchemas()
    {
        try
        {
            var schemas = await _introspectionService.ListSchemasAsync();
            return new { schemas };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing schemas");
            throw;
        }
    }

    [McpServerTool(Name = "describe_schema"),
     Description("Returns detailed schema information including table count and view count")]
    public async Task<object> DescribeSchema(
        [Description("The name of the schema to describe")] string schema_name)
    {
        try
        {
            var schema = await _introspectionService.DescribeSchemaAsync(schema_name);
            if (schema == null)
            {
                return new { error = $"Schema '{schema_name}' not found" };
            }
            return schema;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error describing schema {SchemaName}", schema_name);
            throw;
        }
    }

    [McpServerTool(Name = "list_tables"),
     Description("Returns table names, types (BASE TABLE/VIEW), and basic metadata for a schema")]
    public async Task<object> ListTables(
        [Description("The name of the schema (optional - defaults to current database)")] string? schema_name = null)
    {
        try
        {
            var tables = await _introspectionService.ListTablesAsync(schema_name);
            return new { tables };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing tables for schema {SchemaName}", schema_name);
            throw;
        }
    }

    [McpServerTool(Name = "describe_table"),
     Description("Returns comprehensive table information including columns, primary keys, indexes, and foreign key constraints")]
    public async Task<object> DescribeTable(
        [Description("The name of the table to describe")] string table_name,
        [Description("The name of the schema (optional)")] string? schema_name = null)
    {
        try
        {
            var table = await _introspectionService.DescribeTableAsync(table_name, schema_name);
            if (table == null)
            {
                return new { error = $"Table '{table_name}' not found in schema '{schema_name ?? "current"}'" };
            }
            return table;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error describing table {TableName} in schema {SchemaName}", table_name, schema_name);
            throw;
        }
    }

    [McpServerTool(Name = "list_columns"),
     Description("Returns column information with data types, constraints, and properties for a table")]
    public async Task<object> ListColumns(
        [Description("The name of the table")] string table_name,
        [Description("The name of the schema (optional)")] string? schema_name = null)
    {
        try
        {
            var columns = await _introspectionService.ListColumnsAsync(table_name, schema_name);
            return new { columns };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing columns for table {TableName} in schema {SchemaName}", table_name, schema_name);
            throw;
        }
    }

    [McpServerTool(Name = "list_indexes"),
     Description("Returns index information including type, columns, and uniqueness constraints for a table")]
    public async Task<object> ListIndexes(
        [Description("The name of the table")] string table_name,
        [Description("The name of the schema (optional)")] string? schema_name = null)
    {
        try
        {
            var indexes = await _introspectionService.ListIndexesAsync(table_name, schema_name);
            return new { indexes };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing indexes for table {TableName} in schema {SchemaName}", table_name, schema_name);
            throw;
        }
    }

    [McpServerTool(Name = "list_foreign_keys"),
     Description("Returns foreign key relationships with referenced tables and columns")]
    public async Task<object> ListForeignKeys(
        [Description("The name of the table (optional - if not provided, returns all FKs in schema)")] string? table_name = null,
        [Description("The name of the schema (optional)")] string? schema_name = null)
    {
        try
        {
            var foreignKeys = await _introspectionService.ListForeignKeysAsync(table_name, schema_name);
            return new { foreignKeys };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing foreign keys for table {TableName} in schema {SchemaName}", table_name, schema_name);
            throw;
        }
    }

    [McpServerTool(Name = "list_views"),
     Description("Returns view definitions and metadata for a schema")]
    public async Task<object> ListViews(
        [Description("The name of the schema (optional)")] string? schema_name = null)
    {
        try
        {
            var views = await _introspectionService.ListViewsAsync(schema_name);
            return new { views };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing views for schema {SchemaName}", schema_name);
            throw;
        }
    }
}