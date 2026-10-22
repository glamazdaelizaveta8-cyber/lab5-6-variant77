using SpreadsheetLib.Model;
Console.OutputEncoding = System.Text.Encoding.UTF8;

var sheet = new Spreadsheet(5, 5);
sheet.SetExpression(1, 1, "5");                                   // A1
sheet.SetExpression(1, 2, "inc(A1) > 5 and not (A1 = 3)");        // B1
sheet.SetExpression(1, 3, "B1 eqv (A1 < 2)");                     // C1
sheet.SetExpression(1, 4, "A1 >");                                // D1, помилка
sheet.SetExpression(1, 5, "123456789012345678901234567890 > A1"); // E1, довге число

for (int c = 1; c <= 5; c++)
{
    var r = sheet.Evaluate(1, c);
    Console.WriteLine($"{CellAddress.ColumnName(c)}1: {(r.IsError ? r.Error : r.ToDisplayString())}");
}