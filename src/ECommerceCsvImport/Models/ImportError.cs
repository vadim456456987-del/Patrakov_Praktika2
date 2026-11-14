namespace ECommerceCsvImport.Models;

public sealed class ImportError
{
    public int RowNumber { get; set; }
    public string Message { get; set; } = string.Empty;
}
