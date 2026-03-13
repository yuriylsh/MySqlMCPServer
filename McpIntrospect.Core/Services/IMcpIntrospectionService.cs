using McpIntrospect.Core.Models;

namespace McpIntrospect.Core.Services;

public interface IMcpIntrospectionService
{
    Task<IEnumerable<SchemaInfo>> ListSchemasAsync();
    Task<IEnumerable<TableInfo>> ListTablesAsync(string schemaName);
    Task<TableInfoExtended?> DescribeTableAsync(string tableName, string schemaName);
    Task<IEnumerable<TableReferenceInfo>> FindReferencesAsync(string tableName, string schemaName);
    Task<IEnumerable<dynamic>> ExecuteQueryAsync(string sql);
}
