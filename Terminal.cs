using System.Runtime.InteropServices;
using System.Text;

namespace Pomodoro;

sealed class Terminal : IDisposable
{
    readonly Stream stdout = Console.OpenStandardOutput();
    readonly Encoding originalEncoding = Console.OutputEncoding;

    public Terminal()
    {
        EnableVirtualTerminal();
        Console.OutputEncoding = Encoding.UTF8;
        Console.TreatControlCAsInput = true;
        Write("\e[?1049h\e[?25l\e[2J");
    }

    public void Write(string text)
    {
        if (text.Length == 0)
        {
            return;
        }

        stdout.Write(Encoding.UTF8.GetBytes(text));
        stdout.Flush();
    }

    public void Dispose()
    {
        Write("\e[0m\e[?25h\e[?1049l");
        Console.OutputEncoding = originalEncoding;
    }

    static void EnableVirtualTerminal()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var handle = GetStdHandle(-11);
        if (GetConsoleMode(handle, out var mode))
        {
            SetConsoleMode(handle, mode | 0x0004);
        }
    }

    [DllImport("kernel32.dll")]
    static extern nint GetStdHandle(int handle);

    [DllImport("kernel32.dll")]
    static extern bool GetConsoleMode(nint handle, out uint mode);

    [DllImport("kernel32.dll")]
    static extern bool SetConsoleMode(nint handle, uint mode);
}

static class Sound
{
    public static void Chime(Phase finished, Terminal terminal)
    {
        // Console.Beep only makes a sound on Windows; everywhere else ring the terminal bell.
        if (!OperatingSystem.IsWindows())
        {
            terminal.Write("\a");
            return;
        }

        int[] notes = finished == Phase.Focus ? [659, 784, 1047, 1319] : [784, 659, 784, 1047];
        Task.Run(() =>
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            foreach (var note in notes)
            {
                Console.Beep(note, 140);
            }
        });
    }
}
