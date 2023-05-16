
using log4net;

using TestAppLogic;

namespace TestApp;
internal class ConsoleApp
{
    private const string CMD_GENERATE = "generate";

    private const string CMD_PARSE = "parse";

    private static readonly ILog log = LogManager.GetLogger(typeof(ConsoleApp));

    private static int CheckUserInput(string[] args)
    {
        IGenerator? generator = null;
        IParser? parser = null;

        if (args.Length == 0)
        {
            log.Error("No arguments provided! - Specify at least one argument: <<generate>> or <<parse>>");
            return (int)ErrorCode.NO_ARGS;
        }

        if (args.Length == 1)
        {
            var cmd = args[0];

            if (cmd.ToLower().Equals(CMD_GENERATE))
            {
                generator = new Generator();
            }
            else if (cmd.ToLower().Equals(CMD_PARSE))
            {
                parser = new Parser();
            }
        }
        else if (args.Length == 2 && args[0].Equals(CMD_GENERATE))
        {
            var success = int.TryParse(args[1], out int numOfLines);

            if (!success)
            {
                log.Error("Second argument needs to be of type int.");
                return (int)ErrorCode.INVALID_RANGE;
            }

            if (numOfLines != 0)
            {
                generator = new Generator(numOfLines);
            }
            else
            {
                log.Error("Second argument must be non-zero.");
                return (int)ErrorCode.NON_ZERO;
            }
        }
        else if (args.Length == 2 && args[0].Equals(CMD_PARSE))
        {
            parser = new Parser(args[1]);
        }

        else
        {
            log.Error("Provided " + args.Length + "args, expected 2!");
            return (int)ErrorCode.TOO_MANY_ARGS;
        }

        if (generator != null && args[0].ToLower().Equals(CMD_GENERATE))
        {
            generator.GenerateCsv();
        }
        else if (parser != null && args[0].ToLower().Equals(CMD_PARSE))
        {
            parser.Parse();
        }

        return 0;
    }

    private static int Main(string[] args)
    {
        log.Info("Starting execution.");

        _ = CheckUserInput(args);

        log.Info("Done with executing.");
        return 0;
    }
}