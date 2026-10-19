using System.ComponentModel.DataAnnotations;

namespace SalesOrders.Web.Models;

public class OrderSearchCriteria
{
    /// <summary>Matches against Order Number or Customer Name.</summary>
    [Display(Name = "Order Number / Customer")]
    public string? OrderText { get; set; }

    [Display(Name = "Order Type")]
    public OrderType? OrderType { get; set; }

    [Display(Name = "Created From"), DataType(DataType.Date)]
    public DateTime? FromDate { get; set; }

    [Display(Name = "Created To"), DataType(DataType.Date)]
    public DateTime? ToDate { get; set; }
}

public class OrderIndexViewModel
{
    public OrderSearchCriteria Search { get; set; } = new();
    public IReadOnlyList<OrderHeader> Orders { get; set; } = Array.Empty<OrderHeader>();

    public string? SelectedOrderNumber { get; set; }
    public SalesOrder? SelectedOrder { get; set; }

    [Display(Name = "Product Code")]
    public string? ProductCode { get; set; }
    public IReadOnlyList<OrderLine> SelectedLines { get; set; } = Array.Empty<OrderLine>();
}

/// <summary>Header + lines captured together on the Create / Edit screen.</summary>
public class OrderEditViewModel
{
    public bool IsNew { get; set; }
    public OrderHeader Header { get; set; } = new();
    public List<OrderLine> Lines { get; set; } = new();
}

/// <summary>Posted from the order line popup on the Orders grid.</summary>
public class OrderLineEditModel
{
    [Required]
    public string OrderNumber { get; set; } = string.Empty;

    public OrderLine Line { get; set; } = new();

    // Preserve the grid's search state across the post/redirect.
    public string? ReturnUrl { get; set; }
}

public class LoginViewModel
{
    [Required, StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class CreateUserViewModel
{
    [Required, StringLength(50, MinimumLength = 3)]
    [RegularExpression(@"^[A-Za-z0-9_.\-]+$", ErrorMessage = "Username may only contain letters, digits, '.', '_' and '-'.")]
    public string Username { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Display(Name = "Confirm Password")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = Models.Roles.Guest;
}

public class AppUser
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = Models.Roles.Guest;
}

public class DogViewModel
{
    public IReadOnlyList<string> Breeds { get; set; } = Array.Empty<string>();
    public string? SelectedBreed { get; set; }
    public string? ImageUrl { get; set; }
    public string? Error { get; set; }
}
