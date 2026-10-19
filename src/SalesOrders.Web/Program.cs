using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using SalesOrders.Web.Data;
using SalesOrders.Web.Models;
using SalesOrders.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Every MVC action requires a signed-in user unless marked [AllowAnonymous].
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter());
});

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        // API callers get status codes, not redirects to the login page.
        options.Events.OnRedirectToLogin = ctx => RedirectOrStatus(ctx, StatusCodes.Status401Unauthorized);
        options.Events.OnRedirectToAccessDenied = ctx => RedirectOrStatus(ctx, StatusCodes.Status403Forbidden);
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.CanEdit, p => p.RequireRole(Roles.Admin));
});

var dataDir = Path.Combine(builder.Environment.ContentRootPath,
    builder.Configuration["DataStore:Directory"] ?? "App_Data");

builder.Services.AddSingleton<IOrderStore>(_ =>
    new XmlOrderStore(Path.Combine(dataDir, builder.Configuration["DataStore:OrdersFile"] ?? "orders.xml")));
builder.Services.AddSingleton<IUserStore>(_ =>
    new JsonUserStore(Path.Combine(dataDir, builder.Configuration["DataStore:UsersFile"] ?? "users.json"), new[]
    {
        ("admin", "Admin@123", Roles.Admin),
        ("guest", "Guest@123", Roles.Guest)
    }));
builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.AddHttpClient<DogApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["DogApi:BaseUrl"] ?? "https://dog.ceo/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();

// Create (and seed) the data files at startup rather than on first use.
app.Services.GetRequiredService<IOrderStore>();
app.Services.GetRequiredService<IUserStore>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Fixed culture so prices always use "." as the decimal separator, matching the XML store
// and client-side validation.
var culture = new CultureInfo("en-US");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culture),
    SupportedCultures = new[] { culture },
    SupportedUICultures = new[] { culture }
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Orders}/{action=Index}/{id?}");

app.Run();

static Task RedirectOrStatus(Microsoft.AspNetCore.Authentication.RedirectContext<CookieAuthenticationOptions> ctx, int status)
{
    if (ctx.Request.Path.StartsWithSegments("/api"))
    {
        ctx.Response.StatusCode = status;
    }
    else
    {
        ctx.Response.Redirect(ctx.RedirectUri);
    }
    return Task.CompletedTask;
}
