using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build();

var transportOptions = configuration.GetSection(nameof(StdioClientTransportOptions)).Get<StdioClientTransportOptions>();
var hasReferencedServerDll = transportOptions is { Arguments.Count: > 0 } && File.Exists(transportOptions.Arguments[0]);
if (!hasReferencedServerDll)
{
    Console.WriteLine($"The MCP server DLL configuration is incorrect: the file \"{transportOptions?.Arguments?[0]}\" does not exist.");
    return;
}

var clientTransport = new StdioClientTransport(transportOptions!);
var client = await McpClientFactory.CreateAsync(clientTransport);

// Print the list of tools available from the server.
var tools = await client.ListToolsAsync();
foreach (var tool in tools)
{
    Console.WriteLine($"{tool.Name} ({tool.Description})");
}

