using System.Numerics;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using SpreadsheetLib.Model;
using SpreadsheetLib.Parsing;

namespace SpreadsheetLib.Evaluation;

/// <summary>
/// Етап 2: обчислення значення. Значення — BigInteger (ціле) або bool (логічне).
/// Посилання на клітинки розв'язуються через делегат.
/// </summary>
public sealed class ExpressionEvaluator : LogicExprBaseVisitor<object>
{
    private readonly Func<CellAddress, object> _resolveCell;

    public ExpressionEvaluator(Func<CellAddress, object> resolveCell) => _resolveCell = resolveCell;

    public object Evaluate(IParseTree tree) => Visit(tree);

    public override object VisitCompileUnit(LogicExprParser.CompileUnitContext context) => Visit(context.expr());

    // eqv (найнижчий пріоритет): істина, якщо обидва значення однакові
    public override object VisitExpr(LogicExprParser.ExprContext context)
    {
        var parts = context.orExpr();
        object result = Visit(parts[0]);
        for (int i = 1; i < parts.Length; i++)
        {
            bool l = RequireBool(result, "eqv", parts[i - 1]);
            bool r = RequireBool(Visit(parts[i]), "eqv", parts[i]);
            result = l == r;
        }
        return result;
    }

    public override object VisitOrExpr(LogicExprParser.OrExprContext context)
    {
        var parts = context.andExpr();
        object result = Visit(parts[0]);
        for (int i = 1; i < parts.Length; i++)
        {
            bool l = RequireBool(result, "or", parts[i - 1]);
            bool r = RequireBool(Visit(parts[i]), "or", parts[i]);   // обидва операнди обчислюються
            result = l || r;
        }
        return result;
    }

    public override object VisitAndExpr(LogicExprParser.AndExprContext context)
    {
        var parts = context.notExpr();
        object result = Visit(parts[0]);
        for (int i = 1; i < parts.Length; i++)
        {
            bool l = RequireBool(result, "and", parts[i - 1]);
            bool r = RequireBool(Visit(parts[i]), "and", parts[i]);
            result = l && r;
        }
        return result;
    }

    public override object VisitNotOp(LogicExprParser.NotOpContext context)
        => !RequireBool(Visit(context.notExpr()), "not", context.notExpr());

    public override object VisitToCmp(LogicExprParser.ToCmpContext context) => Visit(context.cmpExpr());

    public override object VisitCmpExpr(LogicExprParser.CmpExprContext context)
    {
        var terms = context.term();
        object left = Visit(terms[0]);
        if (terms.Length == 1) return left;

        object right = Visit(terms[1]);
        int op = context.op.Type;

        if (op == LogicExprParser.EQ)
        {
            if (left is BigInteger a && right is BigInteger b) return a == b;
            if (left is bool x && right is bool y) return x == y;
            throw Error(context, "операнди операції '=' мають бути одного типу (обидва цілі або обидва логічні)");
        }

        string sign = op == LogicExprParser.LT ? "<" : ">";
        BigInteger li = RequireInt(left, sign, terms[0]);
        BigInteger ri = RequireInt(right, sign, terms[1]);
        return op == LogicExprParser.LT ? li < ri : li > ri;
    }

    public override object VisitIntLit(LogicExprParser.IntLitContext context)
        => BigInteger.Parse(context.INT().GetText());

    public override object VisitCellRef(LogicExprParser.CellRefContext context)
    {
        string name = context.CELL().GetText();
        if (!CellAddress.TryParse(name, out var address))
            throw Error(context, $"некоректне ім'я клітинки '{name}'");
        return _resolveCell(address);
    }

    public override object VisitParens(LogicExprParser.ParensContext context) => Visit(context.expr());

    public override object VisitFuncCall(LogicExprParser.FuncCallContext context)
    {
        bool isInc = context.func.Type == LogicExprParser.INC;
        BigInteger v = RequireInt(Visit(context.expr()), isInc ? "inc" : "dec", context.expr());
        return isInc ? v + 1 : v - 1;
    }

    // ---------- допоміжні ----------
    private static EvaluationException Error(ParserRuleContext ctx, string message)
        => new($"позиція {ctx.Start.Column + 1}: {message}");

    private static bool RequireBool(object value, string op, ParserRuleContext ctx)
        => value is bool b ? b : throw Error(ctx, $"операція '{op}' потребує логічного значення, а отримано ціле число");

    private static BigInteger RequireInt(object value, string op, ParserRuleContext ctx)
        => value is BigInteger i ? i : throw Error(ctx, $"операція '{op}' потребує цілого числа, а отримано логічне значення");
}
