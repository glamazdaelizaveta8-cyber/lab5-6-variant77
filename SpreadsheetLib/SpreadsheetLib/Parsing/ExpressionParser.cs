using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace SpreadsheetLib.Parsing;

public sealed class ParseResult
{
    public IParseTree Tree { get; init; }
    public IReadOnlyList<SyntaxErrorInfo> Errors { get; init; }
    public bool Success => Errors.Count == 0;
}

/// <summary>Етап 1: синтаксична перевірка виразу з локалізацією помилок.</summary>
public static class ExpressionParser
{
    public static ParseResult Parse(string expression)
    {
        var listener = new CollectingErrorListener();

        var lexer = new LogicExprLexer(new AntlrInputStream(expression ?? ""));
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(listener);

        var parser = new LogicExprParser(new CommonTokenStream(lexer));
        parser.RemoveErrorListeners();
        parser.AddErrorListener(listener);

        IParseTree tree = parser.compileUnit();
        return new ParseResult { Tree = tree, Errors = listener.Errors };
    }

    /// <summary>Тільки перевірка синтаксису (без обчислення).</summary>
    public static IReadOnlyList<SyntaxErrorInfo> Check(string expression) => Parse(expression).Errors;
}
