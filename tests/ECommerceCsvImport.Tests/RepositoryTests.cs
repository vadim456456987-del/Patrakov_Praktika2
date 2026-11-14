using ECommerceCsvImport.Models;
using ECommerceCsvImport.Services;

namespace ECommerceCsvImport.Tests;

public class RepositoryTests
{
    [Fact]
    public void Add_StoresProductAndIncrementsCount()
    {
        var repository = new InMemoryProductRepository();
        var product = new Product { Sku = "1001", Name = "Ноутбук", Price = 1000m };

        repository.Add(product);

        Assert.Equal(1, repository.Count);
        Assert.Same(product, Assert.Single(repository.GetAll()));
    }

    [Fact]
    public void GetBySku_IgnoresCaseAndSurroundingWhitespace()
    {
        var repository = new InMemoryProductRepository();
        var product = new Product { Sku = "AbC-1", Name = "Товар" };
        repository.Add(product);

        Assert.Same(product, repository.GetBySku("  abc-1  "));
    }

    [Fact]
    public void GetAll_ReturnsSnapshot()
    {
        var repository = new InMemoryProductRepository();
        repository.Add(new Product { Sku = "1", Name = "Первый" });
        var snapshot = repository.GetAll();

        repository.Add(new Product { Sku = "2", Name = "Второй" });

        Assert.Single(snapshot);
        Assert.Equal(2, repository.GetAll().Count);
    }

    [Fact]
    public void Clear_RemovesAllProducts()
    {
        var repository = new InMemoryProductRepository();
        repository.Add(new Product { Sku = "1", Name = "Товар" });

        repository.Clear();

        Assert.Equal(0, repository.Count);
        Assert.Empty(repository.GetAll());
    }
}
