using System.Diagnostics;
using System.Text;
using Pomodoro;

Settings? settings;
try
{
    settings = Settings.Parse(args);
}
catch (ArgumentException e)
{
    Console.Error.WriteLine($"pomodoro: {e.Message}\n");
    Console.Error.WriteLine(Settings.Help);
    return 1;
}

if (settings is null)
{
    Console.WriteLine(Settings.Help);
    return 0;
}

if (settings.Frame is { } frame)
{
    var preview = new Canvas(frame.W, frame.H);
    Ui.Draw(preview, new Session(settings), settings.Mute, 0);
    Console.OutputEncoding = Encoding.UTF8;
    Console.Write(preview.ToPlainText());
    return 0;
}

using var terminal = new Terminal();
var session = new Session(settings);
var muted = settings.Mute;
var colors = settings.Color ?? Colors.Detect();
var animation = Stopwatch.StartNew();
Canvas? previous = null;
var title = "";

while (true)
{
    while (Console.KeyAvailable)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key is ConsoleKey.Q or ConsoleKey.Escape
            || (key.Key == ConsoleKey.C && key.Modifiers.HasFlag(ConsoleModifiers.Control)))
        {
            return 0;
        }

        switch (key.Key)
        {
            case ConsoleKey.Spacebar or ConsoleKey.P:
                session.TogglePause();
                break;
            case ConsoleKey.S or ConsoleKey.N:
                session.Skip();
                break;
            case ConsoleKey.R:
                session.Restart();
                break;
            case ConsoleKey.M:
                muted = !muted;
                break;
            case ConsoleKey.OemPlus or ConsoleKey.Add or ConsoleKey.UpArrow:
                session.Adjust(1);
                break;
            case ConsoleKey.OemMinus or ConsoleKey.Subtract or ConsoleKey.DownArrow:
                session.Adjust(-1);
                break;
        }
    }

    if (session.Tick() && !muted && session.LastFinished is { } finished)
    {
        Sound.Chime(finished, terminal);
    }

    var (width, height) = (Console.WindowWidth, Console.WindowHeight);
    if (width > 0 && height > 0)
    {
        var canvas = new Canvas(width, height);
        Ui.Draw(canvas, session, muted, animation.Elapsed.TotalSeconds);
        terminal.Write(canvas.Render(previous, colors));
        previous = canvas;
    }

    var nextTitle = Ui.Title(session);
    if (nextTitle != title)
    {
        terminal.Write($"\e]0;{nextTitle}\a");
        title = nextTitle;
    }

    Thread.Sleep(40);
}
