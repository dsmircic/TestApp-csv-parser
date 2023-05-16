using log4net;
using System.Text.RegularExpressions;

namespace TestAppLogic;

internal class ConfigReader
{
    private static readonly ILog log = LogManager.GetLogger(typeof(ConfigReader));

    /// <summary>
    ///     Gets the value for a specified parameter.
    /// </summary>
    /// <param name="content">Content to be retrieved.</param>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private static string GetValue(string content, string key)
    {
        string pattern = $@"{key}:\s*(.*)";
        Match match = Regex.Match(content, pattern);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        log.Warn(string.Format($"Key '{key}' not found in the config file."));
        throw new ArgumentException();
    }

    /// <summary>
    ///     Reads path to ArchiveFolder, InputFolder, OutputFolder and number of lines.
    /// </summary>
    /// <returns>Returns read data in the form of a ConfigData struct.</returns>
    public static ConfigParameters ReadConfigData()
    {
        string fileContent = File.ReadAllText("app_config.txt");

        ConfigParameters parameters = new()
        {
            InputFolder = GetValue(fileContent, "InputFolder").Trim(),
            ErrorFolder = GetValue(fileContent, "ErrorFolder").Trim(),
            ArchiveFolder = GetValue(fileContent, "ArchiveFolder").Trim(),
            OutputFolder = GetValue(fileContent, "OutputFolder").Trim(),
            NumOfLines = int.Parse(GetValue(fileContent, "lineNo").Trim())
        };

        return parameters;
    }
}