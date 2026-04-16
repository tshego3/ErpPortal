using System.Text.Json.Serialization;

namespace ErpPortal.Api.Core.Domain;

public sealed record Todo(
    [property: JsonPropertyName("id")]        int Id,
    [property: JsonPropertyName("todo")]      string TodoText,
    [property: JsonPropertyName("completed")] bool Completed,
    [property: JsonPropertyName("userId")]    int UserId
);

public sealed record TodosResponse(
    [property: JsonPropertyName("todos")] List<Todo> Todos,
    [property: JsonPropertyName("total")] int Total,
    [property: JsonPropertyName("skip")]  int Skip,
    [property: JsonPropertyName("limit")] int Limit
);
