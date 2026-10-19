using SalesOrders.Web.Data;
using SalesOrders.Web.Models;

namespace SalesOrders.Web.Services;

/// <summary>Raised when a request breaks a business rule (duplicate order number, unknown order, ...).</summary>
public class OrderValidationException : Exception
{
    public OrderValidationException(string message) : base(message) { }
}

public interface IOrderService
{
    Task<IReadOnlyList<OrderHeader>> SearchAsync(OrderSearchCriteria criteria);
    Task<SalesOrder?> GetAsync(string orderNumber);
    Task<SalesOrder> CreateAsync(OrderHeader header, IEnumerable<OrderLine> lines);
    Task<SalesOrder> UpdateAsync(OrderHeader header, IEnumerable<OrderLine> lines);
    Task DeleteAsync(string orderNumber);

    Task<OrderLine> AddLineAsync(string orderNumber, OrderLine line);
    Task<OrderLine> UpdateLineAsync(string orderNumber, OrderLine line);
    Task DeleteLineAsync(string orderNumber, int lineNumber);

    IReadOnlyList<OrderLine> FilterLines(SalesOrder order, string? productCode);
}

public class OrderService : IOrderService
{
    private readonly IOrderStore _store;

    public OrderService(IOrderStore store)
    {
        _store = store;
    }

    public async Task<IReadOnlyList<OrderHeader>> SearchAsync(OrderSearchCriteria criteria)
    {
        IEnumerable<OrderHeader> headers = (await _store.GetAllAsync()).Select(o => o.Header);

        if (!string.IsNullOrWhiteSpace(criteria.OrderText))
        {
            var text = criteria.OrderText.Trim();
            headers = headers.Where(h =>
                h.OrderNumber.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                h.CustomerName.Contains(text, StringComparison.OrdinalIgnoreCase));
        }
        if (criteria.OrderType is { } type)
        {
            headers = headers.Where(h => h.OrderType == type);
        }
        if (criteria.FromDate is { } from)
        {
            headers = headers.Where(h => h.CreateDate >= from.Date);
        }
        if (criteria.ToDate is { } to)
        {
            // "To" is inclusive of the whole day.
            headers = headers.Where(h => h.CreateDate < to.Date.AddDays(1));
        }

        return headers.OrderByDescending(h => h.CreateDate).ToList();
    }

    public async Task<SalesOrder?> GetAsync(string orderNumber) =>
        Find(await _store.GetAllAsync(), orderNumber);

    public Task<SalesOrder> CreateAsync(OrderHeader header, IEnumerable<OrderLine> lines)
    {
        var newLines = lines.ToList();
        return _store.UpdateAsync(orders =>
        {
            header.OrderNumber = header.OrderNumber.Trim().ToUpperInvariant();
            if (Find(orders, header.OrderNumber) is not null)
            {
                throw new OrderValidationException($"Order '{header.OrderNumber}' already exists.");
            }

            var order = new SalesOrder { Header = header };
            foreach (var line in newLines)
            {
                AppendLine(order, line);
            }
            orders.Add(order);
            return order;
        });
    }

    public Task<SalesOrder> UpdateAsync(OrderHeader header, IEnumerable<OrderLine> lines)
    {
        var postedLines = lines.ToList();
        return _store.UpdateAsync(orders =>
        {
            var order = FindOrThrow(orders, header.OrderNumber);
            header.OrderNumber = order.Header.OrderNumber;
            order.Header = header;

            // Lines already on the order keep their number; new lines (LineNumber 0) continue the sequence.
            var existingNumbers = order.Lines.Select(l => l.LineNumber).ToHashSet();
            var kept = postedLines.Where(l => existingNumbers.Contains(l.LineNumber)).ToList();
            if (kept.Select(l => l.LineNumber).Distinct().Count() != kept.Count)
            {
                throw new OrderValidationException("Duplicate line numbers were submitted.");
            }

            var next = order.Lines.Count == 0 ? 1 : order.Lines.Max(l => l.LineNumber) + 1;
            order.Lines = kept;
            foreach (var line in postedLines.Where(l => !existingNumbers.Contains(l.LineNumber)))
            {
                line.ProductCode = Normalise(line.ProductCode);
                line.LineNumber = next++;
                order.Lines.Add(line);
            }
            kept.ForEach(l => l.ProductCode = Normalise(l.ProductCode));
            order.Lines.Sort((a, b) => a.LineNumber.CompareTo(b.LineNumber));
            return order;
        });
    }

    public Task DeleteAsync(string orderNumber) =>
        _store.UpdateAsync(orders =>
        {
            var order = FindOrThrow(orders, orderNumber);
            orders.Remove(order);
            return true;
        });

    public Task<OrderLine> AddLineAsync(string orderNumber, OrderLine line) =>
        _store.UpdateAsync(orders => AppendLine(FindOrThrow(orders, orderNumber), line));

    public Task<OrderLine> UpdateLineAsync(string orderNumber, OrderLine line) =>
        _store.UpdateAsync(orders =>
        {
            var order = FindOrThrow(orders, orderNumber);
            var index = order.Lines.FindIndex(l => l.LineNumber == line.LineNumber);
            if (index < 0)
            {
                throw new OrderValidationException($"Line {line.LineNumber} was not found on order '{orderNumber}'.");
            }
            line.ProductCode = Normalise(line.ProductCode);
            order.Lines[index] = line;
            return line;
        });

    public Task DeleteLineAsync(string orderNumber, int lineNumber) =>
        _store.UpdateAsync(orders =>
        {
            var order = FindOrThrow(orders, orderNumber);
            if (order.Lines.RemoveAll(l => l.LineNumber == lineNumber) == 0)
            {
                throw new OrderValidationException($"Line {lineNumber} was not found on order '{orderNumber}'.");
            }
            return true;
        });

    public IReadOnlyList<OrderLine> FilterLines(SalesOrder order, string? productCode) =>
        string.IsNullOrWhiteSpace(productCode)
            ? order.Lines
            : order.Lines.Where(l => l.ProductCode.Contains(productCode.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

    /// <summary>
    /// Line numbers auto-increment per order starting at 1 (highest existing + 1). Existing
    /// lines are never renumbered, so deleting a line leaves a gap rather than shifting the rest.
    /// </summary>
    private static OrderLine AppendLine(SalesOrder order, OrderLine line)
    {
        line.LineNumber = order.Lines.Count == 0 ? 1 : order.Lines.Max(l => l.LineNumber) + 1;
        line.ProductCode = Normalise(line.ProductCode);
        order.Lines.Add(line);
        return line;
    }

    private static string Normalise(string code) => code.Trim().ToUpperInvariant();

    private static SalesOrder? Find(IEnumerable<SalesOrder> orders, string orderNumber) =>
        orders.FirstOrDefault(o => string.Equals(o.Header.OrderNumber, orderNumber?.Trim(), StringComparison.OrdinalIgnoreCase));

    private static SalesOrder FindOrThrow(IEnumerable<SalesOrder> orders, string orderNumber) =>
        Find(orders, orderNumber) ?? throw new OrderValidationException($"Order '{orderNumber}' was not found.");
}
