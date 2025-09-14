# PRD — MCP Server for MySQL DDL Context (C#/.NET + Dapper Edition)

## Overview

Build a lightweight Model Context Protocol (MCP) server in C#/.NET that introspects MySQL 8 databases using Dapper ORM and exposes Data Definition Language (DDL) context to Language Learning Model clients. The server provides schema introspection capabilities through MCP tools over JSON-RPC via stdio transport.

## Objectives

- **Primary**: Create a C#/.NET MCP server that exposes MySQL database schema information to LLM clients
- **Secondary**: Demonstrate MCP protocol implementation using the official ModelContextProtocol NuGet package
- **Tertiary**: Provide a working sample client to validate the implementation

## Target Audience

- **Primary**: Developers building LLM-powered database tools and applications
- **Secondary**: Database administrators seeking schema introspection automation
- **Tertiary**: MCP protocol adopters in the .NET ecosystem

## Functional Requirements

### Core MCP Server Capabilities

#### 1. Database Connection Management
- Connect to MySQL 8+ databases using connection strings
- Support standard MySQL connection parameters (host, port, database, username, password)
- Handle connection failures gracefully with meaningful error messages

#### 2. Schema Introspection Tools

**Tool: `list_schemas`**
- Lists all accessible database schemas
- Returns schema names and basic metadata

**Tool: `list_tables`**
- Parameter: `schema_name` (string, required)
- Returns table names, types (BASE TABLE/VIEW), and basic metadata

**Tool: `describe_table`**
- Parameters: 
  - `schema_name` (string, required)
  - `table_name` (string, required)
- Returns comprehensive table information:
  - Column definitions (name, data type, nullable, default values)
  - Primary key information
  - Indexes (name, type, columns, uniqueness)
  - Foreign key constraints (local columns, referenced table/columns)

#### 3. MCP Protocol Compliance
- Implement MCP JSON-RPC protocol over stdio transport
- Support standard MCP initialization handshake
- Handle tool discovery and execution requests
- Provide proper error responses and status codes

### Sample Client Requirements

#### MCP Client Implementation
- Demonstrate connection to the MCP server
- Execute sample introspection workflows
- Show error handling and response processing
- Include examples of common use cases:
  - Getting database overview
  - Exploring table structures
  - Understanding relationships between tables

## Technical Requirements

### Technology Stack
- **Runtime**: .NET 9 or later
- **ORM**: Dapper for MySQL data access
- **MCP Library**: ModelContextProtocol NuGet package (REQUIRED), currently available version is `0.3.0-preview.4`
- **MySQL Driver**: MySqlConnector, support getting connection string from `MCP_MySQL_ConnectionString` environment variable or apsettings.json file
- **Transport**: JSON-RPC over stdio

### Architecture Constraints
- **No Caching**: Direct database queries for each request
- **No Pagination**: Return complete result sets
- **No Read-Only Safety**: Assume read-only usage patterns
- **No Triggers/Routines**: Exclude stored procedures, functions, and triggers from introspection

### Performance Considerations
- Target response times under 2 seconds for typical introspection queries
- Handle databases with up to 1000 tables efficiently
- Minimize memory usage for large schema responses

## Implementation Scope

### In Scope
- MySQL 8+ compatibility
- Basic authentication (username/password)
- Standard MySQL data types mapping
- Primary keys, foreign keys, and indexes
- Views (structure only, not definitions)
- Error handling and logging
- Cross-platform compatibility (.NET runtime)
- Use Central Package Management (root Directory.Packages.props file) for nuget package management 

### Out of Scope
- Stored procedures and functions introspection
- Trigger definitions
- Advanced MySQL features (partitioning, etc.)
- SSL/TLS certificate management
- Connection pooling or persistence
- Data modification operations
- Query result caching
- Pagination for large result sets
- Advanced authentication methods
- Performance optimization beyond basic query efficiency

## Success Criteria

### Minimum Viable Product (MVP)
1. MCP server successfully connects to MySQL 8
2. All specified introspection tools function correctly
3. Sample client can connect and execute basic workflows
4. Proper MCP protocol compliance (initialization, tool discovery, execution)

### Quality Gates
1. **Functionality**: All tools return accurate MySQL schema information
2. **Protocol Compliance**: MCP handshake and message format validation
3. **Error Handling**: Graceful handling of database connection and query failures
4. **Documentation**: Clear setup instructions and usage examples
5. **Testability**: Sample client demonstrates all server capabilities

## Deliverables

### Primary Deliverables
1. **MCP Server Application**
   - Console application implementing MCP server
   - Configuration via command-line arguments or environment variables
   - Comprehensive error logging

2. **Sample MCP Client**
   - Console application demonstrating server usage
   - Example workflows for common introspection tasks
   - Error handling examples

3. **Documentation**
   - Setup and configuration guide
   - Tool reference documentation
   - Sample usage scenarios

### Supporting Files
- Project configuration (`.csproj` files)
- README with quick start instructions
- Example connection strings and configuration

## Technical Specifications

### Configuration Requirements
```json
{
  "connectionString": "Server=localhost;Database=testdb;Uid=user;Pwd=password;",
}
```

### Key Dependencies
```xml
<PackageReference Include="ModelContextProtocol" Version="[latest]" />
<PackageReference Include="Dapper" Version="2.1.35" />
<PackageReference Include="MySqlConnector" Version="2.3.5" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
```

### MCP Tool Response Examples

**Schema List Response:**
```json
{
  "schemas": [
    {
      "name": "information_schema",
      "isSystem": 1
    },
    {
      "name": "testdb",
      "isSystem": 0,
      "tableCount": 5,
      "viewCount": 2
    }
  ]
}
```

**Table Description Response:**
```json
{
  "schema": "testdb",
  "table": "users",
  "type": "BASE TABLE",
  "columns": [
    {
      "name": "id",
      "dataType": "int",
      "isNullable": 0,
      "isPrimaryKey": 1,
      "isAutoIncrement": 1
    },
    {
      "name": "email",
      "dataType": "varchar(255)",
      "isNullable": 1,
      "hasIndex": 1,
      "isUnique": 1
    }
  ],
  "indexes": [
    {
      "name": "PRIMARY",
      "type": "PRIMARY",
      "columns": ["id"],
      "isUnique": 0
    },
    {
      "name": "idx_email",
      "type": "UNIQUE",
      "columns": ["email"],
      "isUnique": 1
    }
  ],
  "foreignKeys": []
}
```

## Risk Assessment

### Technical Risks
- **MySQL Version Compatibility**: Variations in INFORMATION_SCHEMA across MySQL versions
- **Large Schema Performance**: Response times for databases with many tables
- **Connection Management**: Handling database connection failures and timeouts

### Mitigation Strategies
- Test against MySQL 8.0+ specifically
- Set reasonable query timeouts
- Implement comprehensive error handling
- Validate against common MySQL hosting environments

### Examples of setting up MCP server console application
**Program.cs:**
```
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(consoleLogOptions =>
{
    // Configure all logs to go to stderr
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
```

### Example of using XXX to implement MCP tool using ModelContextProtocol nuget package
**StarWarsTools.cs:**
```
using System.ComponentModel;
using System.Text.Json;

using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Server;
using StarWarsMCPServer;

[McpServerToolType]
public static class StarWarsTools
{
    private readonly static ToolsOptions _toolsOptions = new();

    private readonly static HttpClient _httpClient = new();

    static StarWarsTools()
    {
        // Build the configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // Get the Tools configuration
        _toolsOptions = configuration.GetSection(ToolsOptions.SectionName)
                                     .Get<ToolsOptions>()!;

        if (_toolsOptions == null)
        {
            throw new InvalidOperationException("Tools configuration is missing. Please check your appsettings.json file.");
        }

        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_toolsOptions.TavilyApiKey}");
    }

    [McpServerTool(Name = "WookiepediaTool"),
     Description("A tool for getting information on Star Wars from Wookiepedia. " +
                 "This tool takes a prompt as a query and returns a list of results from Wookiepedia.")]
    public static async Task<string> QueryTheWeb([Description("The query to search for information on Wookiepedia.")] string query)
    {
        var requestBody = new
        {
            query,
            include_answer = "advanced",
            include_domains = new[] { "https://starwars.fandom.com/" }
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync("https://api.tavily.com/search", content);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }
}
```

## Timeline Estimate

**Total Development Time**: 8-12 hours for vibe coding session

- **Server Core (4-5 hours)**
  - MCP protocol implementation
  - Database connection and introspection queries
  - Tool implementations

- **Client Sample (2-3 hours)**
  - MCP client implementation
  - Example workflows
  - Testing and validation

- **Documentation & Polish (2-4 hours)**
  - README and setup instructions
  - Code documentation
  - Testing and bug fixes

This PRD provides the foundation for building a focused, practical MCP server that bridges MySQL database introspection with LLM-powered applications in the .NET ecosystem.