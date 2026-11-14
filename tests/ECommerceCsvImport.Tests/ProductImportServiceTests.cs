using ECommerceCsvImport.Models;
using ECommerceCsvImport.Services;

namespace ECommerceCsvImport.Tests;

public class ProductImportServiceTests
{
    private const string Header = "sku,name,description,price,currency,category_name,stock,image_url,is_active,brand,warranty_months,model_number";
    private const string ValidRow = "1001,Ноутбук Lenovo,Для работы,45990,,Компьютеры,5,/images/laptop.jpg,true,Lenovo,24,IdeaPad3";

    [Fact]
    public void ImportFromLines_ImportsValidProductAndAppliesDefaults()
    {
        var repository = new InMemoryProductRepository();
        var service = new ProductImportService(repository);

        var result = service.ImportFromLines(new[] { Header, ValidRow });
        var product = Assert.Single(repository.GetAll());

        Assert.Equal(1, result.TotalRows);
        Assert.Equal(1, result.ImportedCount);
        Assert.Empty(result.Errors);
        Assert.Equal("1001", product.Sku);
        Assert.Equal("RUB", product.Currency);
        Assert.Equal("Компьютеры", product.CategoryName);
        Assert.Equal("Lenovo", product.Brand);
        Assert.Equal(24, product.WarrantyMonths);
        Assert.Equal("Ноутбук Lenovo", product.Name);
    }

    [Theory]
    [InlineData("-10", "Поле price должно быть числом больше 0")]
    [InlineData("abc", "Поле price должно быть числом больше 0")]
    public void ImportFromLines_RejectsInvalidPrice(string price, string expectedMessage)
    {
        var result = ImportOne(ValidRow.Replace(",45990,", $",{price},"));

        Assert.Equal(0, result.ImportedCount);
        Assert.Contains(result.Errors, error => error.RowNumber == 2 && error.Message == expectedMessage);
    }

    [Fact]
    public void ImportFromLines_RejectsNegativeStock()
    {
        var result = ImportOne(ValidRow.Replace(",5,", ",-5,"));

        Assert.Contains(result.Errors, error => error.Message.Contains("Остаток на складе"));
        Assert.Equal(0, result.ImportedCount);
    }

    [Theory]
    [InlineData("yes", "Поле is_active должно иметь значение true, false, 1 или 0")]
    [InlineData("false", "")]
    [InlineData("1", "")]
    [InlineData("0", "")]
    public void ImportFromLines_ParsesSupportedActiveValues(string active, string expectedError)
    {
        var repository = new InMemoryProductRepository();
        var row = ValidRow.Replace(",true,Lenovo,", $",{active},Lenovo,");
        var result = new ProductImportService(repository).ImportFromLines(new[] { Header, row });

        if (expectedError.Length > 0)
        {
            Assert.Contains(result.Errors, error => error.Message == expectedError);
            Assert.Equal(0, result.ImportedCount);
        }
        else
        {
            Assert.Equal(1, result.ImportedCount);
        }
    }

    [Fact]
    public void ImportFromLines_RejectsMissingBrandAndOutOfRangeWarranty()
    {
        var row = ValidRow.Replace(",Lenovo,24,", ",,61,");
        var result = ImportOne(row);

        Assert.Equal(2, result.ErrorCount);
        Assert.Contains(result.Errors, error => error.Message.Contains("brand обязательно"));
        Assert.Contains(result.Errors, error => error.Message.Contains("от 0 до 60"));
        Assert.Equal(0, result.ImportedCount);
    }

    [Fact]
    public void ImportFromLines_ReportsDuplicateSkuInsideTheFile()
    {
        var duplicate = ValidRow.Replace("Ноутбук Lenovo", "Ноутбук ASUS").Replace("Lenovo,24", "ASUS,24");
        var result = new ProductImportService(new InMemoryProductRepository())
            .ImportFromLines(new[] { Header, ValidRow, duplicate });

        Assert.Equal(2, result.TotalRows);
        Assert.Equal(1, result.ImportedCount);
        Assert.Contains(result.Errors, error => error.RowNumber == 3 && error.Message.Contains("уже существует"));
    }

    [Fact]
    public void ImportFromLines_ReportsDuplicateAlreadyInRepository()
    {
        var repository = new InMemoryProductRepository();
        repository.Add(new Product { Sku = "1001", Name = "Уже есть" });
        var result = new ProductImportService(repository).ImportFromLines(new[] { Header, ValidRow });

        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(1, repository.Count);
        Assert.Contains(result.Errors, error => error.Message.Contains("уже существует"));
    }

    [Fact]
    public void ImportFromLines_ReportsHeaderAndColumnCountErrors()
    {
        var repository = new InMemoryProductRepository();
        var service = new ProductImportService(repository);
        var badHeader = service.ImportFromLines(new[] { "sku,name,price" });
        var badRow = service.ImportFromLines(new[] { Header, "1001,Товар" });

        Assert.Contains(badHeader.Errors, error => error.Message == "Неверные заголовки CSV");
        Assert.Contains(badRow.Errors, error => error.Message == "Неверное количество полей в строке");
        Assert.Equal(0, badRow.ImportedCount);
    }

    [Fact]
    public void ImportFromLines_EmptyInputAndHeaderOnlyProduceHelpfulError()
    {
        var service = new ProductImportService(new InMemoryProductRepository());
        var empty = service.ImportFromLines(Array.Empty<string>());
        var headerOnly = service.ImportFromLines(new[] { Header });

        Assert.Contains(empty.Errors, error => error.Message == "Данные для импорта отсутствуют");
        Assert.Contains(headerOnly.Errors, error => error.Message == "Данные для импорта отсутствуют");
    }

    [Fact]
    public void ImportFromCsv_MissingFileReturnsErrorInsteadOfThrowing()
    {
        var result = new ProductImportService(new InMemoryProductRepository())
            .ImportFromCsv(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".csv"));

        Assert.Equal(0, result.ImportedCount);
        Assert.Contains(result.Errors, error => error.Message == "Файл не найден");
    }

    [Fact]
    public void ImportFromLines_ParsesQuotedCommaAndRussianText()
    {
        var row = "1002,\"Ноутбук, игровой\",Тест,60000,RUB,,1,,true,ASUS,36,VivoBook";
        var repository = new InMemoryProductRepository();
        var result = new ProductImportService(repository).ImportFromLines(new[] { Header, row });
        var product = Assert.Single(repository.GetAll());

        Assert.Equal(1, result.ImportedCount);
        Assert.Equal("Ноутбук, игровой", product.Name);
        Assert.Equal("Без категории", product.CategoryName);
    }

    private static ImportResult ImportOne(string row)
    {
        return new ProductImportService(new InMemoryProductRepository())
            .ImportFromLines(new[] { Header, row });
    }
}
