using SalesOrders.Web.Data;
using SalesOrders.Web.Models;
using SalesOrders.Web.Services;

namespace SalesOrders.Tests;

public class OrderServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "salesorders-tests-" + Guid.NewGuid().ToString("N"));
    private readonly string _file;
    private readonly OrderService _service;

    public OrderServiceTests()
    {
        _file = Path.Combine(_dir, "orders.xml");
        // A fresh XML store seeded with the spec's two example orders (SO625144, SO625145).
        _service = new OrderService(new XmlOrderStore(_file));
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    private static OrderHeader Header(string number = "SO900001") => new()
    {
        OrderNumber = number,
        OrderType = OrderType.Staff,
        OrderStatus = OrderStatus.New,
        CustomerName = "Steers",
        CreateDate = new DateTime(2024, 5, 1, 9, 30, 0)
    };

    private static OrderLine Line(string code = "ABC123", int qty = 3) => new()
    {
        ProductCode = code,
        ProductType = ProductType.Motor,
        CostPrice = 10.50m,
        SalesPrice = 20.25m,
        Quantity = qty
    };

    [Fact]
    public async Task CreateAsync_persists_order_and_numbers_lines_from_one()
    {
        await _service.CreateAsync(Header(), new[] { Line("p1"), Line("p2"), Line("p3") });

        // Read back through a brand-new store to prove it was written to the XML file.
        var reloaded = await new OrderService(new XmlOrderStore(_file)).GetAsync("SO900001");

        Assert.NotNull(reloaded);
        Assert.Equal("Steers", reloaded!.Header.CustomerName);
        Assert.Equal(OrderType.Staff, reloaded.Header.OrderType);
        Assert.Equal(new DateTime(2024, 5, 1, 9, 30, 0), reloaded.Header.CreateDate);
        Assert.Equal(new[] { 1, 2, 3 }, reloaded.Lines.Select(l => l.LineNumber));
        Assert.Equal(new[] { "P1", "P2", "P3" }, reloaded.Lines.Select(l => l.ProductCode));
    }

    [Fact]
    public async Task CreateAsync_rejects_duplicate_order_number_case_insensitively()
    {
        var ex = await Assert.ThrowsAsync<OrderValidationException>(
            () => _service.CreateAsync(Header("so625144"), Array.Empty<OrderLine>()));

        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task AddLineAsync_continues_after_highest_line_and_leaves_gaps()
    {
        // SO625145 is seeded with lines 1..3.
        await _service.DeleteLineAsync("SO625145", 2);
        var added = await _service.AddLineAsync("SO625145", Line("NEW001"));

        Assert.Equal(4, added.LineNumber);
        var order = await _service.GetAsync("SO625145");
        Assert.Equal(new[] { 1, 3, 4 }, order!.Lines.Select(l => l.LineNumber));
    }

    [Fact]
    public async Task AddLineAsync_to_order_without_lines_starts_at_one()
    {
        await _service.CreateAsync(Header(), Array.Empty<OrderLine>());

        var added = await _service.AddLineAsync("SO900001", Line());

        Assert.Equal(1, added.LineNumber);
    }

    [Fact]
    public async Task AddLineAsync_to_unknown_order_throws()
    {
        await Assert.ThrowsAsync<OrderValidationException>(() => _service.AddLineAsync("NOPE", Line()));
    }

    [Fact]
    public async Task UpdateAsync_keeps_existing_numbers_drops_removed_lines_and_appends_new_ones()
    {
        var order = (await _service.GetAsync("SO625145"))!;
        var header = order.Header;
        header.OrderStatus = OrderStatus.Processing;
        var kept = order.Lines.Where(l => l.LineNumber != 2).ToList(); // remove line 2
        kept[0].Quantity = 99;
        kept.Add(Line("FRESH1")); // LineNumber 0 => new

        await _service.UpdateAsync(header, kept);

        var updated = (await _service.GetAsync("SO625145"))!;
        Assert.Equal(OrderStatus.Processing, updated.Header.OrderStatus);
        Assert.Equal(new[] { 1, 3, 4 }, updated.Lines.Select(l => l.LineNumber));
        Assert.Equal(99, updated.Lines[0].Quantity);
        Assert.Equal("FRESH1", updated.Lines[2].ProductCode);
    }

    [Fact]
    public async Task UpdateLineAsync_replaces_the_matching_line()
    {
        var line = Line("CHANGED", qty: 42);
        line.LineNumber = 2;

        await _service.UpdateLineAsync("SO625144", line);

        var order = (await _service.GetAsync("SO625144"))!;
        Assert.Equal(2, order.Lines.Count);
        Assert.Equal("CHANGED", order.Lines[1].ProductCode);
        Assert.Equal(42, order.Lines[1].Quantity);
    }

    [Fact]
    public async Task DeleteAsync_removes_order_from_store()
    {
        await _service.DeleteAsync("SO625144");

        Assert.Null(await new OrderService(new XmlOrderStore(_file)).GetAsync("SO625144"));
        Assert.NotNull(await _service.GetAsync("SO625145"));
    }

    [Theory]
    [InlineData("kfc", null, null, null, new[] { "SO625144" })]
    [InlineData("SO6251", null, null, null, new[] { "SO625145", "SO625144" })]
    [InlineData(null, OrderType.Staff, null, null, new string[0])]
    [InlineData(null, null, "2018-03-09", null, new[] { "SO625145" })]
    [InlineData(null, null, null, "2018-03-08", new[] { "SO625144" })] // "To" includes the whole day
    public async Task SearchAsync_filters_by_text_type_and_date_range(
        string? text, OrderType? type, string? from, string? to, string[] expected)
    {
        var results = await _service.SearchAsync(new OrderSearchCriteria
        {
            OrderText = text,
            OrderType = type,
            FromDate = from is null ? null : DateTime.Parse(from),
            ToDate = to is null ? null : DateTime.Parse(to)
        });

        Assert.Equal(expected, results.Select(h => h.OrderNumber));
    }

    [Fact]
    public async Task FilterLines_matches_product_code_substring()
    {
        var order = (await _service.GetAsync("SO625145"))!;

        var lines = _service.FilterLines(order, "uyt");

        Assert.Single(lines);
        Assert.Equal("UYT485911", lines[0].ProductCode);
    }

    [Fact]
    public void XmlStore_writes_spec_shaped_xml()
    {
        var xml = File.ReadAllText(_file);

        Assert.Contains("<SalesOrder>", xml);
        Assert.Contains("<OrderHeader>", xml);
        Assert.Contains("<OrderNumber>SO625144</OrderNumber>", xml);
        Assert.Contains("<CreateDate>2018-03-08 13:52:21</CreateDate>", xml);
        Assert.Contains("<OrderLine>", xml);
        Assert.Contains("<CostPrice>13.54</CostPrice>", xml);
    }
}
