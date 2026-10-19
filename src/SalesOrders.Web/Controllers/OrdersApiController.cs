using Microsoft.AspNetCore.Mvc;
using SalesOrders.Web.Services;

namespace SalesOrders.Web.Controllers;

/// <summary>
/// REST endpoint returning an order's header and lines. Requires a signed-in user
/// (either role); unauthenticated callers receive 401.
/// </summary>
[ApiController]
[Route("api/orders")]
public class OrdersApiController : ControllerBase
{
    private readonly IOrderService _orders;

    public OrdersApiController(IOrderService orders)
    {
        _orders = orders;
    }

    /// <summary>GET /api/orders/SO625144</summary>
    [HttpGet("{orderNumber}")]
    public Task<IActionResult> Get(string orderNumber) => GetOrder(orderNumber);

    /// <summary>GET /api/orders?orderNumber=SO625144</summary>
    [HttpGet]
    public Task<IActionResult> GetByQuery([FromQuery] string? orderNumber) => GetOrder(orderNumber);

    private async Task<IActionResult> GetOrder(string? orderNumber)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            return BadRequest(new { error = "orderNumber is required." });
        }

        var order = await _orders.GetAsync(orderNumber);
        if (order is null)
        {
            return NotFound(new { error = $"Order '{orderNumber}' was not found." });
        }

        return Ok(new
        {
            orderHeader = new
            {
                order.Header.OrderNumber,
                OrderType = order.Header.OrderType.ToString(),
                OrderStatus = order.Header.OrderStatus.ToString(),
                order.Header.CustomerName,
                order.Header.CreateDate
            },
            orderLines = order.Lines.Select(l => new
            {
                l.LineNumber,
                l.ProductCode,
                ProductType = l.ProductType.ToString(),
                l.CostPrice,
                l.SalesPrice,
                l.Quantity
            })
        });
    }
}
