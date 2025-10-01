namespace Core.Services.IO;

public interface ITempFileCleanupService
{
    void CleanUpTempFile(string? tempFilePath);
}

public class TempFileCleanupService : ITempFileCleanupService
{
    public void CleanUpTempFile(string? tempFilePath)
    {
        if (tempFilePath != null && File.Exists(tempFilePath))
        {
            try
            {
                File.Delete(tempFilePath);
            }
            catch (Exception ex)
            {
                // Log the exception if you have a logging mechanism
                Console.WriteLine($"Warning: Could not delete temp file {tempFilePath}. Exception: {ex.Message}");
            }
        }
    }
}