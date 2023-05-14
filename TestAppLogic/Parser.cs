using log4net;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace TestAppLogic
{
    /// <summary>
    /// Parses a folder or .csv file depending on the config file.
    /// Checks if the .csv file contains valid data entries.
    /// </summary>
    public interface IParser
    {
        /// <summary>
        /// Validates given .csv if specified, whole InputFolder if not. Checks if each MSISDN, amount and timestamp is valid.
        /// </summary>
        void Parse();

        /// <summary>
        /// Aggregates amounts and creates a sum for each day of the week.
        /// </summary>
        void Aggregate();
    }

    public abstract class ParserTemplate
    {
        protected static readonly ILog log = LogManager.GetLogger(typeof(Parser));

        /// <summary>
        /// Parses one line in a .csv file, validates MSISDN, amount and timestamp.
        /// </summary>
        /// <param name="data">Struct of data to be parsed.</param>
        /// <param name="lineNo">Specifies which line is currently being validated.</param>
        protected void ParseLine(InputData data, int lineNo)
        {
            ValidateMSISDN(data.MSISDN, lineNo);
            ValidateAmount(data.Amount, lineNo);
            ValidateTimestamp(data.Timestamp, lineNo);
        }

        /// <summary>
        /// Validates MSISDN. Checks if it starts with 'HR3859', 
        /// then checks if the next digit is one of {1, 2, 5, 7, 8},
        /// after whicch should be a 5 or 6 digit number.
        /// </summary>
        /// <param name="MSISDN">String of MSISDN to be checked.</param>
        /// <param name="lineNo">Line of .csv file where given MSISDN is located.</param>
        /// <returns></returns>
        protected abstract bool ValidateMSISDN(string MSISDN, int lineNo);

        /// <summary>
        /// Checks if the amount is between 0 (not included), and 10000 (included).
        /// </summary>
        /// <param name="amount">Amount to be checked.</param>
        /// <param name="lineNo">Line of .csv file where the amount is located.</param>
        /// <returns></returns>
        protected abstract bool ValidateAmount(int amount, int lineNo);

        /// <summary>
        /// Checks if the timestamp was created in the current month.
        /// </summary>
        /// <param name="timestamp">Timestamp to be validated.</param>
        /// <param name="lineNo">Line of .csv file where the timestamp is located.</param>
        /// <returns></returns>
        protected abstract bool ValidateTimestamp(DateTime timestamp, int lineNo);
    }


    public class Parser : ParserTemplate, IParser
    {
        private DataParameters dataParams = new DataParameters
        {
            MIN_AMOUNT = 0,
            MAX_AMOUNT = 10000,
            CURRENT_MONTH = (byte)DateTime.Now.Month,
            CURRENT_YEAR = (short)DateTime.Now.Year
        };

        private string FileName { get; set; }
        private int LineNo { get; set; }
        private long ElapsedTime { get; set; }

        private IReader? reader = null;
        private IDictionary<DayOfWeek, int> AggregateDict;
        private IDictionary<string, IList<string>> ErrorLog;

        public Parser(string fileName = "")
        {
            AggregateDict = new Dictionary<DayOfWeek, int>();
            ErrorLog = new Dictionary<string, IList<string>>();
            FileName = fileName;
        }

        /// <summary>
        /// Initializes the reader to be able to read from the given input file.
        /// </summary>
        private void InitReader()
        {
            try
            {
                reader = new Reader(FileName);
                LineNo = 1;
            }
            catch (FileNotFoundException) { }
        }

        public void Parse()
        {

            if (FileName == "")
            {
                try
                {
                    ParseFolder();
                }
                catch (FileNotFoundException) { }
            }
            else
            {
                log.Info($"Parsing only {FileName} file.");
                InitReader();
                ParseFile();
            }

        }

        /// <summary>
        /// Parses one .csv file.
        /// </summary>
        private void ParseFile()
        {
            Stopwatch stopwatch = new();
            stopwatch.Start();

            log.Info("Parsing file: " + FileName);
            InputData data;

            do
            {
                data = reader.ReadLine();
                try
                {
                    ParseLine(data, ++LineNo);
                }
                catch (InvalidDataException) { }

            } while (reader.HasNextLine());

            stopwatch.Stop();
            ElapsedTime = stopwatch.ElapsedMilliseconds;

            log.Debug("Closing reader for file " + FileName);
            reader.CloseReader();
            log.Info(string.Format(
                "Finished parsing file: '{0}' with: {1} errors in {2} miliseconds.", 
                FileName, 
                ErrorLog.ContainsKey(FileName) ? ErrorLog[FileName].Count : 0,
                ElapsedTime));

            if (!ErrorLog.ContainsKey(FileName))
            {
                Aggregate();

                log.Info($"Moving {FileName} into: '{ConfigReader.ReadConfigData().ArchiveFolder}'.");
                FileManager.MoveFileToArchive(FileName);
            }
            else
            {
                log.Debug($"Writing error logs to file {FileName}.ERRORS.txt");
                FileManager.WriteErrorsToErrorFile(FileName, ErrorLog[FileName]);
                log.Debug($"Finished writing error logs to file {FileName}.ERRORS.txt");
            }

        }

        /// <summary>
        /// Parses whole InputFolder which contains one or more .csv files.
        /// </summary>
        private void ParseFolder()
        {
            log.Info("Parsing whole InputFolder.");
            var parameters = ConfigReader.ReadConfigData();

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
            List<string> sortedFiles = new List<string>();
            foreach (string filePath in filePaths)
            {
                sortedFiles.Add(Path.GetFileName(filePath));
            }

            sortedFiles.Sort();

            foreach (string file in sortedFiles)
            {
                FileName = file;
                InitReader();
                ParseFile();
            }

        }

        public void Aggregate()
        {
            InitReader();

            log.Info("Aggregating file: " + FileName);
            InputData data;

            do
            {
                data = reader.ReadLine();
                try
                {
                    var dayOfWeek = data.Timestamp.DayOfWeek;
                    var amount = data.Amount;

                    if (AggregateDict.ContainsKey(dayOfWeek))
                        AggregateDict[dayOfWeek] += amount;
                    else
                        AggregateDict.Add(dayOfWeek, amount);

                }
                catch { }

            } while (reader.HasNextLine());

            reader.CloseReader();

            log.Debug($"Writing aggregate results to file {FileName}.REPORT.txt");
            FileManager.WriteAggregateToFile(FileName, AggregateDict, ElapsedTime);

            log.Info(string.Format("Finished aggregating file: '{0}' in: {1} miliseconds", FileName, ElapsedTime));
        }


        protected override bool ValidateAmount(int amount, int lineNo)
        {
            if (amount > 0 && amount <= 100000)
                return true;

            var err = string.Format("Amount in file: '{0}' at line: {1} is not valid.", FileName, lineNo);
            log.Error(err);

            if (amount > 10000)
                log.Error($"\tAmount cannot be greater than {dataParams.MAX_AMOUNT}");
            else
                log.Error($"\tAmount cannot be less than {dataParams.MIN_AMOUNT}");


            if (ErrorLog.ContainsKey(FileName))
                ErrorLog[FileName].Add(err);
            else
                ErrorLog.Add(FileName, new List<string> { err });
            throw new InvalidDataException();
        }

        protected override bool ValidateMSISDN(string MSISDN, int lineNo)
        {
            string pattern = @"HR3859[12789]\d\d\d\d\d\d?";

            if (MSISDN == null)
                return false;

            var isMatch = Regex.IsMatch(MSISDN, pattern);

            if (isMatch)
            {
                return true;
            }

            var err = string.Format("MSISDN in file: '{0}' at line: {1} is not valid.", FileName, lineNo);
            log.Error(err);

            if (ErrorLog.ContainsKey(FileName))
                ErrorLog[FileName].Add(err);
            else
                ErrorLog.Add(FileName, new List<string> { err });

            throw new InvalidDataException();
        }

        protected override bool ValidateTimestamp(DateTime timestamp, int lineNo)
        {
            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1, 0, 0, 0);

            if (timestamp < DateTime.Now && timestamp >= startOfMonth)
                return true;

            var err = string.Format("Timestamp in file: '{0}' at line: {1} is not valid.", FileName, lineNo);
            log.Error(err);

            if (timestamp > DateTime.Now)
                log.Error($"\tTimestamp cannot be greater than the current timestamp: {DateTime.Now}");
            else
                log.Error($"\tTimestamp must be within the current month");

            if (ErrorLog.ContainsKey(FileName))
                ErrorLog[FileName].Add(err);
            else
                ErrorLog.Add(FileName, new List<string> { err });

            throw new InvalidDataException();
        }
    }
}
