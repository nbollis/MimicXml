namespace Core.Util;

public static class Logger
{
    public static void WriteLine(string message, int layer = 0)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}]: {new string('\t', layer)}{message}");
    }
}