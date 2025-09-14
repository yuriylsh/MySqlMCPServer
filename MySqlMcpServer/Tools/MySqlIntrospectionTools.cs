using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using MySqlMcpServer.Models;
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
     Description("Lists all accessible database schemas with basic metadata. This tool applies only to MySql database.")]
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
            return JsonSerializer.Serialize(new ErrorResponse($"Error listing schemas: {ex.Message}"), ErrorResponseSerializerContext.Default.ErrorResponse);
        }
    }
    

    [McpServerTool(Name = "list_tables"),
     Description("Returns table names, types (BASE TABLE/VIEW), and basic metadata for a schema. This tool applies only to MySql database.")]
    public async Task<object> ListTables(
        [Description("The name of the schema. This parameter is required.")] string? schema_name)
    {
        if (string.IsNullOrEmpty(schema_name)) return JsonSerializer.Serialize(new ErrorResponse("`schema_name` is required."), ErrorResponseSerializerContext.Default.ErrorResponse);
        try
        {
            var tables = await _introspectionService.ListTablesAsync(schema_name);
            return new { tables };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing tables for schema {SchemaName}", schema_name);
            return JsonSerializer.Serialize(new ErrorResponse($"Error listing tables for schema '{schema_name}': {ex.Message}"), ErrorResponseSerializerContext.Default.ErrorResponse);
        }
    }

    [McpServerTool(Name = "describe_table"),
     Description("Returns comprehensive table information including columns, primary keys, indexes, and foreign key constraints. This tool applies only to MySql database.")]
    public async Task<object> DescribeTable(
        [Description("The name of the table to describe. This parameter is required.")] string table_name,
        [Description("The name of the schema. This parameter is required.")] string? schema_name)
    {
        if (string.IsNullOrEmpty(table_name)) return JsonSerializer.Serialize(new ErrorResponse("`table_name` is required."), ErrorResponseSerializerContext.Default.ErrorResponse);
        if (string.IsNullOrEmpty(schema_name)) return JsonSerializer.Serialize(new ErrorResponse("`schema_name` is required."), ErrorResponseSerializerContext.Default.ErrorResponse);
        try
        {
            var table = await _introspectionService.DescribeTableAsync(table_name, schema_name);
            if (table == null)
            {
                return JsonSerializer.Serialize(new ErrorResponse($"Table `{table_name}` not found in schema `{schema_name}`"), ErrorResponseSerializerContext.Default.ErrorResponse);
            }
            return table;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error describing table {TableName} in schema {SchemaName}", table_name, schema_name);
            return JsonSerializer.Serialize(new ErrorResponse($"Error describing table '{table_name}' in schema '{schema_name}': {ex.Message}"), ErrorResponseSerializerContext.Default.ErrorResponse);
        }
    }
}