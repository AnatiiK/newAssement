namespace SalesOrders.Web.Models;

public enum OrderType
{
    Normal,
    Staff,
    Mechanical,
    Perishable
}

public enum OrderStatus
{
    New,
    Processing,
    Complete
}

public enum ProductType
{
    Apparel,
    Parts,
    Equipment,
    Motor
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string Guest = "Guest";

    public static readonly string[] All = { Admin, Guest };
}

public static class Policies
{
    /// <summary>Add / edit / delete orders, lines and users. Admins only.</summary>
    public const string CanEdit = nameof(CanEdit);
}
