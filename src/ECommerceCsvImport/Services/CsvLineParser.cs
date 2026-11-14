using System.Text;

namespace ECommerceCsvImport.Services;

/// <summary>Разбирает одну CSV-строку, сохраняя пустые поля и кавычки.</summary>
public static class CsvLineParser
{
    public static IReadOnlyList<string> Parse(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        var fields = new List<string>();
        var field = new StringBuilder();
        var insideQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];

            if (character == '"')
            {
                if (insideQuotes && index + 1 < line.Length && line[index + 1] == '"')
                {
                    field.Append('"');
                    index++;
                }
                else if (insideQuotes)
                {
                    insideQuotes = false;
                }
                else if (field.ToString().Trim().Length == 0)
                {
                    field.Clear();
                    insideQuotes = true;
                }
                else
                {
                    field.Append(character);
                }

                continue;
            }

            if (character == ',' && !insideQuotes)
            {
                fields.Add(field.ToString().Trim());
                field.Clear();
                continue;
            }

            field.Append(character);
        }

        fields.Add(field.ToString().Trim());
        return fields;
    }
}
