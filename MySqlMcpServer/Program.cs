using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MySqlMcpServer.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Logging.AddConsole(consoleLogOptions =>
{
    // Configure all logs to go to stderr since stdout is used for MCP communication.
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddSingleton<IMySqlIntrospectionService, MySqlIntrospectionService>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
