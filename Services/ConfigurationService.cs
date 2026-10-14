namespace PokeREST.Services;

/// <summary>
/// Static service for loading environment variables from .env files.
/// Provides functionality to read configuration from environment files for application setup.
/// </summary>
public static class ConfigurationService
{
    /// <summary>
    /// Loads environment variables from a .env file in the current directory.
    /// Parses key=value pairs and sets them as environment variables, ignoring comments and empty lines.
    /// </summary>
    /// <remarks>
    /// Behavior to be aware of:
    /// <list type="bullet">
    /// <item>The file is resolved against the process working directory, not the content root.</item>
    /// <item>Values from the file overwrite variables already set in the environment.</item>
    /// <item>Only lines whose first character is <c>#</c> are comments; quotes and trailing comments are kept as part of the value.</item>
    /// <item>A missing file is silently ignored.</item>
    /// </list>
    /// </remarks>
    public static void LoadEnvironmentVariables()
    {
        var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
        if (File.Exists(envPath))
        {
            var lines = File.ReadAllLines(envPath);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                    continue;

                var parts = line.Split('=', 2);
                if (parts.Length == 2)
                {
                    Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
                }
            }
        }
    }
}
