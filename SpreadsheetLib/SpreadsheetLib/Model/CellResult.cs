namespace SpreadsheetLib.Model;

public sealed class CellResult
{
    public object Value { get; private init; }
    public string Error { get; private init; }
    public bool IsError => Error != null;

    public static CellResult Ok(object value) => new() { Value = value };
    public static CellResult Fail(string error) => new() { Error = error };

    /// <summary>Текст для режиму ЗНАЧЕННЯ.</summary>
    public string ToDisplayString() => IsError ? "#ПОМИЛКА"
        : Value is bool b ? (b ? "ІСТИНА" : "ХИБНІСТЬ")
        : Value.ToString();
}
