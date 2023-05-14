using log4net;
using System.Diagnostics;
using System.Text;

namespace TestAppLogic
{
    /// <summary>
    /// Generates a .csv file and stores it in the directory specified in the config file.
    /// Every line of the .csv file contains a data entry in the form of MSISDN;AMOUNT;TIMESTAMP.
    /// The value for each field is random.
    /// </summary>
    public interface IGenerator
    {

        /// <summary>
        /// Generates a .csv file with the format TestData_yyyyMMdd_HHmmss with random MSISDN, amount and timestamp values.
        /// </summary>
        void GenerateCSV();
    }

    public abstract class GeneratorTemplate
    {
        protected static readonly ILog log = LogManager.GetLogger(typeof(Generator));
        protected readonly Random random = new();

        /// <summary>
        /// Generates a line for the .csv file.
        /// </summary>
        /// <returns>A string in the form of: MSISDN;AMOUNT;TIMESTAMP</returns>
        protected string GenerateCSVLine()
        {
            return $"{GenerateMSISDN()};{GenerateAmount()};{GenerateTimestamp()}";
        }

        /// <summary>
        /// Generates a random MSISDN tag. Always starts with HR3859 followed by any of [1, 2, 7, 8, 9] after which is a random digit, and in the end a radnom 5 or six digit number.
        /// </summary>
        /// <returns>A random MSISDN tag in the form of: HR3859[12789][1-9]\b\d{5,6}\b$</returns>
        protected abstract string GenerateMSISDN();

        /// <summary>
        /// Generates a random amount in cents between 1 and a 10000.
        /// </summary>
        /// <returns>A random int value [1, 10000]</returns>
        protected abstract int GenerateAmount();

        /// <summary>
        /// Generates a random date with a timestamp between the current date and the start of the month.
        /// </summary>
        /// <returns>A random date with a timestamp.</returns>
        protected abstract DateTime GenerateTimestamp();

    }


    public class Generator : GeneratorTemplate, IGenerator
    {
        private int lineNo;

        public Generator(int lineNo = -1)
        {
            this.lineNo = lineNo;
        }

        DataParameters dataParameters = new DataParameters
        {
            MIN_AMOUNT = 1,
            MAX_AMOUNT = 10000,
            CURRENT_MONTH = (byte)DateTime.Now.Month,
            CURRENT_YEAR = (short)DateTime.Now.Year,
            MSISDN_START = "HR3859",
            secondMSDigit = new byte[] { 1, 2, 7, 8, 9 }
        };


        protected override int GenerateAmount()
        {
            return random.Next(dataParameters.MIN_AMOUNT, dataParameters.MAX_AMOUNT);
        }


        protected override string GenerateMSISDN()
        {
            var sixDigit = random.NextDouble() > 0.5 ? 6 : 5;

            StringBuilder stringBuilder = new();

            stringBuilder.Append(dataParameters.MSISDN_START);
            stringBuilder.Append(dataParameters.secondMSDigit[random.Next(0, dataParameters.secondMSDigit.Length - 1)]);
            stringBuilder.Append(random.Next(1, 9));

            for (int i = 0; i < sixDigit; i++)
            {
                stringBuilder.Append(random.Next(0, 9));
            }

            return stringBuilder.ToString();
        }

        protected override DateTime GenerateTimestamp()
        {
            var day = (byte)random.Next(1, DateTime.Now.Day);
            var hours = (byte)random.Next(0, DateTime.Now.Hour);
            var minutes = (byte)random.Next(0, DateTime.Now.Minute);
            var seconds = (byte)random.Next(0, DateTime.Now.Second);

            return new DateTime(dataParameters.CURRENT_YEAR, dataParameters.CURRENT_MONTH, day, hours, minutes, seconds);
        }

        public void GenerateCSV()
        {
            DateTime currentTime = DateTime.Now;
            var parameters = ConfigReader.ReadConfigData();

            string[] headers = { "MSISDN", "Amount", "Timestamp" };
            var fileName = string.Format("TestData_{0}-{1}-{2}_{3}-{4}-{5}.csv", currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour, currentTime.Minute, currentTime.Second);

            using StreamWriter writer = new(parameters.InputFolder + "\\" + fileName);
            writer.WriteLine(string.Join(";", headers));

            if (lineNo == -1)
                lineNo = parameters.NumOfLines;

            log.Info($"Generating: {fileName} with: {lineNo} lines...");
            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < lineNo; i++)
            {
                writer.WriteLine(GenerateCSVLine());
            }

            sw.Stop();

            writer.Close();
            log.Info($"Finished generating {fileName} in {sw.ElapsedMilliseconds} miliseconds.");

        }

    }
}