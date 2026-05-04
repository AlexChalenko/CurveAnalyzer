using System.Globalization;
using System.Text.Json;

namespace CurveAnalyzer.ApiServices.Data;

internal sealed class IssJsonTable
{
    private readonly JsonElement _data;
    private readonly IReadOnlyDictionary<string, int> _columns;

    private IssJsonTable(JsonElement data, IReadOnlyDictionary<string, int> columns)
    {
        _data = data;
        _columns = columns;
    }

    public static IssJsonTable? TryCreate(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var table) ||
            !table.TryGetProperty("columns", out var columnsElement) ||
            !table.TryGetProperty("data", out var dataElement) ||
            columnsElement.ValueKind != JsonValueKind.Array ||
            dataElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var columns = columnsElement
            .EnumerateArray()
            .Select((column, index) => new { Name = column.GetString(), Index = index })
            .Where(column => !string.IsNullOrWhiteSpace(column.Name))
            .ToDictionary(column => column.Name!, column => column.Index, StringComparer.OrdinalIgnoreCase);

        return new IssJsonTable(dataElement, columns);
    }

    public IEnumerable<JsonElement> Rows => _data.EnumerateArray();

    public string? GetString(JsonElement row, string column)
    {
        var value = GetValue(row, column);
        return value?.ValueKind switch
        {
            JsonValueKind.String => value.Value.GetString(),
            JsonValueKind.Number => value.Value.GetRawText(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            _ => null
        };
    }

    public DateTime? GetDate(JsonElement row, string column)
    {
        var value = GetString(row, column);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTime.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var exactDate))
        {
            return exactDate.Date;
        }

        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date.Date
            : null;
    }

    public double? GetDouble(JsonElement row, string column)
    {
        var value = GetValue(row, column);
        if (value is null || value.Value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.Value.ValueKind == JsonValueKind.Number && value.Value.TryGetDouble(out var number))
        {
            return number;
        }

        var text = GetString(row, column);
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number)
            ? number
            : null;
    }

    public int? GetInt(JsonElement row, string column)
    {
        var value = GetValue(row, column);
        if (value is null || value.Value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.Value.ValueKind == JsonValueKind.Number && value.Value.TryGetInt32(out var number))
        {
            return number;
        }

        var text = GetString(row, column);
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out number)
            ? number
            : null;
    }

    private JsonElement? GetValue(JsonElement row, string column)
    {
        if (!_columns.TryGetValue(column, out var index) ||
            row.ValueKind != JsonValueKind.Array ||
            row.GetArrayLength() <= index)
        {
            return null;
        }

        return row[index];
    }
}

internal readonly record struct IssCursor(int Index, int Total, int PageSize)
{
    public int NextStart => Index + PageSize;

    public static IssCursor Empty { get; } = new(0, 0, 0);

    public static IssCursor From(JsonElement root, string name)
    {
        var table = IssJsonTable.TryCreate(root, name);
        var row = table?.Rows.FirstOrDefault();

        return row is null
            ? Empty
            : new IssCursor(
                table!.GetInt(row.Value, "INDEX") ?? 0,
                table.GetInt(row.Value, "TOTAL") ?? 0,
                table.GetInt(row.Value, "PAGESIZE") ?? 0);
    }
}
