namespace TestAppLogic;

public static class FileManager
{
    private static readonly ConfigParameters parameters = ConfigReader.ReadConfigData();

    /// <summary>
    ///     Writes sums of values into an output file specified in the config file.
    ///     The name of the output file is OriginalFileName.REPORT.txt.
    /// </summary>
    /// <param name="fileName">Name of the file where the aggregate will be stored.</param>
    /// <param name="dict">Key: day of the week, value sum of cents on the given day.</param>
    /// <param name="elapsedTime"></param>
    public static void WriteAggregateToFile(string fileName, IDictionary<DayOfWeek, int> dict, long elapsedTime)
    {
        string pathToFile = Path.Combine(parameters.OutputFolder, fileName + ".REPORT.txt");

        using StreamWriter writer = new(pathToFile);
        foreach (KeyValuePair<DayOfWeek, int> entry in dict)
        {
            writer.WriteLine($"{entry.Key}: {entry.Value / 100},{entry.Value % 100} EUR");
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
        var errorLogTime = DateTime.Now;
        foreach (string error in errors)
        {
            writer.WriteLine(error);
        }

        writer.WriteLine("Logging time: " + errorLogTime);
        writer.WriteLine();
        writer.Close();
    }

    /// <summary>
    ///     Moves processed file to a folder specified in the config file.
    /// </summary>
    /// <param name="file">Name of the file which is being moved.</param>
    public static void MoveFileToArchive(string file)
    {
        var source = Path.Combine(parameters.InputFolder, file);
        var destination = Path.Combine(parameters.ArchiveFolder, file);

        File.Copy(source, destination, true);
        File.Delete(source);
    }
}