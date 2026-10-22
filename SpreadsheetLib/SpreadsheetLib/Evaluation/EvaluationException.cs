namespace SpreadsheetLib.Evaluation;

/// <summary>Семантична помилка під час обчислення (типи, посилання, порожні клітинки...).</summary>
public sealed class EvaluationException : Exception
{
    public EvaluationException(string message) : base(message) { }
}
