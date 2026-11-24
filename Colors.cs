namespace Pomodoro;

enum ColorMode { TrueColor, Ansi256, Ansi16 }

static class Colors
{
    static readonly int[] CubeLevels = [0, 95, 135, 175, 215, 255];

    // ANSI color numbers by 60° hue sector: red, yellow, green, cyan, blue, magenta.
    static readonly int[] HueToAnsi = [1, 3, 2, 6, 4, 5];

    public static ColorMode Detect()
    {
        var colorTerm = Environment.GetEnvironmentVariable("COLORTERM") ?? "";
        if (colorTerm is "truecolor" or "24bit" || OperatingSystem.IsWindows())
        {
            return ColorMode.TrueColor;
        }

        // Terminal.app before macOS 26 has no 24-bit color and doesn't set COLORTERM.
        if (Environment.GetEnvironmentVariable("TERM_PROGRAM") == "Apple_Terminal")
        {
            return ColorMode.Ansi256;
        }

        var term = Environment.GetEnvironmentVariable("TERM") ?? "";
        return term.Contains("256color") ? ColorMode.Ansi256 : ColorMode.Ansi16;
    }

    public static ColorMode? Parse(string text) => text.ToLowerInvariant() switch
    {
        "truecolor" or "24bit" => ColorMode.TrueColor,
        "256" => ColorMode.Ansi256,
        "16" => ColorMode.Ansi16,
        _ => null,
    };

    // Always starts from a reset, so faint/bold from the previous cell can't leak into this one.
    public static string Sgr(Rgb color, bool bold, ColorMode mode)
    {
        var weight = bold ? "1;" : "";
        return mode switch
        {
            ColorMode.TrueColor => $"\e[0;{weight}38;2;{color.R};{color.G};{color.B}m",
            ColorMode.Ansi256 => $"\e[0;{weight}38;5;{To256(color)}m",
            _ => $"\e[0;{weight}{To16(color)}m",
        };
    }

    static int To256(Rgb color)
    {
        static int Level(byte v) => v < 48 ? 0 : v < 115 ? 1 : (v - 35) / 40;

        var (r, g, b) = (Level(color.R), Level(color.G), Level(color.B));
        var cube = new Rgb((byte)CubeLevels[r], (byte)CubeLevels[g], (byte)CubeLevels[b]);
        var grayStep = Math.Clamp(((color.R + color.G + color.B) / 3 - 3) / 10, 0, 23);
        var grayValue = (byte)(8 + 10 * grayStep);
        var gray = new Rgb(grayValue, grayValue, grayValue);
        return Distance(color, gray) < Distance(color, cube) ? 232 + grayStep : 16 + 36 * r + 6 * g + b;
    }

    // The 16 base colors are whatever the terminal theme says they are: black can be the background and
    // the bright slots can be greys (Solarized). So pick a normal hue, use faint for dark, default fg for grays.
    static string To16(Rgb color)
    {
        var max = Math.Max(color.R, Math.Max(color.G, color.B));
        var min = Math.Min(color.R, Math.Min(color.G, color.B));
        if (max - min < 40)
        {
            return max >= 160 ? "39" : "2;39";
        }

        double r = color.R, g = color.G, b = color.B, d = max - min;
        var sector = max == color.R ? (g - b) / d : max == color.G ? (b - r) / d + 2 : (r - g) / d + 4;
        var ansi = HueToAnsi[((int)Math.Round(sector) % 6 + 6) % 6];
        return max < 120 ? $"2;3{ansi}" : $"3{ansi}";
    }

    static int Distance(Rgb a, Rgb b)
    {
        var (dr, dg, db) = (a.R - b.R, a.G - b.G, a.B - b.B);
        return dr * dr + dg * dg + db * db;
    }
}
