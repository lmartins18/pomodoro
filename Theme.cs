namespace Pomodoro;

sealed record Theme(string Name, Rgb A, Rgb B)
{
    public static readonly Theme Focus = new("focus", Rgb.Hex("#FF8A5B"), Rgb.Hex("#FF2E63"));
    public static readonly Theme ShortBreak = new("short break", Rgb.Hex("#5EF2B5"), Rgb.Hex("#14B8C4"));
    public static readonly Theme LongBreak = new("long break", Rgb.Hex("#7AA2FF"), Rgb.Hex("#C17BFF"));

    public static Theme Of(Phase phase) => phase switch
    {
        Phase.Focus => Focus,
        Phase.ShortBreak => ShortBreak,
        _ => LongBreak,
    };

    public Rgb At(double t) => Rgb.Lerp(A, B, t);
}

static class Palette
{
    public static readonly Rgb Bright = Rgb.Hex("#F0F6FC");
    public static readonly Rgb Text = Rgb.Hex("#C9D1D9");
    public static readonly Rgb Dim = Rgb.Hex("#7D8590");
    public static readonly Rgb Muted = Rgb.Hex("#484F58");
    public static readonly Rgb Faint = Rgb.Hex("#2A2F37");
    public static readonly Rgb Border = Rgb.Hex("#3D444D");
    public static readonly Rgb Amber = Rgb.Hex("#FFC857");
}

static class Format
{
    public static string Clock(TimeSpan remaining)
    {
        var seconds = (int)Math.Ceiling(remaining.TotalSeconds);
        return $"{seconds / 60:00}:{seconds % 60:00}";
    }

    public static string Short(TimeSpan span)
    {
        if (span.TotalSeconds < 60)
        {
            return $"{(int)Math.Round(span.TotalSeconds)}s";
        }

        return span.Seconds == 0 ? $"{(int)span.TotalMinutes}m" : $"{(int)span.TotalMinutes}m {span.Seconds}s";
    }

    public static string Long(TimeSpan span) =>
        span.TotalHours >= 1 ? $"{(int)span.TotalHours}h {span.Minutes:00}m" : $"{span.Minutes}m {span.Seconds:00}s";
}
