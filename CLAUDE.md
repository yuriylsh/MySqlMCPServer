# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview
This is a MySQL MCP (Model Context Protocol) Server built with C#/.NET that introspects MySQL 8+ databases using Dapper ORM and exposes DDL context to LLM clients via JSON-RPC over stdio transport.

## Architecture Overview
- **MCP Server**: Console application implementing MCP protocol for MySQL schema introspection
- **Sample Client**: Console application demonstrating server usage and common workflows
- **Core Dependencies**: ModelContextProtocol (v0.3.0-preview.4), Dapper, MySqlConnector
- **Transport**: JSON-RPC over stdio (standard input/output)
- **Package Management**: Central Package Management using root Directory.Packages.props file

## Key MCP Tools to Implement
The server exposes these introspection tools:
- `list_schemas` - Lists all accessible database schemas
- `describe_schema` - Returns detailed schema information
- `list_tables` - Returns table names and metadata for a schema
- `describe_table` - Comprehensive table info (columns, PKs, indexes, FKs)
- `list_columns` - Column information with data types and constraints
- `list_indexes` - Index information including type and uniqueness
- `list_foreign_keys` - Foreign key relationships
- `list_views` - View definitions and metadata

## Development Commands

### Build and Run
```bash
# Build the solution
dotnet build

# Run the MCP server
dotnet run --project MySqlMcpServer

# Run the sample client
dotnet run --project MySqlMcpClient
```

### Testing
```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Package Management
This project uses Central Package Management. All package versions are defined in the root `Directory.Packages.props` file.

## Configuration
- **Connection String**: Retrieved from `MCP_MySQL_ConnectionString` environment variable or appsettings.json
- **Server Configuration**: serverName and version in appsettings.json
- **Target Framework**: .NET 9 or later

## Implementation Constraints
- **No Caching**: Direct database queries for each request
- **No Pagination**: Return complete result sets
- **MySQL 8+ Only**: Target MySQL 8.0+ specifically using INFORMATION_SCHEMA
- **Read-Only**: Assume read-only usage patterns
- **No Complex Features**: Exclude stored procedures, functions, triggers from introspection
- **Performance Target**: Response times under 2 seconds for typical queries
- **Capacity Target**: Handle databases with up to 1000 tables efficiently

## Key Technical Decisions
- **ORM**: Use Dapper for lightweight data access, not Entity Framework
- **MCP Library**: MUST use ModelContextProtocol NuGet package (not custom implementation)
- **MySQL Driver**: MySqlConnector (not MySQL.Data)
- **Logging**: Microsoft.Extensions.Logging for comprehensive error logging
- **Configuration**: Microsoft.Extensions.Configuration for settings management

## Error Handling Patterns
- Handle database connection failures gracefully with meaningful error messages
- Implement proper MCP protocol compliance with standard error responses
- Set reasonable query timeouts to prevent hanging
- Validate against common MySQL hosting environments

## Project Structure (To Be Created)
```
MySqlMcpServer/           # Main MCP server console application
├── Program.cs            # Entry point and MCP server setup
├── Tools/                # MCP tool implementations
├── Services/             # Database introspection services
└── Models/               # Data models for schema information

MySqlMcpClient/           # Sample client console application
├── Program.cs            # Client demonstration workflows
└── Examples/             # Common usage scenarios

Tests/                    # Unit and integration tests
└── MySqlMcpServer.Tests/ # Server component tests
```

## Sample Response Formats
All MCP tools return structured JSON responses as defined in the PRD, including:
- Schema lists with system/user schema distinction
- Table descriptions with columns, indexes, and foreign key information
- Proper data type mapping from MySQL to standardized formats
- Metadata like table counts, view counts, and constraint information