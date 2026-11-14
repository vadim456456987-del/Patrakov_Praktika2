using System.Text;
using ECommerceCsvImport.Models;
using ECommerceCsvImport.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceCsvImport.Controllers;

public sealed class ProductsController : Controller
{
    private readonly IProductRepository _repository;
    private readonly IProductImportService _importService;
    private readonly IWebHostEnvironment _environment;

    public ProductsController(
        IProductRepository repository,
        IProductImportService importService,
        IWebHostEnvironment environment)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _importService = importService ?? throw new ArgumentNullException(nameof(importService));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new ProductsIndexViewModel { Products = _repository.GetAll() });
    }

    [HttpGet]
    public IActionResult Import()
    {
        return View(new ImportViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(string? filePath, IFormFile? csvFile)
    {
        ImportResult result;
        if (csvFile is { Length: > 0 })
        {
            await using var stream = csvFile.OpenReadStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var content = await reader.ReadToEndAsync();
            result = _importService.ImportFromLines(content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None));
        }
        else
        {
            var resolvedPath = ResolveFilePath(filePath);
            result = string.IsNullOrWhiteSpace(resolvedPath)
                ? new ImportResult
                {
                    Errors = { new ImportError { RowNumber = 0, Message = "Укажите путь к CSV-файлу или выберите файл" } }
                }
                : _importService.ImportFromCsv(resolvedPath);
        }

        return View("Import", new ImportViewModel { FilePath = filePath ?? string.Empty, Result = result });
    }

    private string? ResolveFilePath(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var trimmed = input.Trim();
        if (Path.IsPathRooted(trimmed) && !trimmed.StartsWith('/'))
        {
            return trimmed;
        }

        var relativePath = trimmed.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var rootPath = Path.GetFullPath(webRoot);
        var candidate = Path.GetFullPath(Path.Combine(rootPath, relativePath));
        var rootWithSeparator = rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? rootPath
            : rootPath + Path.DirectorySeparatorChar;

        return candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase)
            ? candidate
            : null;
    }
}
