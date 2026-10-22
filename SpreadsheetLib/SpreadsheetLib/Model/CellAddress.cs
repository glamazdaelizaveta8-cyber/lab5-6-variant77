using System.Globalization;
using System.Text;

namespace SpreadsheetLib.Model;

/// <summary>Адреса клітинки: рядок і стовпець нумеруються з 1 (A1 = рядок 1, стовпець 1).</summary>
public readonly record struct CellAddress(int Row, int Column)
{
    public static string ColumnName(int column)
    {
        var sb = new StringBuilder();
        while (column > 0)
        {
            column--;
            sb.Insert(0, (char)('A' + column % 26));
            column /= 26;
        }
        return sb.ToString();
    }

    public static bool TryParse(string text, out CellAddress address)
    {
        address = default;
        if (string.IsNullOrEmpty(text)) return false;

        int i = 0, col = 0;
        while (i < text.Length && text[i] < 128 && char.IsLetter(text[i]))
        {
            col = col * 26 + (char.ToUpperInvariant(text[i]) - 'A' + 1);
            if (col > 1_000_000) return false;
            i++;
        }
        if (i == 0 || i == text.Length) return false;
        if (!int.TryParse(text.AsSpan(i), NumberStyles.None, CultureInfo.InvariantCulture, out int row) || row < 1)
            return false;

        address = new CellAddress(row, col);
        return true;
    }

    public override string ToString() => ColumnName(Column) + Row;
}
