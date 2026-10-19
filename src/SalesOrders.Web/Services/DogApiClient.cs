using System.Text.Json.Serialization;

namespace SalesOrders.Web.Services;

/// <summary>Typed HttpClient for https://dog.ceo/dog-api/.</summary>
public class DogApiClient
{
    private readonly HttpClient _http;

    public DogApiClient(HttpClient http)
    {
        _http = http;
    }

    /// <summary>Returns a random image URL, optionally restricted to one breed.</summary>
    public async Task<string> GetRandomImageAsync(string? breed, CancellationToken ct = default)
    {
        var path = string.IsNullOrWhiteSpace(breed)
            ? "api/breeds/image/random"
            : $"api/breed/{Uri.EscapeDataString(breed)}/images/random";

        var response = await _http.GetFromJsonAsync<DogResponse<string>>(path, ct);
        if (response is null || response.Status != "success" || string.IsNullOrEmpty(response.Message))
        {
            throw new HttpRequestException("Dog API did not return an image.");
        }
        return response.Message;
    }

    public async Task<IReadOnlyList<string>> GetBreedsAsync(CancellationToken ct = default)
    {
        var response = await _http.GetFromJsonAsync<DogResponse<Dictionary<string, string[]>>>("api/breeds/list/all", ct);
        return response?.Message?.Keys.OrderBy(b => b).ToList() ?? new List<string>();
    }

    private sealed class DogResponse<T>
    {
        [JsonPropertyName("message")] public T? Message { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
    }
}
