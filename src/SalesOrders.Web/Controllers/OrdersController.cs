using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesOrders.Web.Models;
using SalesOrders.Web.Services;

namespace SalesOrders.Web.Controllers;

public class OrdersController : Controller
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders)
    {
        _orders = orders;
    }

    /// <summary>
    /// Header grid (searchable by order number/customer, type and date range) with the
    /// selected order's lines (searchable by product code) underneath.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(OrderSearchCriteria search, string? selected, string? productCode)
    {
        if (search.FromDate > search.ToDate)
        {
            ModelState.AddModelError("Search.ToDate", "\"Created To\" must be on or after \"Created From\".");
        }

        var model = new OrderIndexViewModel
        {
            Search = search,
            Orders = await _orders.SearchAsync(search),
            SelectedOrderNumber = selected,
            ProductCode = productCode
        };

        if (!string.IsNullOrWhiteSpace(selected))
        {
            model.SelectedOrder = await _orders.GetAsync(selected);
            if (model.SelectedOrder is not null)
            {
                model.SelectedLines = _orders.FilterLines(model.SelectedOrder, productCode);
            }
        }

        return View(model);
    }

    [HttpGet]
    [Authorize(Policy = Policies.CanEdit)]
    public IActionResult Create() =>
        View("Edit", new OrderEditViewModel
        {
            IsNew = true,
            Header = new OrderHeader { CreateDate = TrimToMinute(DateTime.Now) },
            Lines = { new OrderLine { Quantity = 1 } }
        });

    [HttpPost]
    [Authorize(Policy = Policies.CanEdit)]
    public async Task<IActionResult> Create(OrderEditViewModel model)
    {
        model.IsNew = true;
        if (!ModelState.IsValid)
        {
            return View("Edit", model);
        }

        try
        {
            var order = await _orders.CreateAsync(model.Header, model.Lines);
            TempData["Message"] = $"Order {order.Header.OrderNumber} created.";
            return RedirectToAction(nameof(Index), new { selected = order.Header.OrderNumber });
        }
        catch (OrderValidationException ex)
        {
            ModelState.AddModelError("Header.OrderNumber", ex.Message);
            return View("Edit", model);
        }
    }

    [HttpGet]
    [Authorize(Policy = Policies.CanEdit)]
    public async Task<IActionResult> Edit(string id)
    {
        var order = await _orders.GetAsync(id);
        if (order is null)
        {
            return NotFound();
        }
        return View(new OrderEditViewModel { Header = order.Header, Lines = order.Lines });
    }

    [HttpPost]
    [Authorize(Policy = Policies.CanEdit)]
    public async Task<IActionResult> Edit(OrderEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var order = await _orders.UpdateAsync(model.Header, model.Lines);
            TempData["Message"] = $"Order {order.Header.OrderNumber} updated.";
            return RedirectToAction(nameof(Index), new { selected = order.Header.OrderNumber });
        }
        catch (OrderValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [Authorize(Policy = Policies.CanEdit)]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            await _orders.DeleteAsync(id);
            TempData["Message"] = $"Order {id} deleted.";
        }
        catch (OrderValidationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Add (LineNumber 0) or update a single line from the popup on the grid.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.CanEdit)]
    public async Task<IActionResult> SaveLine(OrderLineEditModel model)
    {
        var returnUrl = SafeReturnUrl(model.ReturnUrl, model.OrderNumber);
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Line not saved: " + string.Join(" ",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return LocalRedirect(returnUrl);
        }

        try
        {
            if (model.Line.LineNumber == 0)
            {
                var line = await _orders.AddLineAsync(model.OrderNumber, model.Line);
                TempData["Message"] = $"Line {line.LineNumber} added to {model.OrderNumber}.";
            }
            else
            {
                await _orders.UpdateLineAsync(model.OrderNumber, model.Line);
                TempData["Message"] = $"Line {model.Line.LineNumber} on {model.OrderNumber} updated.";
            }
        }
        catch (OrderValidationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return LocalRedirect(returnUrl);
    }

    [HttpPost]
    [Authorize(Policy = Policies.CanEdit)]
    public async Task<IActionResult> DeleteLine(string orderNumber, int lineNumber, string? returnUrl)
    {
        try
        {
            await _orders.DeleteLineAsync(orderNumber, lineNumber);
            TempData["Message"] = $"Line {lineNumber} removed from {orderNumber}.";
        }
        catch (OrderValidationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return LocalRedirect(SafeReturnUrl(returnUrl, orderNumber));
    }

    private string SafeReturnUrl(string? returnUrl, string orderNumber) =>
        Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Action(nameof(Index), new { selected = orderNumber })!;

    private static DateTime TrimToMinute(DateTime value) =>
        new(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, value.Kind);
}
