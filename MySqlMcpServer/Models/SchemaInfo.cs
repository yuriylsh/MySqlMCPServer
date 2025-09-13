namespace MySqlMcpServer.Models;

public record SchemaInfo(
    string Name,
    bool IsSystem,
    int? TableCount = null,
    int? ViewCount = null
);

public record TableInfo(
    string Schema,
    string Name,
    string Type,
    List<ColumnInfo> Columns,
    List<IndexInfo> Indexes,
    List<ForeignKeyInfo> ForeignKeys
);

public record ColumnInfo(
    string Name,
    string DataType,
    bool IsNullable,
    bool IsPrimaryKey = false,
    bool IsAutoIncrement = false,
    bool HasIndex = false,
    bool IsUnique = false,
    string? DefaultValue = null
);

public record IndexInfo(
    string Name,
    string Type,
    List<string> Columns,
    bool IsUnique
);

public record ForeignKeyInfo(
    string Name,
    List<string> LocalColumns,
    string ReferencedSchema,
    string ReferencedTable,
    List<string> ReferencedColumns
);

public record ViewInfo(
    string Schema,
    string Name,
    string? Definition = null
);