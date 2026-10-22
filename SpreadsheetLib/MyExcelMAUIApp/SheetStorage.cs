using SpreadsheetLib.Model;

namespace MyExcelMAUIApp;

/// <summary>Робота з файлами таблиць (папка Документи/SpreadsheetLab, формат JSON).</summary>
public static class SheetStorage
{
    private const string Extension = ".json";

    public static string Folder
    {
        get
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SpreadsheetLab");
            Directory.CreateDirectory(folder);
            return folder;
        }
    }

    public static string CleanName(string name)
    {
        foreach (char ch in Path.GetInvalidFileNameChars())
            name = name.Replace(ch, '_');
        return name.Trim();
    }

    public static string Save(Spreadsheet sheet, string name)
    {
        string path = Path.Combine(Folder, CleanName(name) + Extension);
        SpreadsheetFile.Save(sheet, path);
        return path;
    }

    public static Spreadsheet Load(string name)
        => SpreadsheetFile.Load(Path.Combine(Folder, name + Extension));

    public static string[] ListNames()
        => Directory.GetFiles(Folder, "*" + Extension)
                    .Select(Path.GetFileNameWithoutExtension)
                    .OrderBy(n => n)
                    .ToArray();
}