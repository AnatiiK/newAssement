using Microsoft.AspNetCore.Mvc;
using SalesOrders.Web.Models;
using SalesOrders.Web.Services;

namespace SalesOrders.Web.Controllers;

/// <summary>Calls the external Dog API and shows the returned image.</summary>
public class DogController : Controller
{
    private readonly DogApiClient _dogs;
    private readonly ILogger<DogController> _logger;

    public DogController(DogApiClient dogs, ILogger<DogController> logger)
    {
        _dogs = dogs;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? breed, CancellationToken ct)
    {
        var model = new DogViewModel { SelectedBreed = breed };
        try
        {
            model.Breeds = await _dogs.GetBreedsAsync(ct);
            if (!string.IsNullOrEmpty(breed) && !model.Breeds.Contains(breed))
            {
                model.Error = $"Unknown breed '{breed}'.";
                model.SelectedBreed = null;
                return View(model);
            }
            model.ImageUrl = await _dogs.GetRandomImageAsync(breed, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            _logger.LogWarning(ex, "Dog API call failed");
            model.Error = "Could not reach the Dog API. Please try again.";
        }
        return View(model);
    }
}
