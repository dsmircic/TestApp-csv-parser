﻿using log4net;
using System.Diagnostics;
using System.Text;

namespace TestAppLogic;

/// <summary>
///     Generates a .csv file and stores it in the directory specified in the config file.
///     Every line of the .csv file contains a data entry in the form of MSISDN;AMOUNT;TIMESTAMP.
///     The value for each field is Random.
/// </summary>
public interface IGenerator
{
    /// <summary>
    ///     Generates a .csv file with the format TestData_yyyyMMdd_HHmmss with Random MSISDN, amount and timestamp values.
    /// </summary>
    void GenerateCsv();
}

public abstract class GeneratorTemplate
{
    protected static readonly ILog log = LogManager.GetLogger(typeof(Generator));
    protected readonly Random Random = new();

    /// <summary>
    ///     Generates a line for the .csv file.
    /// </summary>
    /// <returns>A string in the form of: MSISDN;AMOUNT;TIMESTAMP</returns>
    protected string GenerateCsvLine()
    {
        return $"{GenerateMsisdn()};{GenerateAmount()};{GenerateTimestamp()}";
    }

    /// <summary>
    ///     Generates a Random MSISDN tag. Always starts with HR3859 followed by any of [1, 2, 7, 8, 9] after which is a Random
    ///     digit, and in the end a random 5 or six digit number.
    /// </summary>
    /// <returns>A Random MSISDN tag in the form of: HR3859[12789][1-9]\b\d{5,6}\b$</returns>
    protected abstract string GenerateMsisdn();

    /// <summary>
    ///     Generates a Random amount in cents between 1 and a 10000.
    /// </summary>
    /// <returns>A Random int value [1, 10000]</returns>
    protected abstract int GenerateAmount();

    /// <summary>
    ///     Generates a Random date with a timestamp between the current date and the start of the month.
    /// </summary>
    /// <returns>A Random date with a timestamp.</returns>
    protected abstract DateTime GenerateTimestamp();
}

public class Generator : GeneratorTemplate, IGenerator
{
    private readonly DataParameters _dataParameters = new()
    {
        MIN_AMOUNT = 1,
        MAX_AMOUNT = 10000,
        CURRENT_MONTH = (byte)DateTime.Now.Month,
        CURRENT_YEAR = (short)DateTime.Now.Year,
        MSISDN_START = "HR3859",
        secondMSDigit = new byte[] { 1, 2, 7, 8, 9 }
    };

    private int lineNo;

    public Generator(int lineNo = -1)
    {
        this.lineNo = lineNo;
    }

    public void GenerateCsv()
    {
        DateTime currentTime = DateTime.Now;
        ConfigParameters parameters = ConfigReader.ReadConfigData();

        string[] headers = { "MSISDN", "Amount", "Timestamp" };
        string fileName = string.Format("TestData_{0}-{1}-{2}_{3}-{4}-{5}.csv", currentTime.Year, currentTime.Month,
            currentTime.Day, currentTime.Hour, currentTime.Minute, currentTime.Second);

        using StreamWriter writer = new(parameters.InputFolder + "\\" + fileName);
        writer.WriteLine(string.Join(";", headers));

        if (lineNo == -1)
        {
            lineNo = parameters.NumOfLines;
        }

        log.Info($"Generating: {fileName} with: {lineNo} lines...");
        Stopwatch sw = Stopwatch.StartNew();

        for (int i = 0; i < lineNo; i++)
        {
            writer.WriteLine(GenerateCsvLine());
        }

        sw.Stop();

        writer.Close();
        log.Info($"Finished generating {fileName} in {sw.ElapsedMilliseconds} miliseconds.");
    }


    protected override int GenerateAmount()
    {
        return Random.Next(_dataParameters.MIN_AMOUNT, _dataParameters.MAX_AMOUNT);
    }


    protected override string GenerateMsisdn()
    {
        int sixDigit = Random.NextDouble() > 0.5 ? 6 : 5;

        StringBuilder stringBuilder = new();

        _ = stringBuilder.Append(_dataParameters.MSISDN_START);
        _ = stringBuilder.Append(_dataParameters.secondMSDigit[Random.Next(0, _dataParameters.secondMSDigit.Length - 1)]);
        _ = stringBuilder.Append(Random.Next(1, 9));

        for (int i = 0; i < sixDigit; i++)
        {
            _ = stringBuilder.Append(Random.Next(0, 9));
        }

        return stringBuilder.ToString();
    }

    protected override DateTime GenerateTimestamp()
    {
        byte day = (byte)Random.Next(1, DateTime.Now.Day);
        byte hours = (byte)Random.Next(0, DateTime.Now.Hour);
        byte minutes = (byte)Random.Next(0, DateTime.Now.Minute);
        byte seconds = (byte)Random.Next(0, DateTime.Now.Second);

        return new DateTime(_dataParameters.CURRENT_YEAR, _dataParameters.CURRENT_MONTH, day, hours, minutes, seconds);
    }
}