using System.Text;

namespace Pomodoro;

readonly record struct Rgb(byte R, byte G, byte B)
{
    public static Rgb Hex(string hex)
    {
        var value = Convert.ToInt32(hex.TrimStart('#'), 16);
        return new Rgb((byte)(value >> 16), (byte)((value >> 8) & 0xFF), (byte)(value & 0xFF));
    }

    public static Rgb Lerp(Rgb a, Rgb b, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return new Rgb(Mix(a.R, b.R, t), Mix(a.G, b.G, t), Mix(a.B, b.B, t));
    }

    public Rgb Scale(double factor) => new(Clamp(R * factor), Clamp(G * factor), Clamp(B * factor));

    static byte Mix(byte a, byte b, double t) => (byte)Math.Round(a + (b - a) * t);

    static byte Clamp(double value) => (byte)Math.Clamp(Math.Round(value), 0, 255);
}

sealed class Canvas(int width, int height)
{
    readonly char[] chars = Enumerable.Repeat(' ', width * height).ToArray();
    readonly Rgb[] colors = new Rgb[width * height];
    readonly bool[] bolds = new bool[width * height];

    public int Width => width;
    public int Height => height;
    public int ClipRight { get; set; } = int.MaxValue;

    public void Put(int x, int y, char ch, Rgb color, bool bold = false)
    {
        if (x < 0 || y < 0 || x >= Math.Min(width, ClipRight) || y >= height)
        {
            return;
        }

        var i = y * width + x;
        chars[i] = ch;
        colors[i] = color;
        bolds[i] = bold;
    }

    public void Text(int x, int y, string text, Rgb color, bool bold = false)
    {
        for (var i = 0; i < text.Length; i++)
        {
            Put(x + i, y, text[i], color, bold);
        }
    }

    public void Box(int x, int y, int w, int h, Rgb border)
    {
        for (var i = x + 1; i < x + w - 1; i++)
        {
            Put(i, y, '─', border);
            Put(i, y + h - 1, '─', border);
        }

        for (var j = y + 1; j < y + h - 1; j++)
        {
            Put(x, j, '│', border);
            Put(x + w - 1, j, '│', border);
        }

        Put(x, y, '╭', border);
        Put(x + w - 1, y, '╮', border);
        Put(x, y + h - 1, '╰', border);
        Put(x + w - 1, y + h - 1, '╯', border);
    }

    // btop-style label cut into a border line: ┐label┌ on a top edge, ┘label└ on a bottom edge.
    public int Tab(int x, int y, bool top, Rgb border, params (string Text, Rgb Color, bool Bold)[] parts)
    {
        Put(x, y, top ? '┐' : '┘', border);
        var cx = x + 1;
        foreach (var (text, color, bold) in parts)
        {
            Text(cx, y, text, color, bold);
            cx += text.Length;
        }

        Put(cx, y, top ? '┌' : '└', border);
        return cx + 1;
    }

    // Emits only the cells that differ from the previous frame.
    public string Render(Canvas? previous)
    {
        var full = previous is null || previous.Width != width || previous.Height != height;
        var sb = new StringBuilder();
        Rgb? color = null;
        var bold = false;
        var (cursorX, cursorY) = (-1, -1);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var i = y * width + x;
                if (!full && previous!.chars[i] == chars[i] && previous.colors[i] == colors[i] && previous.bolds[i] == bolds[i])
                {
                    continue;
                }

                if (cursorX != x || cursorY != y)
                {
                    sb.Append($"\e[{y + 1};{x + 1}H");
                }

                if (chars[i] != ' ')
                {
                    if (bolds[i] != bold)
                    {
                        bold = bolds[i];
                        sb.Append(bold ? "\e[1m" : "\e[22m");
                    }

                    if (colors[i] != color)
                    {
                        color = colors[i];
                        sb.Append($"\e[38;2;{colors[i].R};{colors[i].G};{colors[i].B}m");
                    }
                }

                sb.Append(chars[i]);
                (cursorX, cursorY) = (x + 1, y);
            }
        }

        if (sb.Length == 0)
        {
            return "";
        }

        return (full ? "\e[?2026h\e[0m\e[2J" : "\e[?2026h\e[0m") + sb + "\e[0m\e[?2026l";
    }

    public string ToPlainText()
    {
        var sb = new StringBuilder();
        for (var y = 0; y < height; y++)
        {
            sb.Append(chars, y * width, width).Append('\n');
        }

        return sb.ToString();
    }
}
