using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Xml.Serialization;

namespace SalesOrders.Web.Models;

/// <summary>
/// Root of the XML data store: &lt;SalesOrders&gt;&lt;SalesOrder&gt;...&lt;/SalesOrder&gt;&lt;/SalesOrders&gt;.
/// </summary>
[XmlRoot("SalesOrders")]
public class SalesOrderDocument
{
    [XmlElement("SalesOrder")]
    public List<SalesOrder> Orders { get; set; } = new();
}

/// <summary>
/// One sales order: a header plus its lines, matching the XML example in the spec.
/// </summary>
public class SalesOrder
{
    [XmlElement("OrderHeader")]
    public OrderHeader Header { get; set; } = new();

    [XmlElement("OrderLine")]
    public List<OrderLine> Lines { get; set; } = new();
}

public class OrderHeader
{
    public const string DateFormat = "yyyy-MM-dd HH:mm:ss";

    [Required, Display(Name = "Order Number")]
    [StringLength(20, MinimumLength = 3)]
    [RegularExpression(@"^[A-Za-z0-9\-]+$", ErrorMessage = "Order Number may only contain letters, digits and dashes.")]
    public string OrderNumber { get; set; } = string.Empty;

    [Required, Display(Name = "Order Type")]
    public OrderType OrderType { get; set; }

    [Required, Display(Name = "Order Status")]
    public OrderStatus OrderStatus { get; set; }

    [Required, Display(Name = "Customer Name")]
    [StringLength(100, MinimumLength = 2)]
    public string CustomerName { get; set; } = string.Empty;

    [XmlIgnore]
    [Required, Display(Name = "Create Date")]
    [DataType(DataType.DateTime)]
    public DateTime CreateDate { get; set; } = DateTime.Now;

    /// <summary>Serialises CreateDate in the spec's "2018-03-08 13:52:21" format.</summary>
    [XmlElement("CreateDate")]
    public string CreateDateXml
    {
        get => CreateDate.ToString(DateFormat, CultureInfo.InvariantCulture);
        set => CreateDate = DateTime.ParseExact(value, DateFormat, CultureInfo.InvariantCulture);
    }
}

public class OrderLine
{
    [Display(Name = "Line #")]
    public int LineNumber { get; set; }

    [Required, Display(Name = "Product Code")]
    [StringLength(20, MinimumLength = 3)]
    [RegularExpression(@"^[A-Za-z0-9\-]+$", ErrorMessage = "Product Code may only contain letters, digits and dashes.")]
    public string ProductCode { get; set; } = string.Empty;

    [Required, Display(Name = "Product Type")]
    public ProductType ProductType { get; set; }

    [Required, Display(Name = "Cost Price")]
    [Range(typeof(decimal), "0.01", "9999999", ErrorMessage = "Cost Price must be between 0.01 and 9 999 999.")]
    public decimal CostPrice { get; set; }

    [Required, Display(Name = "Sales Price")]
    [Range(typeof(decimal), "0.01", "9999999", ErrorMessage = "Sales Price must be between 0.01 and 9 999 999.")]
    public decimal SalesPrice { get; set; }

    [Required]
    [Range(1, 1000000, ErrorMessage = "Quantity must be a whole number between 1 and 1 000 000.")]
    public int Quantity { get; set; }
}
