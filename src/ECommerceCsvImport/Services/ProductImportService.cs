using System.Globalization;
using ECommerceCsvImport.Models;

namespace ECommerceCsvImport.Services;

/// <summary>Читает CSV, проверяет записи варианта «Электроника» и сохраняет корректные товары.</summary>
public sealed class ProductImportService : IProductImportService
{
    private const string DefaultCurrency = "RUB";
    private const string DefaultCategory = "Без категории";
    private const int MaxWarrantyMonths = 60;

    private static readonly string[] RequiredColumns =
    {
        "sku", "name", "description", "price", "currency", "category_name",
        "stock", "image_url", "is_active", "brand", "warranty_months", "model_number"
    };

    private readonly IProductRepository _repository;

    public ProductImportService(IProductRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public ImportResult ImportFromCsv(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return FileError("Файл не найден");
        }

        try
        {
            return ImportFromLines(File.ReadAllLines(filePath));
        }
        catch (IOException)
        {
            return FileError("Не удалось прочитать файл");
        }
        catch (UnauthorizedAccessException)
        {
            return FileError("Нет доступа к файлу");
        }
    }

    public ImportResult ImportFromLines(IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        var source = lines.ToList();
        var result = new ImportResult();
        var headerIndex = source.FindIndex(line => !string.IsNullOrWhiteSpace(line));

        if (headerIndex < 0)
        {
            result.Errors.Add(new ImportError { RowNumber = 0, Message = "Данные для импорта отсутствуют" });
            return result;
        }

        var headerFields = CsvLineParser.Parse(source[headerIndex]);
        var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < headerFields.Count; index++)
        {
            var columnName = headerFields[index].Trim().TrimStart('\uFEFF');
            if (!columnMap.ContainsKey(columnName))
            {
                columnMap.Add(columnName, index);
            }
        }

        if (RequiredColumns.Any(column => !columnMap.ContainsKey(column)))
        {
            result.Errors.Add(new ImportError
            {
                RowNumber = headerIndex + 1,
                Message = "Неверные заголовки CSV"
            });
            return result;
        }

        var importedSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var lineIndex = headerIndex + 1; lineIndex < source.Count; lineIndex++)
        {
            var line = source[lineIndex];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            result.TotalRows++;
            var rowNumber = lineIndex + 1;
            var values = CsvLineParser.Parse(line);
            if (values.Count != headerFields.Count)
            {
                result.Errors.Add(new ImportError
                {
                    RowNumber = rowNumber,
                    Message = "Неверное количество полей в строке"
                });
                continue;
            }

            string Value(string column) => values[columnMap[column]].Trim();

            var rowErrors = new List<ImportError>();
            void AddError(string message) => rowErrors.Add(new ImportError { RowNumber = rowNumber, Message = message });

            var sku = Value("sku");
            if (string.IsNullOrWhiteSpace(sku)) AddError("Поле sku не может быть пустым");

            var name = Value("name");
            if (string.IsNullOrWhiteSpace(name)) AddError("Поле name не может быть пустым");

            var priceValid = decimal.TryParse(Value("price"), NumberStyles.Number, CultureInfo.InvariantCulture, out var price)
                && price > 0;
            if (!priceValid) AddError("Поле price должно быть числом больше 0");

            var stockValid = int.TryParse(Value("stock"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var stock)
                && stock >= 0;
            if (!stockValid) AddError("Остаток на складе должен быть целым числом не меньше 0");

            var activeText = Value("is_active");
            var activeValid = TryParseActive(activeText, out var isActive);
            if (!activeValid) AddError("Поле is_active должно иметь значение true, false, 1 или 0");

            var brand = Value("brand");
            if (string.IsNullOrWhiteSpace(brand)) AddError("Поле brand обязательно для варианта «Электроника»");

            var warrantyValid = int.TryParse(Value("warranty_months"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var warrantyMonths)
                && warrantyMonths >= 0
                && warrantyMonths <= MaxWarrantyMonths;
            if (!warrantyValid) AddError("Гарантия должна быть целым числом от 0 до 60 месяцев");

            if (rowErrors.Count > 0)
            {
                result.Errors.AddRange(rowErrors);
                continue;
            }

            if (_repository.GetBySku(sku) is not null || !importedSkus.Add(sku))
            {
                result.Errors.Add(new ImportError { RowNumber = rowNumber, Message = "Артикул товара уже существует" });
                continue;
            }

            var currency = Value("currency");
            var category = Value("category_name");
            var product = new Product
            {
                Sku = sku,
                Name = name,
                Description = Value("description"),
                Price = price,
                Currency = string.IsNullOrWhiteSpace(currency) ? DefaultCurrency : currency.ToUpperInvariant(),
                CategoryName = string.IsNullOrWhiteSpace(category) ? DefaultCategory : category,
                StockQuantity = stock,
                ImageUrl = Value("image_url"),
                IsActive = isActive,
                Brand = brand,
                WarrantyMonths = warrantyMonths,
                ModelNumber = Value("model_number")
            };

            _repository.Add(product);
            result.ImportedCount++;
        }

        if (result.TotalRows == 0 && result.Errors.Count == 0)
        {
            result.Errors.Add(new ImportError { RowNumber = 0, Message = "Данные для импорта отсутствуют" });
        }

        return result;
    }

    private static bool TryParseActive(string value, out bool isActive)
    {
        if (string.Equals(value, "1", StringComparison.OrdinalIgnoreCase))
        {
            isActive = true;
            return true;
        }

        if (string.Equals(value, "0", StringComparison.OrdinalIgnoreCase))
        {
            isActive = false;
            return true;
        }

        return bool.TryParse(value, out isActive);
    }

    private static ImportResult FileError(string message)
    {
        var result = new ImportResult();
        result.Errors.Add(new ImportError { RowNumber = 0, Message = message });
        return result;
    }
}
