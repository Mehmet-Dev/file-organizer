using System.Diagnostics;
using FileOrganizer.Util;

namespace FileOrganizer;

static class Program
{
    /// <summary>
    /// Indicates whether files should actually be moved or not.
    /// Can be used to see results and not actually moved.
    /// Affecting flag: --dry-run
    /// Also affected by choosing not to organize it in the confirmation.
    /// </summary>
    static bool NoMoving = false;

    /// <summary>
    /// Indicates whether it should only print out the final results.
    /// Affecting flag: --loud
    /// Will be ignored when Verbose is set to true.
    /// </summary>
    static bool Silent = true;

    /// <summary>
    /// Indicates whether the unknown files should be organized too.
    /// Affecting flag: --organize-unknown
    /// Will be ignored when NoMoving is set to true.
    /// </summary>
    static bool OrganizeUnknown = false;

    /// <summary>
    /// Indicates whether even more details should be exposed.
    /// Realistically speaking this would affect the speed of the process too. I think.
    /// Affecting flag: --verbose
    /// </summary>
    static bool Verbose = false;

    /// <summary>
    /// Indicates whether to skip the confirmation to organize the folder.
    /// Affecting flag: --skip-safe
    /// </summary>
    static bool SkipSafe = false;

    /// <summary>
    /// Indicates whether to measure the duration of the process.
    /// Affecting flag: --skip-bench
    /// </summary>
    static bool TrackTime = true;

    /// <summary>
    /// Used for silent organizing.
    /// </summary>
    private static int _animationFrame = 0;

    static void Main(string[] args)
    {
        Console.Clear();
        if (args.Length == 0)
        {
            ConsoleWriter.Info("Usage: {path} {flags}\nUse flag --help for a list of flags");
            return;
        }

        NoMoving = args.Contains("--dry-run");
        Silent = !args.Contains("--loud");
        OrganizeUnknown = args.Contains("--organize-unknown");
        Verbose = args.Contains("--verbose");
        SkipSafe = args.Contains("--skip-safe");
        TrackTime = !args.Contains("--skip-bench");

        string path = args[0];

        if (!SkipSafe && !NoMoving)
        {
            if (!GetUserConfirmation(path))
            {
                NoMoving = true;
            }
        }

        // general error checking
        if (File.Exists(path))
        {
            ConsoleWriter.Error("Provided path is a file, quitting.");
            return;
        }

        // setting up program
        var extensionMap = GetExtensionMap();
        Dictionary<string, int> countMap = [];
        IEnumerable<string> files = Directory.EnumerateFiles(path);
        int unknownCount = 0; // for when the extension doesn't match any

        Stopwatch watch = new();

        if (TrackTime)
            watch = Stopwatch.StartNew();

        foreach (var name in files)
        {
            if (Silent && !Verbose)
                ShowProgressAnimation();

            string extension = Path.GetExtension(name);

            string result;

            if (extensionMap.TryGetValue(extension, out result!))
            {
                if (Verbose)
                {
                    var info = new FileInfo(name);
                    double sizeInKb = info.Length / 1024.0;

                    ConsoleWriter.Success($"[{result.ToUpper()}] {info.Name}");
                    ConsoleWriter.Dark($"  └─ Size: {sizeInKb:F2} KB");
                    ConsoleWriter.Dark($"  └─ Created: {info.CreationTime:yyyy-MM-dd HH:mm:ss}");
                    ConsoleWriter.Dark($"  └─ Modified: {info.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
                    ConsoleWriter.Dark($"  └─ Locked: {info.IsReadOnly}");
                }
                else if (!Silent)
                    ConsoleWriter.Success($"{name} goes into {result}.");
                if (!countMap.ContainsKey(result))
                {
                    countMap[result] = 0;
                }

                countMap[result]++;

                if (!NoMoving)
                    MoveFile(path, name, result);

                continue;
            }

            ConsoleWriter.Warning($"{name} is unknown.");
            unknownCount++;

            if (OrganizeUnknown)
                MoveFile(path, name, "Unknown");
        }

        if (TrackTime)
            watch.Stop();

        if (Silent && !Verbose)
            Console.Clear();
        ConsoleWriter.Success($"--* Final result in {watch.ElapsedMilliseconds}ms *--");
        foreach (KeyValuePair<string, int> pair in countMap)
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
        { ".doc", "Documents" },
        { ".txt", "Documents" },
        { ".xlsx", "Documents" },
        { ".xls", "Documents" },
        { ".csv", "Documents" },
        { ".pptx", "Documents" },
        { ".md", "Documents" },
        { ".epub", "Documents" },
    
        // Images
        { ".png", "Images" },
        { ".jpg", "Images" },
        { ".jpeg", "Images" },
        { ".gif", "Images" },
        { ".svg", "Images" },
        { ".webp", "Images" },
        { ".ico", "Images" },
        { ".heic", "Images" },
    
        // Audio
        { ".mp3", "Audio" },
        { ".wav", "Audio" },
        { ".flac", "Audio" },
        { ".m4a", "Audio" },
        { ".ogg", "Audio" },

        // Video
        { ".mp4", "Video" },
        { ".mkv", "Video" },
        { ".mov", "Video" },
        { ".avi", "Video" },
        { ".webm", "Video" },
    
        // Archives & Disk Images
        { ".zip", "Archives" },
        { ".tar.gz", "Archives" },
        { ".tar", "Archives" },
        { ".gz", "Archives" },
        { ".rar", "Archives" },
        { ".7z", "Archives" },
        { ".iso", "Archives" },

        // Code & Scripts
        { ".cs", "Code" },
        { ".py", "Code" },
        { ".js", "Code" },
        { ".html", "Code" },
        { ".json", "Code" },
        { ".sh", "Code" },

        // Installers & Executables
        { ".exe", "Installers" },
        { ".msi", "Installers" },
        { ".deb", "Installers" },
        { ".rpm", "Installers" },
        { ".appimage", "Installers" },
    };

    private static bool GetUserConfirmation(string targetPath)
    {
        ConsoleWriter.Warning($"Are you sure you want to organize: {targetPath}? (y/n)");
        Console.Write("> ");

        string? input = Console.ReadLine()?.Trim().ToLower();

        // If they explicitly typed 'y' or 'yes', we are good to go
        if (input == "y" || input == "yes")
        {
            return true;
        }

        // Default to safety for any other input
        return false;
    }

    private static void ShowProgressAnimation()
    {
        _animationFrame = (_animationFrame + 1) % 4;
        string dots = new string('.', _animationFrame);

        // The \r resets the cursor, the spaces at the end clean up old trailing dots
        Console.Write($"\rOrganizing files{dots}    ");
    }

    private static void MoveFile(string sourcePath, string filePath, string destination)
    {
        string destinationPath = Path.Combine(sourcePath, destination);
        if (!Directory.Exists(destinationPath))
            Directory.CreateDirectory(destinationPath);

        string tempFileName = Path.GetFileName(filePath);
        string destinationFile = Path.Combine(destinationPath, tempFileName);

        // if the file exists, try renaming it
        for (int i = 0; File.Exists(destinationFile); i++)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath) + $" ({i})" + Path.GetExtension(filePath);
            destinationFile = Path.Combine(destinationPath, fileName);
        }

        File.Move(filePath, destinationFile);
    }
}