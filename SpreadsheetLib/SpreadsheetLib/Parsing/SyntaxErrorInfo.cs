namespace SpreadsheetLib.Parsing;

/// <summary>Синтаксична помилка з локалізацією в тексті виразу.</summary>
public sealed record SyntaxErrorInfo(int Line, int Column, string Message)
{
    public override string ToString() => $"позиція {Column}: {Message}";
}
