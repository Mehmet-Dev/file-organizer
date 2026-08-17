namespace FileOrganizer.Core;

public static class FileOrganizer
{
    /// <summary>
    /// Organizes the files into the target directory
    /// </summary>
    /// <param name="targetDirectory">Directory to organize</param>
    /// <param name="options">Config options</param>
    /// <param name="onFileMoved">Callback when a file is categorized</param>
    /// <param name="onUnknownFile">Callback when a file extension aint recognized</param>
    public static void Organize(
        string targetDirectory,
        OrganizerOptions options,
        Action<string, string, string>? onFileMoved = null,
        Action<string>? onUnknownFile = null)
    {
        var extensionMap = GetExtensionMap();
        var files = Directory.EnumerateFiles(targetDirectory);
        var history = new List<string[]>();

        // only add history header if we actually move files
        if(!options.DryRun)
            history.Add([$"File history of folder {targetDirectory} at {DateTime.Now:yyyy-MM-dd HH:mm:ss}"]);
        
        foreach(var file in files)
        {
            string fileName = Path.GetFileName(file);

            // fix for compound extensions i.e. tar.gz
            string extension = fileName.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase) ? ".tar.gz" : Path.GetExtension(file);

            if(extensionMap.TryGetValue(extension, out string? category))
            {
                string destinationFile = GetDestinationPath(targetDirectory, file, category, options.DryRun);

                if (!options.DryRun)
                {
                    File.Move(file, destinationFile);
                    history.Add([file, destinationFile]);
                }

                onFileMoved?.Invoke(file, destinationFile, category);
            }
            else
            {
                onUnknownFile?.Invoke(file);

                if(options.OrganizeUnknown)
                {
                    string destinationFile = GetDestinationPath(targetDirectory, file, "Unknown", options.DryRun);

                    if(!options.DryRun)
                    {
                        File.Move(file, destinationFile);
                        history.Add([file, destinationFile]);
                    }

                    onFileMoved?.Invoke(file, destinationFile, "Unknown");
                }
            }
        }

        if(!options.DryRun && history.Count > 1)
            HistoryManager.SaveHistory(history, options.HistoryDirectory);
    }


    private static string GetDestinationPath(string source, string originalFile, string category, bool isDry)
    {
        string destination = Path.Combine(source, category);

        // create the folder if it doesnt exist and isnt a dry run
        if(!isDry && !Directory.Exists(destination))
            Directory.CreateDirectory(destination);

        string originalName = Path.GetFileNameWithoutExtension(originalFile);
        string extension = Path.GetExtension(originalFile);

        // ensure tar.gz remains properly
        if(originalFile.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
        {
            originalName = originalName.Substring(0, originalName.Length - 4);
            extension = ".tar.gz";
        }

        string destinationFile = Path.Combine(destination, Path.GetFileName(originalFile));

        int counter = 1;

        while(File.Exists(destinationFile))
        {
            destinationFile = Path.Combine(destination, $"{originalName} ({counter}){extension}");
            counter++;
        }

        return destinationFile;
    }

    private static Dictionary<string, string> GetExtensionMap() => new(StringComparer.OrdinalIgnoreCase)
    {
        // Documents
        { ".pdf", "Documents" }, { ".docx", "Documents" }, { ".doc", "Documents" },
        { ".txt", "Documents" }, { ".xlsx", "Documents" }, { ".xls", "Documents" },
        { ".csv", "Documents" }, { ".pptx", "Documents" }, { ".md", "Documents" },
        { ".epub", "Documents" },
    
        // Images
        { ".png", "Images" }, { ".jpg", "Images" }, { ".jpeg", "Images" },
        { ".gif", "Images" }, { ".svg", "Images" }, { ".webp", "Images" },
        { ".ico", "Images" }, { ".heic", "Images" },
    
        // Audio
        { ".mp3", "Audio" }, { ".wav", "Audio" }, { ".flac", "Audio" },
        { ".m4a", "Audio" }, { ".ogg", "Audio" },

        // Video
        { ".mp4", "Video" }, { ".mkv", "Video" }, { ".mov", "Video" },
        { ".avi", "Video" }, { ".webm", "Video" },
    
        // Archives & Disk Images
        { ".zip", "Archives" }, { ".tar.gz", "Archives" }, { ".tar", "Archives" },
        { ".gz", "Archives" }, { ".rar", "Archives" }, { ".7z", "Archives" },
        { ".iso", "Archives" },

        // Code & Scripts
        { ".cs", "Code" }, { ".py", "Code" }, { ".js", "Code" },
        { ".html", "Code" }, { ".json", "Code" }, { ".sh", "Code" },

        // Installers & Executables
        { ".exe", "Installers" }, { ".msi", "Installers" }, { ".deb", "Installers" },
        { ".rpm", "Installers" }, { ".appimage", "Installers" }
    };
}