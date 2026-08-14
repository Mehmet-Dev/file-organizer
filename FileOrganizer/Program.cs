using System.Diagnostics;
using System.Text.Json;
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
    /// Used in resetting an earlier done sorting.
    /// Affecting flag: --undo-move
    /// </summary>
    static bool UndoMove = false;

    /// <summary>
    /// Used for silent organizing.
    /// </summary>
    private static int _animationFrame = 0;

    /// <summary>
    /// Variable used for globally storing file moving history.
    /// Used in <see cref="MoveFile"/> to make a history, and used in <see cref="InitiateUndo"/> to move things back to their original places.
    /// </summary>
    private static List<string[]> _history = new();

    static void Main(string[] args)
    {
        if (args.Contains("--help"))
        {
            ShowHelp();
            return;
        }

        string historyFolder = Path.Combine(AppContext.BaseDirectory, "History");
        if (!Directory.Exists(historyFolder))
            Directory.CreateDirectory(historyFolder);

        Console.Clear();
        if (args.Length == 0)
        {
            ConsoleWriter.Info("Usage: {path} {flags}\nUse flag --help for a list of flags");
            return;
        }

        UndoMove = args.Contains("--undo-move");

        if (UndoMove)
        {
            InitiateUndo(historyFolder);
        }

        NoMoving = args.Contains("--dry-run");
        Silent = !args.Contains("--loud");
        OrganizeUnknown = args.Contains("--organize-unknown");
        Verbose = args.Contains("--verbose");
        SkipSafe = args.Contains("--skip-safe");
        TrackTime = !args.Contains("--skip-bench");

        string path = args[0];

        // general error checking
        if (File.Exists(path))
        {
            ConsoleWriter.Error("Provided path is a file, quitting.");
            return;
        }

        if (!SkipSafe && !NoMoving)
        {
            if (!GetUserConfirmation($"Are you sure you want to organize: {path}?"))
            {
                NoMoving = true;
            }
        }

        // setting up program
        var extensionMap = GetExtensionMap();
        Dictionary<string, int> countMap = [];
        IEnumerable<string> files = Directory.EnumerateFiles(path);
        int unknownCount = 0; // for when the extension doesn't match any

        // Only create the history folder in the case we actually move files
        if (!NoMoving)
            _history.Add([$"File history of folder {Path.GetDirectoryName(path)} at {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}"]);

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
                {
                    MoveFile(path, name, result);
                }


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

        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(_history, options);

        string historyFile = $"history_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        string historyFilePath = Path.Combine(historyFolder, historyFile);
        File.WriteAllText(historyFilePath, json);
    }

    /// <summary>
    /// Used. TRUE
    /// </summary>
    private static void InitiateUndo(string historyFolder)
    {
        string[] histories = Directory.GetFiles(historyFolder);
        List<string> historyHeaders = [];

        if (histories.Length <= 0)
        {
            ConsoleWriter.Error("ERROR: There are no any past history files present. Exiting.");
            Environment.Exit(-1);
        }

        // getting headers of the json files
        foreach (string history in histories)
        {
            using var stream = File.OpenRead(history);
            using var doc = JsonDocument.Parse(stream);

            JsonElement root = doc.RootElement;
            JsonElement first = root.EnumerateArray().FirstOrDefault();

            if (first.ValueKind == JsonValueKind.Array)
            {
                string headerText = first[0].GetString();
                historyHeaders.Add(headerText);
            }
        }

        // displaying the possible choices in a nice manner
        for (int i = 0; i < historyHeaders.Count; i++)
        {
            ConsoleWriter.Info($"{i + 1}. {historyHeaders[i]}");
        }

        int index = 0;

        while (true)
        {
            ConsoleWriter.Warning($"\nMake a choice (1-{historyHeaders.Count + 1}), 0 to exit.");
            string? input = Console.ReadLine();

            // not an int
            if (!int.TryParse(input, out int choice))
            {
                ConsoleWriter.Error("ERROR: Invalid input, please try again.");
                continue;
            }

            // if user exits
            if (choice == 0)
            {
                ConsoleWriter.Info("Exiting...");
                Environment.Exit(0);
            }

            // if index is out of range
            if (choice > historyHeaders.Count || choice < 1)
            {
                ConsoleWriter.Error("ERROR: Index is out of range, try again.");
                continue;
            }

            //finally assume everything is dealt with
            index = --choice;
            break;
        }

        Console.Clear();
        bool confirm = GetUserConfirmation($"Are you sure you want to undo the following organization: \"{historyHeaders[index]}\"?");

        // start the process of undoing
        if (confirm)
        {
            // i suppose we try implementing it with our previous approach of reading it through the IO stream
            using var stream = File.OpenRead(histories[index]);
            using var doc = JsonDocument.Parse(stream);

            JsonElement root = doc.RootElement;

            foreach (JsonElement elm in root.EnumerateArray().Skip(1))
            {
                string before = elm[0].GetString();
                string after = elm[1].GetString();

                // case 1: check whether the file is still in the "after" part
                if (!File.Exists(after))
                {
                    ConsoleWriter.Error($"ERROR: File {after} does not exist, skipping.");
                    continue;
                }

                // case 2: check whether the file exists in the before part. if yes, rename the file
                if (File.Exists(before))
                {
                    string directory = Path.GetDirectoryName(before)!;
                    string name = Path.GetFileNameWithoutExtension(before);
                    string extension = Path.GetExtension(before);

                    int i = 1;
                    string newPath;

                    do
                    {
                        newPath = Path.Combine(directory, $"{name} ({i}){extension}");
                        i++;
                    }
                    while (File.Exists(newPath));

                    before = newPath;
                }

                try { File.Move(after, before); }
                catch (Exception e)
                {
                    ConsoleWriter.Error($"ERROR: {e.Message}");
                }
            }

            // assuming it ran well i guess...
            ConsoleWriter.Success("Successfully reverted changes. If there are any errors, you'll see it there.");
            File.Delete(histories[index]);
        }

        if (!confirm)
            ConsoleWriter.Info("Exiting...");

        Environment.Exit(0);
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

        // Installers & ExecutablesFlySharp
        { ".exe", "Installers" },
        { ".msi", "Installers" },
        { ".deb", "Installers" },
        { ".rpm", "Installers" },
        { ".appimage", "Installers" },
    };

    private static bool GetUserConfirmation(string prompt)
    {
        ConsoleWriter.Warning($"{prompt} (y/n)");
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
        _history.Add([filePath, destinationFile]);
    }

    private static void ShowHelp()
    {
        ConsoleWriter.Info("FileOrganizer");
        ConsoleWriter.Dark("A cross-platform CLI utility for organizing files by extension.");
        Console.WriteLine();

        ConsoleWriter.Warning("USAGE:");
        Console.WriteLine("  FileOrganizer <path> [flags]");
        Console.WriteLine();

        ConsoleWriter.Warning("EXAMPLES:");
        Console.WriteLine("  FileOrganizer ~/Downloads");
        Console.WriteLine("  FileOrganizer ~/Downloads --dry-run");
        Console.WriteLine("  FileOrganizer ~/Downloads --verbose --loud");
        Console.WriteLine("  FileOrganizer --undo-move");
        Console.WriteLine();

        ConsoleWriter.Warning("FLAGS:");

        ConsoleWriter.Info("  --help");
        Console.WriteLine("      Display this help message.");

        ConsoleWriter.Info("  --dry-run");
        Console.WriteLine("      Preview the organization without moving any files.");

        ConsoleWriter.Info("  --loud");
        Console.WriteLine("      Display information about each file while organizing.");

        ConsoleWriter.Info("  --verbose");
        Console.WriteLine("      Display detailed information about each file, including");
        Console.WriteLine("      size, creation date, modification date and read-only status.");
        Console.WriteLine("      Overrides --loud.");

        ConsoleWriter.Info("  --organize-unknown");
        Console.WriteLine("      Move files with unrecognized extensions into an");
        Console.WriteLine("      \"Unknown\" directory.");

        ConsoleWriter.Info("  --skip-safe");
        Console.WriteLine("      Skip the confirmation prompt before organizing.");

        ConsoleWriter.Info("  --skip-bench");
        Console.WriteLine("      Disable execution time measurement.");

        ConsoleWriter.Info("  --undo-move");
        Console.WriteLine("      Select a previous organization history and undo its changes.");

        Console.WriteLine();
        ConsoleWriter.Warning("ORGANIZATION:");

        ConsoleWriter.Success("  Documents");
        ConsoleWriter.Dark("      .pdf .docx .doc .txt .xlsx .xls .csv .pptx .md .epub");

        ConsoleWriter.Success("  Images");
        ConsoleWriter.Dark("      .png .jpg .jpeg .gif .svg .webp .ico .heic");

        ConsoleWriter.Success("  Audio");
        ConsoleWriter.Dark("      .mp3 .wav .flac .m4a .ogg");

        ConsoleWriter.Success("  Video");
        ConsoleWriter.Dark("      .mp4 .mkv .mov .avi .webm");

        ConsoleWriter.Success("  Archives");
        ConsoleWriter.Dark("      .zip .tar.gz .tar .gz .rar .7z .iso");

        ConsoleWriter.Success("  Code");
        ConsoleWriter.Dark("      .cs .py .js .html .json .sh");

        ConsoleWriter.Success("  Installers");
        ConsoleWriter.Dark("      .exe .msi .deb .rpm .appimage");

        Console.WriteLine();
        ConsoleWriter.Warning("NOTES:");
        Console.WriteLine("  Files with unrecognized extensions are left untouched unless");
        Console.WriteLine("  --organize-unknown is specified.");
        Console.WriteLine();
        Console.WriteLine("  A history file is created whenever files are actually moved,");
        Console.WriteLine("  allowing previous operations to be reverted with --undo-move.");
    }
}