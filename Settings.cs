using System.Globalization;

namespace Pomodoro;

sealed record Settings(double Focus, double Short, double Long, int Rounds, bool Mute, (int W, int H)? Frame)
{
    public const string Help = """
        pomodoro - terminal pomodoro timer

        usage: pomodoro [focus] [short] [long] [rounds] [options]

          -f, --focus <min>    focus length            (default 25)
          -s, --short <min>    short break length      (default 5)
          -l, --long <min>     long break length       (default 15)
          -r, --rounds <n>     focus rounds per cycle  (default 4)
          -m, --mute           start with sound off
              --frame <WxH>    print one frame as plain text and exit
          -h, --help           show this help

        minutes can be fractional:  pomodoro 0.2 0.1 0.15 2   (quick demo)

        keys  space pause/resume · s skip · r restart · +/- adjust 1 min · m mute · q quit
        """;

    public TimeSpan LengthOf(Phase phase) => TimeSpan.FromMinutes(phase switch
    {
        Phase.Focus => Focus,
        Phase.ShortBreak => Short,
        _ => Long,
    });

    public static Settings? Parse(string[] args)
    {
        double focus = 25;
        double shortBreak = 5;
        double longBreak = 15;
        var rounds = 4;
        var mute = false;
        (int, int)? frame = null;
        var positional = 0;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            string Value() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{arg} needs a value");

            switch (arg)
            {
                case "-h" or "--help" or "/?":
                    return null;
                case "-m" or "--mute":
                    mute = true;
                    break;
                case "-f" or "--focus":
                    focus = Minutes(Value());
                    break;
                case "-s" or "--short":
                    shortBreak = Minutes(Value());
                    break;
                case "-l" or "--long":
                    longBreak = Minutes(Value());
                    break;
                case "-r" or "--rounds":
                    rounds = RoundCount(Value());
                    break;
                case "--frame":
                    frame = Size(Value());
                    break;
                default:
                    switch (positional++)
                    {
                        case 0: focus = Minutes(arg); break;
                        case 1: shortBreak = Minutes(arg); break;
                        case 2: longBreak = Minutes(arg); break;
                        case 3: rounds = RoundCount(arg); break;
                        default: throw new ArgumentException($"unexpected argument '{arg}'");
                    }
                    break;
            }
        }

        return new(focus, shortBreak, longBreak, rounds, mute, frame);
    }

    static double Minutes(string text) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value > 0 && value <= 99
            ? value
            : throw new ArgumentException($"'{text}' is not a valid number of minutes (0-99)");

    static int RoundCount(string text) =>
        int.TryParse(text, out var value) && value is >= 1 and <= 12
            ? value
            : throw new ArgumentException($"'{text}' is not a valid round count (1-12)");

    static (int, int) Size(string text) =>
        text.Split('x') is [var w, var h] && int.TryParse(w, out var width) && int.TryParse(h, out var height) && width > 0 && height > 0
            ? (width, height)
            : throw new ArgumentException($"'{text}' is not a size like 100x30");
}
