using System.CommandLine;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using McpIntrospect.MySql.Services;

var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

var schemaOption = new Option<string>("--schema")
{
    Description = "Database schema name",
    DefaultValueFactory = _ => "nbc_amp"
};

var tableArgForDescribe = new Argument<string>("table") { Description = "Table name" };
var tableArgForRefs = new Argument<string>("table") { Description = "Table name" };
var sqlArgForQuery = new Argument<string>("sql") { Description = "SQL query to execute (read-only)" };

var listSchemasCommand = new Command("list-schemas", "List all accessible database schemas");

var listTablesCommand = new Command("list-tables", "List all tables in a schema") { schemaOption };

var describeTableCommand = new Command("describe-table", "Get full table definition (columns, indexes, foreign keys)")
{
    tableArgForDescribe,
    schemaOption
};

var findReferencesCommand = new Command("find-references", "Find all tables that reference this table via foreign keys")
{
    tableArgForRefs,
    schemaOption
};

var queryCommand = new Command("query", "Execute a read-only SQL query against the database")
{
    sqlArgForQuery
};

listSchemasCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var service = BuildService();
    await RunCommand(async () => await service.ListSchemasAsync());
});

listTablesCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var schema = parseResult.GetValue(schemaOption)!;
    var service = BuildService();
    await RunCommand(async () => await service.ListTablesAsync(schema));
});

describeTableCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var schema = parseResult.GetValue(schemaOption)!;
    var table = parseResult.GetValue(tableArgForDescribe)!;
    var service = BuildService();
    await RunCommand(async () =>
        (object?)await service.DescribeTableAsync(table, schema)
        ?? new { error = $"Table '{table}' not found in schema '{schema}'" });
});

findReferencesCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var schema = parseResult.GetValue(schemaOption)!;
    var table = parseResult.GetValue(tableArgForRefs)!;
    var service = BuildService();
    await RunCommand(async () => await service.FindReferencesAsync(table, schema));
});

queryCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var sql = parseResult.GetValue(sqlArgForQuery)!;
    var service = BuildService();
    await RunCommand(async () => await service.ExecuteQueryAsync(sql));
});

var rootCommand = new RootCommand("MySQL database schema introspection CLI")
{
    listSchemasCommand,
    listTablesCommand,
    describeTableCommand,
    findReferencesCommand,
    queryCommand
};

return await rootCommand.Parse(args).InvokeAsync();

MySqlIntrospectionService BuildService()
{
    var configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true)
        .AddEnvironmentVariables()
        .Build();
    return new MySqlIntrospectionService(configuration, NullLogger<MySqlIntrospectionService>.Instance);
}

async Task RunCommand(Func<Task<object>> action)
{
    try
    {
        var result = await action();
        Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
    }
    catch (Exception ex)
    {
        Console.WriteLine(JsonSerializer.Serialize(new { error = ex.Message }, jsonOptions));
    }
}
