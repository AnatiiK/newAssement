using System.Xml;
using System.Xml.Serialization;
using SalesOrders.Web.Models;

namespace SalesOrders.Web.Data;

/// <summary>
/// Stores orders in an XML file shaped like the spec's example
/// (&lt;SalesOrders&gt;&lt;SalesOrder&gt;&lt;OrderHeader/&gt;&lt;OrderLine/&gt;...).
/// Writes go to a temp file first and are then swapped in, so a crash mid-write
/// never leaves a truncated data file.
/// </summary>
public class XmlOrderStore : IOrderStore
{
    private static readonly XmlSerializer Serializer = new(typeof(SalesOrderDocument));
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly string _path;

    public XmlOrderStore(string path)
    {
        _path = path;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        if (!File.Exists(_path))
        {
            Write(new SalesOrderDocument { Orders = SeedData.Orders() });
        }
    }

    public async Task<IReadOnlyList<SalesOrder>> GetAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            return Read().Orders;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<T> UpdateAsync<T>(Func<List<SalesOrder>, T> mutate)
    {
        await _lock.WaitAsync();
        try
        {
            var doc = Read();
            var result = mutate(doc.Orders);
            Write(doc);
            return result;
        }
        finally
        {
            _lock.Release();
        }
    }

    private SalesOrderDocument Read()
    {
        using var stream = File.OpenRead(_path);
        return (SalesOrderDocument?)Serializer.Deserialize(stream) ?? new SalesOrderDocument();
    }

    private void Write(SalesOrderDocument doc)
    {
        var temp = _path + ".tmp";
        var settings = new XmlWriterSettings { Indent = true };
        using (var writer = XmlWriter.Create(temp, settings))
        {
            // Omit the default xsi/xsd namespace declarations for a cleaner file.
            var ns = new XmlSerializerNamespaces();
            ns.Add(string.Empty, string.Empty);
            Serializer.Serialize(writer, doc, ns);
        }
        File.Move(temp, _path, overwrite: true);
    }
}
