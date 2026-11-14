using ECommerceCsvImport.Models;

namespace ECommerceCsvImport.Services;

/// <summary>Потокобезопасное хранилище товаров в памяти процесса.</summary>
public sealed class InMemoryProductRepository : IProductRepository
{
    private readonly List<Product> _products = new();
    private readonly object _sync = new();

    public int Count
    {
        get
        {
            lock (_sync)
            {
                return _products.Count;
            }
        }
    }

    public void Add(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);
        lock (_sync)
        {
            _products.Add(product);
        }
    }

    public IReadOnlyList<Product> GetAll()
    {
        lock (_sync)
        {
            return _products.ToList();
        }
    }

    public Product? GetBySku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            return null;
        }

        lock (_sync)
        {
            return _products.FirstOrDefault(product =>
                string.Equals(product.Sku, sku.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }

    public void Clear()
    {
        lock (_sync)
        {
            _products.Clear();
        }
    }
}
