using SalesOrders.Web.Models;

namespace SalesOrders.Web.Data;

/// <summary>The two example orders from the spec, used to seed an empty data store.</summary>
public static class SeedData
{
    public static List<SalesOrder> Orders() => new()
    {
        new SalesOrder
        {
            Header = new OrderHeader
            {
                OrderNumber = "SO625144",
                OrderType = OrderType.Normal,
                OrderStatus = OrderStatus.New,
                CustomerName = "KFC",
                CreateDate = new DateTime(2018, 3, 8, 13, 52, 21)
            },
            Lines =
            {
                new OrderLine { LineNumber = 1, ProductCode = "GSX837420", ProductType = ProductType.Parts, CostPrice = 13.54m, SalesPrice = 84.49m, Quantity = 10 },
                new OrderLine { LineNumber = 2, ProductCode = "AVF697420", ProductType = ProductType.Apparel, CostPrice = 19.34m, SalesPrice = 105.99m, Quantity = 25 }
            }
        },
        new SalesOrder
        {
            Header = new OrderHeader
            {
                OrderNumber = "SO625145",
                OrderType = OrderType.Normal,
                OrderStatus = OrderStatus.New,
                CustomerName = "Nandos",
                CreateDate = new DateTime(2018, 3, 9, 8, 56, 21)
            },
            Lines =
            {
                new OrderLine { LineNumber = 1, ProductCode = "YTS786663", ProductType = ProductType.Parts, CostPrice = 8.44m, SalesPrice = 49.22m, Quantity = 2 },
                new OrderLine { LineNumber = 2, ProductCode = "UYT485911", ProductType = ProductType.Equipment, CostPrice = 41.57m, SalesPrice = 250.45m, Quantity = 12 },
                new OrderLine { LineNumber = 3, ProductCode = "PLO879996", ProductType = ProductType.Apparel, CostPrice = 21.57m, SalesPrice = 65.45m, Quantity = 8 }
            }
        }
    };
}
