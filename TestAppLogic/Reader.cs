using log4net;

namespace TestAppLogic;

public interface IReader
{
    /// <summary>
    ///     Reads one line of a .csv file.
    /// </summary>
    /// <returns>Data which was read in the form of a InputData struct.</returns>
    InputData ReadLine();

    /// <summary>
    ///     Checks whether there is a next line in the .csv file.
    /// </summary>
    /// <returns>True if there is at least one more line after the current one, false otherwise.</returns>
    bool HasNextLine();

    void CloseReader();
}

internal class Reader : IReader
{
    private static readonly ILog log = LogManager.GetLogger(typeof(Reader));

    private readonly string _fileName;
    private readonly StreamReader? _reader;
    private ConfigParameters _config;
    private int _lineNo;

    public Reader(string fileName)
    {
        _fileName = fileName;
        _lineNo = 1;
        _config = ConfigReader.ReadConfigData();

        string filePath = Path.Combine(_config.InputFolder, fileName);

        if (!File.Exists(filePath))
        {
            log.Error(string.Format("File: '{0}' does not exist!", filePath));
            throw new FileNotFoundException(fileName);
        }

        _reader = new StreamReader(filePath);
        _ = _reader.ReadLine(); // first line is just a header and it is not important
    }

    public void CloseReader()
    {
        if (_reader != null)
        {
            _reader.Close();
            _reader.Dispose();
        }
    }

    public InputData ReadLine()
    {
        string line;

        InputData data = new();

        if (HasNextLine())
        {
            line = _reader.ReadLine();
            _lineNo++;
            try
            {
                string[] split = SplitInput(line, _lineNo);

                data.MSISDN = split[(int)DataPosition.MSISDN_POS];

                try
                {
                    data.Amount = Convert.ToInt32(split[(int)DataPosition.AMOUNT_POS]);
                }
                catch
                {
                    data.Amount = -1;
                    log.Error(string.Format("Amount in file: '{0}' at line: {1} is not a number!", _fileName, _lineNo));
                }

                try
                {
                    data.Timestamp = Convert.ToDateTime(split[(int)DataPosition.TIMESTAMP_POS]);
                }
                catch
                {
                    data.Timestamp = new DateTime(0, 0, 0, 0, 0, 0);
                    log.Error(string.Format("Date in file: '{0}' at line: {1} is not in a known format!", _fileName,
                        _lineNo));
                }
            }
            catch
            {
            }
        }

        return data;
    }

    public bool HasNextLine()
    {
        return _reader.Peek() >= 0;
    }

    /// <summary>
    ///     Splits the input line into MSISDN, amount and timestamp values.
    /// </summary>
    /// <param name="line">The line which needs to be split.</param>
    /// <param name="lineNo">The number of the line which is being read.</param>
    /// <returns></returns>
    /// <exception cref="InvalidDataException"></exception>
    private string[] SplitInput(string line, int lineNo)
    {
        if (line.Split(";").Length == 3)
        {
            return line.Split(";");
        }

        _config = ConfigReader.ReadConfigData();
        string errorFile = Path.Combine(_config.ErrorFolder, _fileName + ".ERROR.txt");
        string error = "Invalid data entry in line: " + lineNo;

        using StreamWriter writer = new(errorFile, true);
        log.Error(error);
        writer.WriteLine(error);
        writer.Close();

        throw new InvalidDataException("Not enough data columns in line " + lineNo);
    }
}