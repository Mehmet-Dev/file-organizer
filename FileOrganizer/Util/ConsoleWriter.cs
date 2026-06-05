namespace FileOrganizer.Util;

public static class ConsoleWriter
{
    public static void Colorize(string message, ConsoleColor color, bool newLine = true)
    {
        ConsoleColor originalColor = Console.ForegroundColor;
        Console.ForegroundColor = color;

        if (newLine)
            Console.WriteLine(message);
        else
            Console.Write(message);

        Console.ForegroundColor = originalColor;
    }

    public static void Info(string message) => Colorize(message, ConsoleColor.Cyan);
    public static void Success(string message) => Colorize(message, ConsoleColor.Green);
    public static void Warning(string message) => Colorize(message, ConsoleColor.Yellow);
    public static void Error(string message) => Colorize(message, ConsoleColor.Red);
    public static void Dark(string message) => Colorize(message, ConsoleColor.DarkGray);
}