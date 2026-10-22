using SpreadsheetLib.Model;

namespace MyExcelMAUIApp;

public partial class MainPage : ContentPage
{
    private const double RowHeaderWidth = 46;
    private const double ColumnWidth = 130;
    private const double RowHeight = 40;

    private Spreadsheet _sheet = new(10, 6);
    private Entry[,] _cells;          // індексація з 1: _cells[рядок, стовпець]
    private bool _valueMode;          // false = ВИРАЗ, true = ЗНАЧЕННЯ
    private string _currentName;      // ім'я поточного файлу (без розширення)

    public MainPage()
    {
        InitializeComponent();
        BuildGrid();
    }

    // =====================================================================
    //  Побудова та оновлення таблиці
    // =====================================================================

    private void BuildGrid()
    {
        SheetGrid.Children.Clear();
        SheetGrid.RowDefinitions.Clear();
        SheetGrid.ColumnDefinitions.Clear();

        SheetGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(RowHeaderWidth)));
        for (int c = 1; c <= _sheet.ColumnCount; c++)
            SheetGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(ColumnWidth)));

        SheetGrid.RowDefinitions.Add(new RowDefinition(new GridLength(RowHeight)));
        for (int r = 1; r <= _sheet.RowCount; r++)
            SheetGrid.RowDefinitions.Add(new RowDefinition(new GridLength(RowHeight)));

        // Заголовки
        SheetGrid.Add(MakeHeader(""), 0, 0);
        for (int c = 1; c <= _sheet.ColumnCount; c++)
            SheetGrid.Add(MakeHeader(CellAddress.ColumnName(c)), c, 0);
        for (int r = 1; r <= _sheet.RowCount; r++)
            SheetGrid.Add(MakeHeader(r.ToString()), 0, r);

        // Клітинки
        _cells = new Entry[_sheet.RowCount + 1, _sheet.ColumnCount + 1];
        for (int r = 1; r <= _sheet.RowCount; r++)
            for (int c = 1; c <= _sheet.ColumnCount; c++)
            {
                var entry = CreateCellEntry(r, c);
                _cells[r, c] = entry;
                SheetGrid.Add(entry, c, r);
            }

        RefreshAll();
    }

    private static Label MakeHeader(string text)
    {
        var label = new Label
        {
            Text = text,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };
        label.SetAppThemeColor(VisualElement.BackgroundColorProperty,
            Color.FromArgb("#E6E6E6"), Color.FromArgb("#3A3A3A"));
        return label;
    }

    private Entry CreateCellEntry(int row, int column)
    {
        var entry = new Entry { FontSize = 14, IsReadOnly = _valueMode };
        entry.Focused += (_, _) => ShowCellInfo(row, column);
        entry.Completed += (_, _) => CommitCell(entry, row, column);
        entry.Unfocused += (_, _) => CommitCell(entry, row, column);
        return entry;
    }

    private void RefreshAll()
    {
        for (int r = 1; r <= _sheet.RowCount; r++)
            for (int c = 1; c <= _sheet.ColumnCount; c++)
                UpdateCell(r, c);
    }

    private void UpdateCell(int row, int column)
    {
        var entry = _cells[row, column];
        string expression = _sheet.GetExpression(row, column);

        if (string.IsNullOrEmpty(expression))
        {
            entry.Text = "";
            entry.ClearValue(Entry.TextColorProperty);
            return;
        }

        CellResult result = _sheet.Evaluate(row, column);
        entry.Text = _valueMode ? result.ToDisplayString() : expression;

        if (result.IsError) entry.TextColor = Colors.Red;
        else entry.ClearValue(Entry.TextColorProperty);
    }

    private void CommitCell(Entry entry, int row, int column)
    {
        if (_valueMode) return;
        if (row >= _cells.GetLength(0) || column >= _cells.GetLength(1)) return;
        if (!ReferenceEquals(_cells[row, column], entry)) return;   // застарілий елемент після перебудови

        string text = (entry.Text ?? "").Trim();
        if (text == _sheet.GetExpression(row, column)) return;

        _sheet.SetExpression(row, column, text);
        RefreshAll();
        ShowCellInfo(row, column);
    }

    private void ShowCellInfo(int row, int column)
    {
        var address = new CellAddress(row, column);
        string expression = _sheet.GetExpression(row, column);

        if (string.IsNullOrEmpty(expression))
        {
            StatusLabel.Text = $"{address}: порожня клітинка";
            return;
        }

        CellResult result = _sheet.Evaluate(row, column);
        StatusLabel.Text = result.IsError
            ? $"{address}: {expression}  →  ПОМИЛКА: {result.Error}"
            : $"{address}: {expression}  →  {result.ToDisplayString()}";
    }

    // =====================================================================
    //  Режими ВИРАЗ / ЗНАЧЕННЯ
    // =====================================================================

    private void OnModeToggled(object sender, ToggledEventArgs e)
    {
        _valueMode = e.Value;
        foreach (var entry in _cells)
            if (entry != null) entry.IsReadOnly = _valueMode;
        RefreshAll();
    }

    // =====================================================================
    //  Структура таблиці
    // =====================================================================

    private void OnAddRowClicked(object sender, EventArgs e)
    {
        _sheet.AddRow();
        BuildGrid();
    }

    private void OnAddColumnClicked(object sender, EventArgs e)
    {
        _sheet.AddColumn();
        BuildGrid();
    }

    private async void OnRemoveRowClicked(object sender, EventArgs e)
    {
        if (_sheet.RowCount <= 1)
        {
            await DisplayAlert("Увага", "Не можна видалити єдиний рядок.", "OK");
            return;
        }
        bool hasData = _sheet.NonEmptyCells.Any(k => k.Key.Row == _sheet.RowCount);
        if (hasData && !await DisplayAlert("Підтвердження",
                $"Останній рядок ({_sheet.RowCount}) містить дані. Видалити?", "Так", "Ні"))
            return;

        _sheet.RemoveLastRow();
        BuildGrid();
    }

    private async void OnRemoveColumnClicked(object sender, EventArgs e)
    {
        if (_sheet.ColumnCount <= 1)
        {
            await DisplayAlert("Увага", "Не можна видалити єдиний стовпець.", "OK");
            return;
        }
        bool hasData = _sheet.NonEmptyCells.Any(k => k.Key.Column == _sheet.ColumnCount);
        string name = CellAddress.ColumnName(_sheet.ColumnCount);
        if (hasData && !await DisplayAlert("Підтвердження",
                $"Останній стовпець ({name}) містить дані. Видалити?", "Так", "Ні"))
            return;

        _sheet.RemoveLastColumn();
        BuildGrid();
    }

    // =====================================================================
    //  Файли
    // =====================================================================

    private async void OnNewClicked(object sender, EventArgs e)
    {
        if (!await DisplayAlert("Нова таблиця", "Поточна таблиця буде закрита. Продовжити?", "Так", "Ні"))
            return;

        _sheet = new Spreadsheet(10, 6);
        _currentName = null;
        StatusLabel.Text = "Створено нову таблицю.";
        BuildGrid();
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        string name = await DisplayPromptAsync("Збереження", "Ім'я файлу (без розширення):",
            "Зберегти", "Скасувати", initialValue: _currentName ?? "table");
        if (string.IsNullOrWhiteSpace(name)) return;

        try
        {
            string path = SheetStorage.Save(_sheet, name);
            _currentName = SheetStorage.CleanName(name);
            await DisplayAlert("Збережено", $"Файл збережено:\n{path}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Помилка збереження", ex.Message, "OK");
        }
    }

    private async void OnOpenClicked(object sender, EventArgs e)
    {
        var names = SheetStorage.ListNames();
        if (names.Length == 0)
        {
            await DisplayAlert("Відкрити", $"У папці {SheetStorage.Folder} немає збережених таблиць.", "OK");
            return;
        }

        string choice = await DisplayActionSheet("Виберіть таблицю", "Скасувати", null, names);
        if (string.IsNullOrEmpty(choice) || choice == "Скасувати") return;

        try
        {
            _sheet = SheetStorage.Load(choice);
            _currentName = choice;
            StatusLabel.Text = $"Відкрито файл «{choice}».";
            BuildGrid();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Помилка відкриття", ex.Message, "OK");
        }
    }
    private void OnCalculateClicked(object sender, EventArgs e)
    {
        ModeSwitch.IsToggled = true;   // показати значення
        RefreshAll();
    }

    private async void OnExitClicked(object sender, EventArgs e)
    {
        if (await DisplayAlert("Підтвердження", "Ви дійсно хочете вийти?", "Так", "Ні"))
            Application.Current?.Quit();
    }

    // =====================================================================
    //  Довідка та інформація про програму
    // =====================================================================

    private async void OnHelpClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Довідка",
            "Кожна клітинка містить ЛОГІЧНИЙ вираз.\n\n" +
            "Операнди: цілі числа (довільної довжини), імена клітинок (A1, B2, AA10), круглі дужки.\n" +
            "Функції: inc(x), dec(x).\n" +
            "Порівняння: =, <, >.\n" +
            "Логічні операції: not, and, or, eqv.\n" +
            "Пріоритет (від вищого): порівняння, not, and, or, eqv.\n\n" +
            "Приклад: inc(A1) > 5 and not (B1 = 3)\n\n" +
            "Режим ВИРАЗ показує тексти виразів, режим ЗНАЧЕННЯ - обчислені значення " +
            "(ІСТИНА / ХИБНІСТЬ / число).\n" +
            "Клітинки з помилками виділяються червоним; торкніться клітинки - " +
            "під панеллю кнопок з'явиться опис помилки та її позиція.\n\n" +
            $"Файли зберігаються в папці:\n{SheetStorage.Folder}",
            "OK");
    }

    private async void OnAboutClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Про програму",
            "Електронна таблиця\n" +
            "Лабораторна робота №1, варіант 77\n" +
            "(inc/dec, =, <, >, not, and/or, eqv)\n\n" +
            "Синтаксичний аналіз: ANTLR4\n" +
            "Інтерфейс: .NET MAUI\n\n" +
            "Автор: (Гламазда Єлизавета Миколаївна, К-27)",
            "OK");
    }

}