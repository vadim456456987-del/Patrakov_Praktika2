namespace ECommerceCsvImport.Models;

public sealed class ImportViewModel
{
    public string FilePath { get; set; } = "/csv/sample_products.csv";
    public ImportResult? Result { get; set; }
}
