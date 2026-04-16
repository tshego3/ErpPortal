using System.Text.Json.Serialization;

namespace ErpPortal.Api.Core.Domain;

public sealed record Product(
    [property: JsonPropertyName("id")]          int Id,
    [property: JsonPropertyName("title")]       string Title,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("price")]       decimal Price,
    [property: JsonPropertyName("brand")]       string? Brand,
    [property: JsonPropertyName("category")]    string Category,
    [property: JsonPropertyName("thumbnail")]   string Thumbnail
);

public sealed record ProductsResponse(
    [property: JsonPropertyName("products")] List<Product> Products,
    [property: JsonPropertyName("total")]    int Total,
    [property: JsonPropertyName("skip")]     int Skip,
    [property: JsonPropertyName("limit")]    int Limit
);
