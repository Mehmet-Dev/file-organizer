using System.Diagnostics;
using FileOrganizer.Core;

namespace FileOrganizer.CLI;

static class Program
{
    static string Version = "v0.0.1-alpha.1-dev.experimental.unstable.preview";
    private static int _animationFrame = 0;

    /// <summary>
    /// The cool CLI solution for the file organizer I think
    /// </summary>
    /// <param name="args"></param>
    static void Main(string[] args)
    {
        if (args.Contains("--help"))
        {
            ShowHelp();
            return;
        }

        if(args.Contains("--version"))
        {
            ConsoleWriter.Info($"Version: {Version}\nI am a sentient app.");
            return;
        }

        string historyFolder = Path.Combine(AppContext.BaseDirectory, "History");
        Console.Clear();

        if(args.Length == 0)
        {
            ConsoleWriter.Info("Usage: FileOrganizer <path> [flags]\nUse flag --help for a list of flags");
            return;
        }

        // handle undo flag early
        if(args.Contains("--undo-move"))
        {
            InitiateUndo(historyFolder);
            return;
        }

        // all the cool flags
        bool silent = !args.Contains("--loud");
        bool verbose = args.Contains("--verbose");
        bool skipSafe = args.Contains("--skip-safe");
        bool trackTime = !args.Contains("--skip-bench");

        var options = new OrganizerOptions // required for the core file
        {
            DryRun = args.Contains("--dry-run"),
            OrganizeUnknown = args.Contains("--organize-unknown"),
            HistoryDirectory = historyFolder
        };

        // find path
        string? path = args.FirstOrDefault(a => !a.StartsWith("--"));

        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) // if path isnt a directory/doesnt exist
        {
            ConsoleWriter.Error("Provided path is not a valid directory. Quitting.");
            return;
        }

        // safety
        if(!skipSafe && !options.DryRun)
        {
            if (!GetUserConfirmation($"Are you sure you want to organize: {path}?"))
            {
                options.DryRun = true;
            }
        }

        var countMap = new Dictionary<string, int>();
        int unknownCount = 0;

        Stopwatch watch = trackTime ? Stopwatch.StartNew() : new(); // tracking time

        FileOrganizer.Core.FileOrganizer.Organize(
            targetDirectory: path,
            options: options,
            onFileMoved: (original, dest, category) => // whenever a file moves this shit gets executed
            {
                if(silent && !verbose) 
                    ShowProgressAnimation();

                if(verbose)
                {
                    // Use FileInfo on the original file since dest might not exist yet if DryRun
                    var info = new FileInfo(original); 
                    double sizeInKb = info.Exists ? info.Length / 1024.0 : 0;

                    ConsoleWriter.Success($"[{category.ToUpper()}] {info.Name}");
                    ConsoleWriter.Dark($"  └─ Size: {sizeInKb:F2} KB");
                    ConsoleWriter.Dark($"  └─ Path: {dest}");
                }
                else if (!silent)
                    ConsoleWriter.Success($"{original} goes into {category}.");

                countMap[category] = countMap.GetValueOrDefault(category, 0) + 1;
            },
            onUnknownFile: (original) =>
            {
                unknownCount++;
                if(!silent || verbose)
                    ConsoleWriter.Warning($"{original} is unknown.");
            }
        );

        if(trackTime)
            watch.Stop();
        
        if(silent && !verbose)
            Console.Clear();
        
        ConsoleWriter.Success($"--* Final result in {watch.ElapsedMilliseconds}ms *--");
        foreach (var pair in countMap)
        {
            ConsoleWriter.Info($"{pair.Key}: {pair.Value}");
        }
        ConsoleWriter.Warning($"Unknown: {unknownCount}");
    }

    private static void InitiateUndo(string historyFolder)
    {
        var historyLogs = HistoryManager.GetHistoryLogs(historyFolder);
        if(historyLogs.Count == 0)
        {
            ConsoleWriter.Error("ERROR: No past history files present. Exiting.");
            Environment.Exit(-1);
        }

        for(int i = 0; i < historyLogs.Count; i++)
            ConsoleWriter.Info($"{i + 1}. {historyLogs[i].Header}");
        
        int index = 0;
        while(true)
        {
            ConsoleWriter.Warning($"\nMake a choice (1-{historyLogs.Count}), 0 to exit.");
            string? input = Console.ReadLine();

            if (!int.TryParse(input, out int choice))
            {
                ConsoleWriter.Error("ERROR: Invalid input, please try again.");
                continue;
            }

            if (choice == 0)
            {
                ConsoleWriter.Info("Exiting...");
                Environment.Exit(0);
            }

            if (choice > historyLogs.Count || choice < 1)
            {
                ConsoleWriter.Error("ERROR: Index is out of range, try again.");
                continue;
            }

            index = choice - 1;
            break;
        }

        Console.Clear();
        bool confirm = GetUserConfirmation($"Are you sure you want to undo: \"{historyLogs[index].Header}\"?");

        if(confirm)
        {
            HistoryManager.Undo(historyLogs[index].FilePath, onError: (err) => ConsoleWriter.Error(err));
            ConsoleWriter.Success("Successfully reverted changes.");
        }
        else
            ConsoleWriter.Info("Exiting...");
    }

    private static bool GetUserConfirmation(string prompt)
    {
        ConsoleWriter.Warning($"{prompt} (y/n)");
        Console.Write("> ");
        string? input = Console.ReadLine()?.Trim().ToLower();
        return input == "y" || input == "yes";
    }

    private static void ShowProgressAnimation()
    {
        _animationFrame = (_animationFrame + 1) % 4;
        string dots = new string('.', _animationFrame);
        Console.Write($"\rOrganizing files{dots}    ");
    }

    private static void ShowHelp()
    {
        ConsoleWriter.Info("FileOrganizer CLI");
        ConsoleWriter.Dark("A cross-platform CLI utility for organizing files by extension.");
        Console.WriteLine();
        ConsoleWriter.Warning("USAGE:");
        Console.WriteLine("  FileOrganizer <path> [flags]");
        Console.WriteLine();
        ConsoleWriter.Warning("FLAGS:");
        ConsoleWriter.Info("  --dry-run            Preview organization without moving files");
        ConsoleWriter.Info("  --loud               Display files as they are organized");
        ConsoleWriter.Info("  --verbose            Display detailed metadata for each file");
        ConsoleWriter.Info("  --organize-unknown   Place unrecognized files into 'Unknown'");
        ConsoleWriter.Info("  --skip-safe          Skip confirmation prompt");
        ConsoleWriter.Info("  --skip-bench         Skip timing execution");
        ConsoleWriter.Info("  --undo-move          Select and revert a previous run");
        ConsoleWriter.Info("  --version            Show version info");
        ConsoleWriter.Info("  --help               Display this help text");
    }
}