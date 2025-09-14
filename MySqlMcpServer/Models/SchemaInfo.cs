namespace MySqlMcpServer.Models;

public record SchemaInfo(
    string Name,
    int IsSystem,
    long TableCount,
    long ViewCount
);

public record TableInfo(
    string Name,
    string Type
);

public record TableInfoExtended(
    string Name,
    string Type,
    List<ColumnInfo> Columns,
    List<IndexInfo> Indexes,
    List<ForeignKeyInfo> ForeignKeys);

public record ColumnInfo(
    string Name,
    string DataType,
    string IsNullable,
    string ColumnKey,
    string Extra,
    string DefaultValue,
    int IsPrimaryKey,
    int IsAutoIncrement,
    int HasIndex,
    int IsUnique
);

public record IndexInfo(
    string Name,
    string Type,
    string ColumnName,
    int IsUnique
);

public record ForeignKeyInfo(
    string Name,
    string LocalColumn,
    string ReferencedSchema,
    string ReferencedTable,
    string ReferencedColumn
);