# MySQL MCP Server

A Model Context Protocol (MCP) server that provides MySQL database schema introspection capabilities to LLM clients.

## Features

- **Schema Introspection**: Query MySQL database schemas, tables, columns, indexes, and relationships
- **MCP Protocol Compliance**: Implements MCP over JSON-RPC via stdio transport
- **MySQL 8+ Support**: Built specifically for MySQL 8.0+ using INFORMATION_SCHEMA
- **Lightweight**: Uses Dapper ORM for efficient database queries

## Quick Start

### Prerequisites

- .NET 9 or later
- MySQL 8.0+ database
- Valid MySQL connection string

### Configuration

1. Set the MySQL connection string via environment variable:
   ```bash
   export MCP_MySQL_ConnectionString="Server=localhost;Database=testdb;Uid=root;Pwd=password;"
   ```

   Or update `appsettings.json` in the MySqlMcpServer project.

### Build and Run

```bash
# Build the solution
dotnet build

# Run the MCP server
dotnet run --project MySqlMcpServer

# Run the sample client (demonstration only)
dotnet run --project MySqlMcpClient
```

## Available MCP Tools

The server exposes these introspection tools:

- `list_schemas` - Lists all accessible database schemas
- `describe_schema` - Returns detailed schema information including table/view counts
- `list_tables` - Returns table names and metadata for a schema
- `describe_table` - Comprehensive table info (columns, indexes, foreign keys)
- `list_columns` - Column information with data types and constraints
- `list_indexes` - Index information including type and uniqueness
- `list_foreign_keys` - Foreign key relationships
- `list_views` - View definitions and metadata

## Example Usage

Once the server is running, you can send MCP JSON-RPC requests:

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "list_schemas"
  }
}
```

## Project Structure

```
MySqlMcpServer/           # Main MCP server console application
├── Program.cs            # Entry point and MCP server setup
├── Tools/                # MCP tool implementations
│   └── MySqlIntrospectionTools.cs
├── Services/             # Database introspection services
│   └── MySqlIntrospectionService.cs
├── Models/               # Data models for schema information
│   ├── SchemaInfo.cs
│   └── ServerConfig.cs
└── appsettings.json      # Configuration

MySqlMcpClient/           # Sample client console application
└── Program.cs            # Client demonstration

Directory.Packages.props  # Central package management
```

## Dependencies

- **ModelContextProtocol** (v0.3.0-preview.4) - MCP protocol implementation
- **Dapper** (v2.1.35) - Lightweight ORM for MySQL queries
- **MySqlConnector** (v2.3.5) - MySQL database driver
- **Microsoft.Extensions.*** - Configuration, logging, and dependency injection

## Testing

The server can be tested by:

1. Starting the server: `dotnet run --project MySqlMcpServer`
2. Connecting via any MCP-compatible client
3. Calling the available tools to introspect your MySQL database

## License

This project demonstrates MySQL MCP server implementation for educational and development purposes.