namespace ECommerceCsvImport.Models;

public sealed class ProductsIndexViewModel
{
    public IReadOnlyList<Product> Products { get; set; } = Array.Empty<Product>();
    public int TotalCount => Products.Count;
    public decimal TotalValue => Products.Sum(product => product.Price);
}
