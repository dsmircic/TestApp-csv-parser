namespace TestAppLogic;

public enum ErrorCode
{
    NON_ZERO = -1,

    INVALID_COMMAND = -2,

    TOO_MANY_ARGS = -3,

    NO_ARGS = -4,

    INVALID_RANGE = -5
}

public struct DataParameters
{
    public byte MIN_AMOUNT;

    public short MAX_AMOUNT;

    public byte CURRENT_MONTH;

    public short CURRENT_YEAR;

    public string MSISDN_START;

    public byte[] secondMSDigit;
}

public struct InputData
{
    public string MSISDN;

    public int Amount;

    public DateTime Timestamp;
}

public enum DataPosition
{
    MSISDN_POS,

    AMOUNT_POS,

    TIMESTAMP_POS
}

public struct ConfigParameters
{
    public string InputFolder;

    public string ErrorFolder;

    public string ArchiveFolder;

    public string OutputFolder;

    public int NumOfLines;
}