using System.Text.Json.Serialization;

namespace McpIntrospect.Core.Models;

public record ErrorResponse(string error);

[JsonSerializable(typeof(ErrorResponse))]
public partial class ErrorResponseSerializerContext: JsonSerializerContext {}
