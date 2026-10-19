using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesOrders.Web.Data;
using SalesOrders.Web.Models;

namespace SalesOrders.Web.Controllers;

[Authorize(Policy = Policies.CanEdit)]
public class UsersController : Controller
{
    private readonly IUserStore _users;

    public UsersController(IUserStore users)
    {
        _users = users;
    }

    public async Task<IActionResult> Index() => View(await _users.GetAllAsync());

    [HttpGet]
    public IActionResult Create() => View(new CreateUserViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (!Roles.All.Contains(model.Role))
        {
            ModelState.AddModelError(nameof(model.Role), "Select a valid role.");
        }
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _users.CreateAsync(model.Username.Trim(), model.Password, model.Role);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.Username), ex.Message);
            return View(model);
        }

        TempData["Message"] = $"User '{model.Username}' created.";
        return RedirectToAction(nameof(Index));
    }
}
