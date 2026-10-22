using SpreadsheetLib.Evaluation;
using SpreadsheetLib.Parsing;

namespace SpreadsheetLib.Model;

/// <summary>Електронна таблиця: вирази в клітинках, обчислення з кешем і виявленням циклів.</summary>
public sealed class Spreadsheet
{
    private readonly Dictionary<CellAddress, string> _expressions = new();
    private readonly Dictionary<CellAddress, CellResult> _cache = new();
    private readonly HashSet<CellAddress> _inProgress = new();

    public int RowCount { get; private set; }
    public int ColumnCount { get; private set; }

    public Spreadsheet(int rows = 10, int columns = 6)
    {
        if (rows < 1 || columns < 1) throw new ArgumentOutOfRangeException();
        RowCount = rows;
        ColumnCount = columns;
    }

    // ---------- редагування структури ----------
    public void AddRow() { RowCount++; _cache.Clear(); }
    public void AddColumn() { ColumnCount++; _cache.Clear(); }

    /// <summary>Видаляє останній рядок (посилання на нього стануть помилковими).</summary>
    public bool RemoveLastRow()
    {
        if (RowCount <= 1) return false;
        foreach (var key in _expressions.Keys.Where(k => k.Row == RowCount).ToList()) _expressions.Remove(key);
        RowCount--;
        _cache.Clear();
        return true;
    }

    public bool RemoveLastColumn()
    {
        if (ColumnCount <= 1) return false;
        foreach (var key in _expressions.Keys.Where(k => k.Column == ColumnCount).ToList()) _expressions.Remove(key);
        ColumnCount--;
        _cache.Clear();
        return true;
    }

    // ---------- вміст клітинок ----------
    public string GetExpression(int row, int column)
        => _expressions.TryGetValue(new CellAddress(row, column), out var s) ? s : "";

    public void SetExpression(int row, int column, string expression)
    {
        var a = new CellAddress(row, column);
        if (string.IsNullOrWhiteSpace(expression)) _expressions.Remove(a);
        else _expressions[a] = expression.Trim();
        _cache.Clear();
    }

    public IEnumerable<KeyValuePair<CellAddress, string>> NonEmptyCells => _expressions;

    public void Clear() { _expressions.Clear(); _cache.Clear(); }

    // ---------- обчислення ----------
    public CellResult Evaluate(int row, int column)
    {
        _inProgress.Clear();
        return Compute(new CellAddress(row, column));
    }

    private CellResult Compute(CellAddress a)
    {
        if (_cache.TryGetValue(a, out var cached)) return cached;
        if (!_inProgress.Add(a)) return CellResult.Fail($"циклічне посилання на клітинку {a}");

        try
        {
            string text = GetExpression(a.Row, a.Column);
            CellResult result;

            if (string.IsNullOrWhiteSpace(text))
            {
                result = CellResult.Fail("клітинка порожня");
            }
            else
            {
                var parse = ExpressionParser.Parse(text);        // етап 1: синтаксис
                if (!parse.Success)
                {
                    result = CellResult.Fail("синтаксична помилка: " + string.Join("; ", parse.Errors));
                }
                else
                {
                    try                                          // етап 2: обчислення
                    {
                        result = CellResult.Ok(new ExpressionEvaluator(Resolve).Evaluate(parse.Tree));
                    }
                    catch (EvaluationException ex)
                    {
                        result = CellResult.Fail(ex.Message);
                    }
                }
            }

            _cache[a] = result;
            return result;
        }
        finally
        {
            _inProgress.Remove(a);
        }
    }

    private object Resolve(CellAddress a)
    {
        if (a.Row > RowCount || a.Column > ColumnCount)
            throw new EvaluationException($"посилання {a} виходить за межі таблиці");

        var r = Compute(a);
        if (r.IsError) throw new EvaluationException($"клітинка {a}: {r.Error}");
        return r.Value;
    }
}
