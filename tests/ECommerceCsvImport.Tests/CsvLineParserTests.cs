using ECommerceCsvImport.Services;

namespace ECommerceCsvImport.Tests;

public class CsvLineParserTests
{
    [Fact]
    public void Parse_SplitsSimpleCsvRow()
    {
        var fields = CsvLineParser.Parse("1001,Ноутбук,45990");

        Assert.Equal(new[] { "1001", "Ноутбук", "45990" }, fields);
    }

    [Fact]
    public void Parse_PreservesEmptyFields()
    {
        var fields = CsvLineParser.Parse("1001,,4990,");

        Assert.Equal(new[] { "1001", "", "4990", "" }, fields);
    }

    [Fact]
    public void Parse_DoesNotSplitCommaInsideQuotes()
    {
        var fields = CsvLineParser.Parse("1001,\"Ноутбук, игровой\",59990");

        Assert.Equal(new[] { "1001", "Ноутбук, игровой", "59990" }, fields);
    }

    [Fact]
    public void Parse_UnescapesDoubleQuotesInsideQuotedField()
    {
        var fields = CsvLineParser.Parse("1013,\"Название \"\"Special\"\"\",Описание");

        Assert.Equal(new[] { "1013", "Название \"Special\"", "Описание" }, fields);
    }

    [Fact]
    public void Parse_TrimsFields()
    {
        var fields = CsvLineParser.Parse(" 1001 , Ноутбук , 45990 ");

        Assert.Equal(new[] { "1001", "Ноутбук", "45990" }, fields);
    }
}
