using System.Text.Json.Serialization;

namespace MySqlIntrospect.Core.Models;

public record ErrorResponse(string error);

[JsonSerializable(typeof(ErrorResponse))]
public partial class ErrorResponseSerializerContext: JsonSerializerContext {}
