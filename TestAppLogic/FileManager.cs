using log4net;
using System.Runtime.Intrinsics.X86;

namespace TestAppLogic;

public static class FileManager
{
    private static readonly ConfigParameters parameters = ConfigReader.ReadConfigData();
    private static readonly ILog log = LogManager.GetLogger(typeof(FileManager));

    /// <summary>
    ///     Writes sums of values into an output file specified in the config file.
    ///     The name of the output file is OriginalFileName.REPORT.txt.
    /// </summary>
    /// <param name="fileName">Name of the file where the aggregate will be stored.</param>
    /// <param name="dict">Key: day of the week, value sum of cents on the given day.</param>
    /// <param name="elapsedTime"></param>
    public static void WriteAggregateToFile(string fileName, IDictionary<DayOfWeek, long> dict, long elapsedTime)
    {
        string pathToFile = Path.Combine(parameters.OutputFolder, fileName + ".REPORT.txt");

        var sortedDict = dict.OrderBy(pair => pair.Key);
        var maxKeyLength = sortedDict.Max(pair => pair.Key.ToString().Length);
        string currentKey;
        string paddedKey;

        using StreamWriter writer = new(pathToFile);
        foreach (KeyValuePair<DayOfWeek, long> entry in sortedDict)
        {
            currentKey = entry.Key.ToString();
            paddedKey = currentKey.PadRight(maxKeyLength);
            writer.WriteLine($"{paddedKey}: {((decimal) entry.Value / 100) : 0.00} EUR");
        }

        writer.WriteLine("--------------------------------");
        writer.WriteLine("Processing time: {0} milliseconds", elapsedTime.ToString());
        writer.Close();
    }

    /// <summary>
    ///     Writes error logs to the ErrorFolder specified in the config file.
    ///     The name of the error file is OriginalFileName.ERROR.txt.
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="errors"></param>
    public static void WriteErrorsToErrorFile(string fileName, IList<string> errors)
    {
        var pathToFile = Path.Combine(parameters.ErrorFolder, fileName + ".ERROR.txt");

        using StreamWriter writer = new(pathToFile, true);
        foreach (string error in errors)
        {
            writer.WriteLine(error);
        }

        writer.WriteLine();
        writer.Close();
    }

    /// <summary>
    /// Moves the given file to the specified ErrorFolder.
    /// </summary>
    /// <param name="file"></param>
    public static void MoveFileToErrorFolder(string file)
    {
        var source = Path.Combine(parameters.InputFolder, file);
        var destination = Path.Combine(parameters.ErrorFolder, file);

        if (!File.Exists(destination))
        {
            log.Debug($"Starting relocation of {file} to {destination}...");
            File.Move(source, destination);
            log.Debug($"Finished relocation of {file} to {destination}.");
        } else
        {
            log.Warn($"File {file} already exists at destination.");
        }

    }

    /// <summary>
    ///     Moves processed file to a folder specified in the config file.
    /// </summary>
    /// <param name="file">Name of the file which is being moved.</param>
    public static void MoveFileToArchive(string file)
    {
        var source = Path.Combine(parameters.InputFolder, file);
        var destination = Path.Combine(parameters.ArchiveFolder, file);

        if (!File.Exists(destination))
        {
            log.Debug($"Starting relocation of {file} to {destination}...");
            File.Move(source, destination);
            log.Debug($"Finished relocation of {file} to {destination}.");
        } else
        {
            log.Warn($"File {file} already exists at destination.");
        }
    }
}