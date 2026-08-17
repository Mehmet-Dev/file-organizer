namespace FileOrganizer.Core;

/// <summary>
/// Config settings for the file organizer
/// </summary>
public class OrganizerOptions
{
    /// <summary>
    /// If true it just shows the results of what will change 
    /// </summary>
    public bool DryRun { get; set; } = false;

    /// <summary>
    /// If true files that don't fall into any category will be moved to an unknown folder
    /// </summary>
    public bool OrganizeUnknown { get; set; } = false;

    /// <summary>
    /// The directory where the history folders are saved into :P 
    /// </summary>
    public string HistoryDirectory { get; set; } = Path.Combine(AppContext.BaseDirectory, "History");
}