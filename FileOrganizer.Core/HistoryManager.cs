using System.Text.Json;

namespace FileOrganizer.Core;

public static class HistoryManager
{
    /// <summary>
    /// Saves the movement history to a JSON file.
    /// </summary>
    public static void SaveHistory(List<string[]> history, string historyDirectory)
    {
        if (!Directory.Exists(historyDirectory))
            Directory.CreateDirectory(historyDirectory);

        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(history, options);

        string historyFile = $"history_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        string historyFilePath = Path.Combine(historyDirectory, historyFile);
        
        File.WriteAllText(historyFilePath, json);
    }

    /// <summary>
    /// Reads the history directory and returns a list of history files and their headers.
    /// </summary>
    public static List<(string FilePath, string Header)> GetHistoryLogs(string historyDirectory)
    {
        var logs = new List<(string, string)>();

        if (!Directory.Exists(historyDirectory))
            return logs;

        string[] histories = Directory.GetFiles(historyDirectory, "*.json");

        foreach (string historyPath in histories)
        {
            try
            {
                using var stream = File.OpenRead(historyPath);
                using var doc = JsonDocument.Parse(stream);

                JsonElement root = doc.RootElement;
                JsonElement first = root.EnumerateArray().FirstOrDefault();

                if (first.ValueKind == JsonValueKind.Array)
                {
                    string headerText = first[0].GetString() ?? "Unknown History";
                    logs.Add((historyPath, headerText));
                }
            }
            catch
            {
                // If a JSON file is corrupted or empty, we just skip it
            }
        }

        return logs;
    }

    /// <summary>
    /// Reverts the file movements defined in the given history file.
    /// </summary>
    public static void Undo(string historyFilePath, Action<string>? onError = null)
    {
        if (!File.Exists(historyFilePath))
        {
            onError?.Invoke($"ERROR: History file not found at {historyFilePath}");
            return;
        }

        using (var stream = File.OpenRead(historyFilePath))
        using (var doc = JsonDocument.Parse(stream))
        {
            JsonElement root = doc.RootElement;

            // Skip the first element (the header)
            foreach (JsonElement elm in root.EnumerateArray().Skip(1))
            {
                string originalPath = elm[0].GetString()!;
                string currentPath = elm[1].GetString()!;

                if (!File.Exists(currentPath))
                {
                    onError?.Invoke($"ERROR: File {currentPath} does not exist, skipping.");
                    continue;
                }

                // If another file has taken the original spot, rename this one so we don't overwrite
                if (File.Exists(originalPath))
                {
                    string directory = Path.GetDirectoryName(originalPath)!;
                    string name = Path.GetFileNameWithoutExtension(originalPath);
                    string extension = Path.GetExtension(originalPath);

                    int i = 1;
                    string newPath;

                    do
                    {
                        newPath = Path.Combine(directory, $"{name} ({i}){extension}");
                        i++;
                    }
                    while (File.Exists(newPath));

                    originalPath = newPath;
                }

                try
                {
                    File.Move(currentPath, originalPath);
                }
                catch (Exception e)
                {
                    onError?.Invoke($"ERROR moving {currentPath}: {e.Message}");
                }
            }
        } // The stream is automatically closed here when the using block ends

        // Now that the stream is closed, we can safely delete the history file
        File.Delete(historyFilePath);
    }
}