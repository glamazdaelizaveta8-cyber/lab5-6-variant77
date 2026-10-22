using System.Text.Json;

namespace SpreadsheetLib.Model;

/// <summary>Збереження/завантаження таблиці у власному JSON-форматі.</summary>
public static class SpreadsheetFile
{
    private sealed class Dto
    {
        public int Rows { get; set; }
        public int Columns { get; set; }
        public Dictionary<string, string> Cells { get; set; } = new();
    }

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static string ToJson(Spreadsheet sheet)
    {
        var dto = new Dto { Rows = sheet.RowCount, Columns = sheet.ColumnCount };
        foreach (var kv in sheet.NonEmptyCells) dto.Cells[kv.Key.ToString()] = kv.Value;
        return JsonSerializer.Serialize(dto, Options);
    }

    public static Spreadsheet FromJson(string json)
    {
        var dto = JsonSerializer.Deserialize<Dto>(json) ?? throw new InvalidDataException("Порожній файл");
        var sheet = new Spreadsheet(dto.Rows, dto.Columns);
        foreach (var kv in dto.Cells)
        {
            if (!CellAddress.TryParse(kv.Key, out var a) || a.Row > dto.Rows || a.Column > dto.Columns)
                throw new InvalidDataException($"Некоректна адреса клітинки '{kv.Key}'");
            sheet.SetExpression(a.Row, a.Column, kv.Value);
        }
        return sheet;
    }

    public static void Save(Spreadsheet sheet, string path) => File.WriteAllText(path, ToJson(sheet));
    public static Spreadsheet Load(string path) => FromJson(File.ReadAllText(path));
}
