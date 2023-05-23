
using log4net;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace TestAppLogic;
/// <summary>
///     Parses a folder or .csv file depending on the config file.
///     Checks if the .csv file contains valid data entries.
/// </summary>
public interface IParser
{
    /// <summary>
    ///     Validates given .csv if specified, whole InputFolder if not. Checks if each MSISDN, amount and timestamp is valid.
    /// </summary>
    void Parse();
}

public abstract class ParserTemplate
{
    protected static readonly ILog log = LogManager.GetLogger(typeof(Parser));

    /// <summary>
    ///     Parses one line in a .csv file, validates MSISDN, amount and timestamp.
    /// </summary>
    /// <param name="data">Struct of data to be parsed.</param>
    /// <param name="lineNo">Specifies which line is currently being validated.</param>
    protected bool ParseLine(InputData data, int lineNo)
    {

        return  ValidateMsisdn(data, lineNo) && 
                ValidateAmount(data, lineNo) && 
                ValidateTimestamp(data, lineNo);
    }

    /// <summary>
    ///     Checks if the amount is between 0 (not included), and 10000 (included).
    /// </summary>
    /// <param name="amount">Amount to be checked.</param>
    /// <param name="lineNo">Line of .csv file where the amount is located.</param>
    /// <returns></returns>
    protected abstract bool ValidateAmount(InputData data, int lineNo);

    /// <summary>
    ///     Validates MSISDN. Checks if it starts with 'HR3859',
    ///     then checks if the next digit is one of {1, 2, 5, 7, 8},
    ///     after whicch should be a 5 or 6 digit number.
    /// </summary>
    /// <param name="MSISDN">String of MSISDN to be checked.</param>
    /// <param name="lineNo">Line of .csv file where given MSISDN is located.</param>
    /// <returns></returns>
    protected abstract bool ValidateMsisdn(InputData data, int lineNo);

    /// <summary>
    ///     Checks if the timestamp was created in the current month.
    /// </summary>
    /// <param name="timestamp">Timestamp to be validated.</param>
    /// <param name="lineNo">Line of .csv file where the timestamp is located.</param>
    /// <returns></returns>
    protected abstract bool ValidateTimestamp(InputData data, int lineNo);
}

public class Parser : ParserTemplate, IParser
{
    private readonly IDictionary<DayOfWeek, long> _aggregate;

    private readonly DataParameters _dataParams = new()
    {
        MIN_AMOUNT = 0,
        MAX_AMOUNT = 10000,
        CURRENT_MONTH = (byte)DateTime.Now.Month,
        CURRENT_YEAR = (short)DateTime.Now.Year
    };

    private readonly IDictionary<string, IList<string>> ErrorLog;

    private IReader? _reader;

    public Parser(string fileName = "")
    {
        _aggregate = new Dictionary<DayOfWeek, long>();
        ErrorLog = new Dictionary<string, IList<string>>();
        FileName = fileName;
    }

    private long ElapsedTime { get; set; }

    private string FileName { get; set; }
    private DateTime FileTimestamp { get; set; }

    private int LineNo { get; set; }

    public void Parse()
    {
        if (FileName == string.Empty)
        {
            try
            {
                ParseFolder();
            }
            catch (FileNotFoundException)
            {
            }
        }
        else
        {
            log.Info($"Parsing only {FileName} file.");
            InitReader();
            ParseFile();
        }
    }

    protected override bool ValidateAmount(InputData data, int lineNo)
    {
        var amount = data.Amount;

        if (amount is > 0 and <= 100000)
        {
            return true;
        }

        var err = $"Line: {lineNo} - {$"{data.MSISDN};{data.Amount};{data.Timestamp}"} {DateTime.Now : yyyy.MM.dd. HH:mm:ss} Validation error - ";

        if (amount > 10000)
        {
            err += $" Amount cannot be greater than {_dataParams.MAX_AMOUNT}";
            log.Error($"Amount cannot be greater than {_dataParams.MAX_AMOUNT}");
        }
        else
        {
            err += $" Amount cannot be less than {_dataParams.MIN_AMOUNT}";
            log.Error($"Amount cannot be less than {_dataParams.MIN_AMOUNT}");
        }

        if (ErrorLog.ContainsKey(FileName))
        {
            ErrorLog[FileName].Add(err);
        }
        else
        {
            ErrorLog.Add(FileName, new List<string> { err });
        }

        return false;
    }

    protected override bool ValidateMsisdn(InputData data, int lineNo)
    {
        var pattern = @"^3859[12789][1-9]\d{5,6}$";
        var MSISDN = data.MSISDN;

        if (MSISDN == null)
            return false;

        var isMatch = Regex.IsMatch(MSISDN, pattern);

        if (isMatch) return true;

        var err = $"Line: {lineNo} - {$"{data.MSISDN};{data.Amount};{data.Timestamp}"} {DateTime.Now: yyyy.MM.dd. HH:mm:ss} Validation error - Invalid MSISDN";
        log.Error(err);

        if (ErrorLog.ContainsKey(FileName))
        {
            ErrorLog[FileName].Add(err);
        }
        else
        {
            ErrorLog.Add(FileName, new List<string> { err });
        }

        return false;
    }

    protected override bool ValidateTimestamp(InputData data, int lineNo)
    {
        DateTime startOfMonth = new(DateTime.Now.Year, DateTime.Now.Month, 1, 0, 0, 0);
        var timestamp = data.Timestamp;

        if (timestamp < DateTime.Now && timestamp >= startOfMonth)
        {
            return true;
        }

        var err = $"Line: {lineNo} - {$"{data.MSISDN};{data.Amount};{data.Timestamp}"} {DateTime.Now: yyyy.MM.dd. HH:mm:ss} Validation error";

        if (timestamp > DateTime.Now)
        {
            err += $"Timestamp cannot be greater than the current timestamp: {DateTime.Now}";
            log.Error($"Timestamp cannot be greater than the current timestamp: {DateTime.Now}");
        }
        else if (timestamp > FileTimestamp)
        {
            err += $"Timestamp cannot be greater than the timestamp in the file name: {FileTimestamp}";
            log.Error($"Timestamp must be less than the timestamp in the file name {FileTimestamp}.");
        }

        if (ErrorLog.ContainsKey(FileName))
        {
            ErrorLog[FileName].Add(err);
        }
        else
        {
            ErrorLog.Add(FileName, new List<string> { err });
        }

        return false;
    }

    /// <summary>
    ///     Initializes the _reader to be able to read from the given input file.
    /// </summary>
    private void InitReader()
    {
        try
        {
            _reader = new Reader(FileName);
            LineNo = 1;
        }
        catch (FileNotFoundException)
        {
        }
    }

    private void extractTimeStamp(string fileName)
    {
        string pattern = @"TestData_(\d{8}_\d{2}:\d{2}:\d{2}).csv";

        // Use Regex.Match to find the first occurrence of the pattern
        Match match = Regex.Match(fileName, pattern);

        if (match.Success)
        {
            string timestamp = match.Groups[1].Value;
            FileTimestamp = Convert.ToDateTime(timestamp);
        }
    }

    /// <summary>
    ///     Parses one .csv file.
    /// </summary>
    private void ParseFile()
    {
        Stopwatch stopwatch = new();
        stopwatch.Start();

        log.Info("Parsing file: " + FileName);
        extractTimeStamp(FileName);
        InputData data;

        do
        {
            data = _reader.ReadLine();

            if (this.ParseLine(data, ++LineNo))
            {
                var day = data.Timestamp.DayOfWeek;

                if (_aggregate.ContainsKey(day))
                {
                    _aggregate[day] += data.Amount;
                }
                else
                {
                    _aggregate[day] = data.Amount;
                }
            }

            
        }
        while (_reader.HasNextLine());

        stopwatch.Stop();
        ElapsedTime = stopwatch.ElapsedMilliseconds;

        log.Debug("Closing _reader for file " + FileName);
        _reader.CloseReader();
        log.Info(
            string.Format(
                "Finished parsing file: '{0}' with: {1} errors in {2} milliseconds.",
                FileName,
                ErrorLog.ContainsKey(FileName) ? ErrorLog[FileName].Count : 0,
                ElapsedTime));

        if (!ErrorLog.ContainsKey(FileName))
        {
            FileManager.WriteAggregateToFile(FileName, _aggregate, ElapsedTime);
            FileManager.MoveFileToArchive(FileName);
        }
        else
        {
            log.Debug($"Writing error logs to file {FileName}.ERRORS.txt");
            FileManager.WriteErrorsToErrorFile(FileName, ErrorLog[FileName]);
            FileManager.MoveFileToErrorFolder(FileName);
            log.Debug($"Finished writing error logs to file {FileName}.ERRORS.txt");
        }

    }

    /// <summary>
    ///     Parses whole InputFolder which contains one or more .csv files.
    /// </summary>
    private void ParseFolder()
    {
        log.Info("Parsing whole InputFolder.");
        ConfigParameters parameters = ConfigReader.ReadConfigData();

        string[] filePaths = Directory.GetFiles(parameters.InputFolder);
        log.Debug($"Read whole {parameters.InputFolder} contents.");

        if (filePaths.Length == 0)
        {
            var err = string.Format("No files in: '{0}' directory.", parameters.InputFolder);
            log.Error(err);

            if (ErrorLog.ContainsKey(FileName))
                ErrorLog[FileName].Add(err);
            else
                ErrorLog.Add(FileName, new List<string> { err });

            throw new FileNotFoundException();
        }

        // files should be in chronological order once they are sorted by their names
        List<string> sortedFiles = new();
        foreach (var filePath in filePaths) sortedFiles.Add(Path.GetFileName(filePath));

        sortedFiles.Sort();

        foreach (string file in sortedFiles)
        {
            FileName = file;
            InitReader();
            ParseFile();
        }
    }
}