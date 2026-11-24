namespace Pomodoro;

static class Ui
{
    const int BlockHeight = 11;
    const int ValueColumn = 11;

    public static void Draw(Canvas c, Session s, bool muted, double t)
    {
        if (c.Width < 20 || c.Height < 5)
        {
            c.Text(0, 0, Format.Clock(s.Remaining), Theme.Of(s.Phase).A, true);
            return;
        }

        var panels = c.Width >= 60;
        var timeline = c.Width >= 50;
        int MainHeight() => c.Height - (panels ? 6 : 0) - (timeline ? 4 : 0);
        if (MainHeight() < 14)
        {
            timeline = false;
        }

        if (MainHeight() < 14)
        {
            panels = false;
        }

        if (MainHeight() < 13 || c.Width < 46)
        {
            DrawCompact(c, s);
            return;
        }

        var y = MainHeight();
        DrawMain(c, s, muted, t, y);
        if (panels)
        {
            var half = c.Width / 2;
            DrawSession(c, s, t, 0, y, half);
            DrawStats(c, s, half, y, c.Width - half);
            y += 6;
        }

        if (timeline)
        {
            DrawTimeline(c, s, y);
        }
    }

    public static string Title(Session s) =>
        $"{(s.Phase == Phase.Focus ? "🍅" : "☕")} {Format.Clock(s.Remaining)} · {Theme.Of(s.Phase).Name}{(s.Paused ? " (paused)" : "")}";

    static void DrawMain(Canvas c, Session s, bool muted, double t, int height)
    {
        var theme = Theme.Of(s.Phase);
        var border = Accent(s, theme);
        c.Box(0, 0, c.Width, height, border);
        c.Tab(2, 0, true, border, ("pomodoro", Palette.Bright, true));

        var now = DateTime.Now.ToString("HH:mm:ss");
        var clockEnd = c.Tab((c.Width - now.Length - 2) / 2, 0, true, border, (now, Palette.Text, false));

        var settings = s.Settings;
        var plan = $"{Format.Short(settings.LengthOf(Phase.Focus))} · {Format.Short(settings.LengthOf(Phase.ShortBreak))} · {Format.Short(settings.LengthOf(Phase.LongBreak))} ×{settings.Rounds}";
        var planStart = c.Width - 3 - (plan.Length + 2);
        if (planStart > clockEnd + 1)
        {
            c.Tab(planStart, 0, true, border, (plan, Palette.Dim, false));
        }

        var top = 1 + (height - 2 - BlockHeight) / 2;
        DrawLabel(c, s, theme, t, top);
        DrawClock(c, s, theme, t, top + 2);
        DrawMeter(c, s, theme, top + 2 + BigFont.Height + 1);
        DrawInfo(c, s, theme, top + 2 + BigFont.Height + 2);
        DrawKeys(c, s, muted, height - 1, border);
    }

    static void DrawLabel(Canvas c, Session s, Theme theme, double t, int y)
    {
        const string separator = "  ·  ";
        var name = string.Join("   ", theme.Name.ToUpperInvariant().Split(' ').Select(word => string.Join(' ', word.AsEnumerable())));
        var round = $"round {s.Round + 1}/{s.Settings.Rounds}";
        var length = 3 + name.Length + separator.Length + round.Length + (s.Paused ? separator.Length + 6 : 0);
        var x = (c.Width - length) / 2;

        if (s.Paused)
        {
            c.Put(x, y, '‖', Palette.Amber.Scale(0.6 + 0.4 * Wave(t, 1.6)), true);
        }
        else
        {
            c.Put(x, y, '●', theme.A.Scale(0.45 + 0.55 * Wave(t, 2.4)));
        }

        x += 3;
        for (var i = 0; i < name.Length; i++)
        {
            c.Put(x + i, y, name[i], theme.At(i / Math.Max(1.0, name.Length - 1)), true);
        }

        x += name.Length;
        c.Text(x, y, separator, Palette.Muted);
        x += separator.Length;
        c.Text(x, y, round, Palette.Dim);
        x += round.Length;
        if (s.Paused)
        {
            c.Text(x, y, separator, Palette.Muted);
            c.Text(x + separator.Length, y, "paused", Palette.Amber, true);
        }
    }

    static void DrawClock(Canvas c, Session s, Theme theme, double t, int y)
    {
        var rows = BigFont.Render(Format.Clock(s.Remaining));
        var width = rows[0].Length;
        var x = (c.Width - width) / 2;
        var level = s.Paused ? 0.45 + 0.3 * Wave(t, 2.2) : 1.0;

        for (var r = 0; r < rows.Length; r++)
        {
            for (var i = 0; i < width; i++)
            {
                var ch = rows[r][i];
                if (ch == ' ')
                {
                    continue;
                }

                var color = theme.At(0.65 * r / (rows.Length - 1) + 0.35 * i / (width - 1)).Scale(level);
                c.Put(x + i, y + r, ch, BigFont.IsShadow(ch) ? color.Scale(0.38) : color);
            }
        }
    }

    static void DrawMeter(Canvas c, Session s, Theme theme, int y)
    {
        var width = Math.Clamp(c.Width - 26, 20, 60);
        var percent = $"{(int)(s.Progress * 100),3}%";
        var x = (c.Width - width - 1 - percent.Length) / 2;
        DrawMeterBar(c, s, theme, x, y, width);
        c.Text(x + width + 1, y, percent, Palette.Text);
    }

    static void DrawMeterBar(Canvas c, Session s, Theme theme, int x, int y, int width)
    {
        var filled = s.Progress * width;
        for (var i = 0; i < width; i++)
        {
            var lit = Math.Clamp(filled - i, 0, 1);
            c.Put(x + i, y, '■', Rgb.Lerp(Palette.Faint, theme.At(i / Math.Max(1.0, width - 1)), lit));
        }
    }

    static void DrawInfo(Canvas c, Session s, Theme theme, int y)
    {
        if (s.ChangedAgo is { TotalSeconds: < 6 } && s.LastFinished is { } finished)
        {
            var message = !s.LastCompleted
                ? $"» skipped {Theme.Of(finished).Name}"
                : finished == Phase.Focus
                    ? $"✓ focus complete · enjoy the {theme.Name}"
                    : "✓ break over · back to focus";
            Centered(c, y, (message, s.LastCompleted ? theme.A : Palette.Dim));
            return;
        }

        var (next, length) = s.Next;
        var nextTheme = Theme.Of(next);
        Centered(
            c,
            y,
            ("ends ", Palette.Dim),
            ((DateTime.Now + s.Remaining).ToString("HH:mm"), Palette.Text),
            ("  ·  next ", Palette.Dim),
            (nextTheme.Name, nextTheme.A),
            ($" {Format.Short(length)}", Palette.Dim));
    }

    static void DrawKeys(Canvas c, Session s, bool muted, int y, Rgb border)
    {
        (string Key, string Label)[] keys =
        [
            ("space", s.Paused ? "resume" : "pause"),
            ("s", "skip"),
            ("r", "restart"),
            ("+/-", "time"),
            ("m", muted ? "unmute" : "mute"),
            ("q", "quit"),
        ];

        var x = 2;
        foreach (var (key, label) in keys)
        {
            if (x + key.Length + label.Length + 3 > c.Width - 2)
            {
                break;
            }

            x = c.Tab(x, y, false, border, (key, Palette.Bright, true), (" " + label, Palette.Dim, false)) + 1;
        }
    }

    static void DrawSession(Canvas c, Session s, double t, int x, int y, int w)
    {
        var theme = Theme.Of(s.Phase);
        var (next, nextLength) = s.Next;
        var rounds = s.Settings.Rounds;
        c.Box(x, y, w, 6, Palette.Border);
        c.Tab(x + 2, y, true, Palette.Border, ("session", Palette.Bright, true));
        c.ClipRight = x + w - 1;

        Row(c, x + 2, y + 1, "phase", ("● ", theme.A), (theme.Name, Palette.Text));

        c.Text(x + 2, y + 2, "round", Palette.Dim);
        var dot = x + 2 + ValueColumn;
        for (var i = 0; i < rounds; i++)
        {
            var done = i < s.Round || (i == s.Round && s.Phase != Phase.Focus);
            var current = i == s.Round && s.Phase == Phase.Focus;
            var (glyph, color) = done
                ? ('●', Theme.Focus.A)
                : current ? ('◉', Theme.Focus.A.Scale(0.5 + 0.5 * Wave(t, 1.8))) : ('○', Palette.Muted);
            c.Put(dot, y + 2, glyph, color);
            dot += 2;
        }

        c.Text(dot + 1, y + 2, $"{s.Round + 1}/{rounds}", Palette.Dim);

        var nextTheme = Theme.Of(next);
        Row(c, x + 2, y + 3, "next", ("● ", nextTheme.A), (nextTheme.Name, Palette.Text), ($" · {Format.Short(nextLength)}", Palette.Dim));
        Row(c, x + 2, y + 4, "cycle", ("ends at ", Palette.Dim), (s.CycleEnd.ToString("HH:mm"), Palette.Text));
        c.ClipRight = c.Width;
    }

    static void DrawStats(Canvas c, Session s, int x, int y, int w)
    {
        var (breaks, breakTime) = s.Breaks;
        c.Box(x, y, w, 6, Palette.Border);
        c.Tab(x + 2, y, true, Palette.Border, ("stats", Palette.Bright, true));
        c.ClipRight = x + w - 1;

        Row(c, x + 2, y + 1, "pomodoros", ($"{s.Pomodoros}  ", Palette.Bright), (new string('●', Math.Min(s.Pomodoros, 24)), Theme.Focus.A));
        Row(c, x + 2, y + 2, "focused", (Format.Long(s.Focused), Palette.Text));
        Row(c, x + 2, y + 3, "breaks", ($"{breaks}", Palette.Text), ($" · {Format.Long(breakTime)}", Palette.Dim));
        Row(c, x + 2, y + 4, "started", (s.StartedAt.ToString("HH:mm"), Palette.Text), ($" · {Format.Long(DateTime.Now - s.StartedAt)} ago", Palette.Dim));
        c.ClipRight = c.Width;
    }

    static void DrawTimeline(Canvas c, Session s, int y)
    {
        c.Box(0, y, c.Width, 4, Palette.Border);
        c.Tab(2, y, true, Palette.Border, ("timeline", Palette.Bright, true));

        var segments = s.History.Select(h => (h.Phase, Seconds: h.Length.TotalSeconds, Lit: true)).ToList();
        segments.Add((s.Phase, s.Elapsed.TotalSeconds, true));
        segments.Add((s.Phase, s.Remaining.TotalSeconds, false));
        segments.AddRange(s.Upcoming().Select(u => (u.Phase, u.Length.TotalSeconds, false)));

        var total = segments.Sum(x => x.Seconds);
        var now = s.History.Sum(h => h.Length.TotalSeconds) + s.Elapsed.TotalSeconds;
        var (x0, width) = (2, c.Width - 4);

        for (var i = 0; i < width; i++)
        {
            var at = (i + 0.5) / width * total;
            var k = 0;
            var end = segments[0].Seconds;
            while (k < segments.Count - 1 && at >= end)
            {
                end += segments[++k].Seconds;
            }

            var color = Theme.Of(segments[k].Phase).At(0.35);
            c.Put(x0 + i, y + 1, '▆', segments[k].Lit ? color : Rgb.Lerp(Palette.Faint, color, 0.3));
        }

        var start = s.StartedAt.ToString("HH:mm");
        var finish = s.CycleEnd.ToString("HH:mm");
        var marker = x0 + (int)Math.Min(width - 1, now / total * width);
        if (marker > x0 + start.Length)
        {
            c.Text(x0, y + 2, start, Palette.Dim);
        }

        if (marker < x0 + width - finish.Length - 1)
        {
            c.Text(x0 + width - finish.Length, y + 2, finish, Palette.Dim);
        }

        c.Text(marker, y + 2, marker + 5 < x0 + width - finish.Length - 1 ? "▲ now" : "▲", Palette.Bright);
    }

    static void DrawCompact(Canvas c, Session s)
    {
        var theme = Theme.Of(s.Phase);
        var mid = c.Height / 2;
        c.Box(0, 0, c.Width, c.Height, Accent(s, theme));
        Centered(c, mid - 1, (theme.Name.ToUpperInvariant() + (s.Paused ? " · PAUSED" : ""), s.Paused ? Palette.Amber : theme.A));
        Centered(c, mid, (Format.Clock(s.Remaining), Palette.Bright));
        DrawMeterBar(c, s, theme, 3, mid + 1, c.Width - 6);
    }

    static Rgb Accent(Session s, Theme theme)
    {
        var flash = s.ChangedAgo is { TotalSeconds: < 2.4 } ago && (int)(ago.TotalSeconds * 5) % 2 == 0;
        return flash ? theme.A : Rgb.Lerp(Palette.Border, theme.A, 0.45);
    }

    static void Row(Canvas c, int x, int y, string label, params (string Text, Rgb Color)[] parts)
    {
        c.Text(x, y, label, Palette.Dim);
        var cx = x + ValueColumn;
        foreach (var (text, color) in parts)
        {
            c.Text(cx, y, text, color);
            cx += text.Length;
        }
    }

    static void Centered(Canvas c, int y, params (string Text, Rgb Color)[] parts)
    {
        var x = (c.Width - parts.Sum(p => p.Text.Length)) / 2;
        foreach (var (text, color) in parts)
        {
            c.Text(x, y, text, color);
            x += text.Length;
        }
    }

    static double Wave(double t, double period) => 0.5 + 0.5 * Math.Sin(t * Math.Tau / period);
}
