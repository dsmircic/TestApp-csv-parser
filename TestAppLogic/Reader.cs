using log4net;

namespace TestAppLogic
{
    public interface IReader
    {
        /// <summary>
        /// Reads one line of a .csv file.
        /// </summary>
        /// <returns>Data which was read in the form of a InputData struct.</returns>
        InputData ReadLine();

        /// <summary>
        /// Checks wheather there is a next line in the .csv file.
        /// </summary>
        /// <returns>True if there is at least one more line after the current one, false otherwise.</returns>
        bool HasNextLine();
        void CloseReader();
    }


    internal class Reader : IReader
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Reader));

        private readonly string fileName;
        private int lineNo;
        private readonly StreamReader? reader = null;
        private ConfigParameters parameters;

        public Reader(string fileName)
        {
            this.fileName = fileName;
            lineNo = 1;
            parameters = ConfigReader.ReadConfigData();

            string filePath = Path.Combine(parameters.InputFolder, fileName);

            if (!File.Exists(filePath))
            {
                log.Error(string.Format("File: '{0}' does not exist!", filePath));
                throw new FileNotFoundException(fileName);
            }
            else
            {
                reader = new StreamReader(filePath);
                reader.ReadLine(); // first line is just a header and it is not important
            }

        }

        public void CloseReader()
        {
            if (reader != null)
            {
                reader.Close();
                reader.Dispose();
            }
        }

        public InputData ReadLine()
        {
            string line;

            InputData data = new InputData();

            if (HasNextLine())
            {
                line = reader.ReadLine();
                lineNo++;
                try
                {
                    string[] split = SplitInput(line, lineNo);

                    data.MSISDN = split[(int)DataPosition.MSISDN_POS];

                    try
                    {
                        data.Amount = Convert.ToInt32(split[(int)DataPosition.AMOUNT_POS]);
                    }
                    catch
                    {
                        data.Amount = -1;
                        log.Error(string.Format("Amount in file: '{0}' at line: {1} is not a number!", fileName, lineNo));
                    }

                    try
                    {
                        data.Timestamp = Convert.ToDateTime(split[(int)DataPosition.TIMESTAMP_POS]);
                    }
                    catch
                    {
                        data.Timestamp = new DateTime(0, 0, 0, 0, 0, 0);
                        log.Error(string.Format("Date in file: '{0}' at line: {1} is not in a known format!", fileName, lineNo));
                    }


                }
                catch { }

            }

            return data;
        }

        public bool HasNextLine()
        {
            return reader.Peek() >= 0;
        }

        /// <summary>
        /// Splits the input line into MSISDN, amount and timestamp values.
        /// </summary>
        /// <param name="line">The line which needs to be split.</param>
        /// <param name="lineNo">The number of the line which is being read.</param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        private string[] SplitInput(string line, int lineNo)
        {
            if ((line.Split(";").Length == 3))
            {
                return line.Split(";");
            }
            else
            {
                parameters = ConfigReader.ReadConfigData();
                var errorFile = Path.Combine(parameters.ErrorFolder, fileName + ".ERROR.txt");
                var error = "Invalid data entry in line: " + lineNo;

                using StreamWriter writer = new(errorFile, true);
                log.Error(error);
                writer.WriteLine(error);
                writer.Close();

                throw new InvalidDataException("Not enough data columns in line " + lineNo);
            }
        }

    }
}
