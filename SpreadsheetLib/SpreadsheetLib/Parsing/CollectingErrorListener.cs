using Antlr4.Runtime;

namespace SpreadsheetLib.Parsing;

/// <summary>
/// Збирає всі лексичні та синтаксичні помилки (не кидає виключень),
/// щоб показати користувачу точні місця помилок.
/// </summary>
internal sealed class CollectingErrorListener : BaseErrorListener, IAntlrErrorListener<int>
{
    public List<SyntaxErrorInfo> Errors { get; } = new();

    // Помилки парсера
    public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol,
        int line, int charPositionInLine, string msg, RecognitionException e)
    {
        string text = offendingSymbol == null || offendingSymbol.Type == TokenConstants.EOF
            ? "Несподіваний кінець виразу"
            : $"Неочікуваний елемент '{offendingSymbol.Text}'";
        Errors.Add(new SyntaxErrorInfo(line, charPositionInLine + 1, text));
    }

    // Помилки лексера
    public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol,
        int line, int charPositionInLine, string msg, RecognitionException e)
    {
        int i = msg.IndexOf('\'');
        string fragment = i >= 0 ? msg.Substring(i) : msg;
        Errors.Add(new SyntaxErrorInfo(line, charPositionInLine + 1, $"Недопустимий символ {fragment}"));
    }
}
