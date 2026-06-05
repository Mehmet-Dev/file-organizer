using FileOrganizer.Util;

namespace FileOrganizer;

class Program
{
    static void Main(string[] args)
    {
        Console.Clear();
        if (args.Length == 0)
        {
            ConsoleWriter.Info("Usage: {path} {flags}\nUse flag --help for a list of flags");
            return;
        }

        string path = args[0];

        // general error checking
        if(File.Exists(path))
        {
            ConsoleWriter.Error("Provided path is a file, quitting.");
            return;
        }

        // setting up program
        var extensionMap = GetExtensionMap();
        var countMap = GetGeneralCountMap();
        IEnumerable<string> files = Directory.EnumerateFiles(path);
        int unknownCount = 0; // for when the extension doesn't match any


        foreach (var name in files)
        {
            string extension = Path.GetExtension(name);

            if (extensionMap.TryGetValue(extension, out string? result))
            {
                ConsoleWriter.Success($"{name} goes into {result}.");
                countMap[result]++;
                continue;
            }

            ConsoleWriter.Warning($"{name} is unknown.");
            unknownCount++;
        }


        ConsoleWriter.Success("--* Final result *--");
        foreach(KeyValuePair<string, int> pair in countMap)
        {
            ConsoleWriter.Info($"{pair.Key}: {pair.Value}");
        }
        ConsoleWriter.Warning($"Unknown: {unknownCount}");
    }

    private static Dictionary<string, string> GetExtensionMap() => new(StringComparer.OrdinalIgnoreCase)
    {
        // Documents
        { ".pdf", "Documents" },
        { ".docx", "Documents" },
        { ".txt", "Documents" },
        { ".xlsx", "Documents" },
        { ".csv", "Documents" },
    
        // Images
        { ".png", "Images" },
        { ".jpg", "Images" },
        { ".jpeg", "Images" },
        { ".gif", "Images" },
        { ".svg", "Images" },
    
        // Media (Audio/Video)
        { ".mp3", "Media" },
        { ".mp4", "Media" },
        { ".mkv", "Media" },
        { ".wav", "Media" },
    
        // Archives
        { ".zip", "Archives" },
        { ".tar.gz", "Archives" },
        { ".rar", "Archives" },
        { ".7z", "Archives" }
    };

    private static Dictionary<string, int> GetGeneralCountMap() => new()
    {
        {"Documents", 0},
        {"Images", 0},
        {"Media", 0},
        {"Archives", 0}
    };
}