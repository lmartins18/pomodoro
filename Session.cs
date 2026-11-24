using System.Diagnostics;

namespace Pomodoro;

enum Phase { Focus, ShortBreak, LongBreak }

sealed record Segment(Phase Phase, TimeSpan Length, bool Completed);

sealed class Session(Settings settings)
{
    readonly Stopwatch clock = Stopwatch.StartNew();
    TimeSpan banked;
    TimeSpan? runningSince = TimeSpan.Zero;
    TimeSpan? changedAt;

    public Settings Settings { get; } = settings;
    public Phase Phase { get; private set; } = Phase.Focus;
    public int Round { get; private set; }
    public TimeSpan Duration { get; private set; } = settings.LengthOf(Phase.Focus);
    public DateTime StartedAt { get; } = DateTime.Now;
    public List<Segment> History { get; } = [];
    public Phase? LastFinished { get; private set; }
    public bool LastCompleted { get; private set; }

    public bool Paused => runningSince is null;
    public TimeSpan Elapsed => banked + (runningSince is { } since ? clock.Elapsed - since : TimeSpan.Zero);
    public TimeSpan Remaining => Elapsed >= Duration ? TimeSpan.Zero : Duration - Elapsed;
    public double Progress => Math.Clamp(Elapsed / Duration, 0, 1);
    public TimeSpan? ChangedAgo => changedAt is { } at ? clock.Elapsed - at : null;

    public (Phase Phase, TimeSpan Length) Next
    {
        get
        {
            var (phase, _) = Following(Phase, Round);
            return (phase, Settings.LengthOf(phase));
        }
    }

    public DateTime CycleEnd => DateTime.Now + Remaining + Upcoming().Aggregate(TimeSpan.Zero, (sum, u) => sum + u.Length);

    public int Pomodoros => History.Count(x => x.Phase == Phase.Focus && x.Completed);

    public TimeSpan Focused => SumOf(x => x == Phase.Focus);

    public (int Count, TimeSpan Time) Breaks =>
        (History.Count(x => x.Phase != Phase.Focus && x.Completed), SumOf(x => x != Phase.Focus));

    public IEnumerable<(Phase Phase, TimeSpan Length)> Upcoming()
    {
        var (phase, round) = (Phase, Round);
        while (phase != Phase.LongBreak)
        {
            (phase, round) = Following(phase, round);
            yield return (phase, Settings.LengthOf(phase));
        }
    }

    public bool Tick()
    {
        if (Paused || Elapsed < Duration)
        {
            return false;
        }

        Advance(completed: true);
        return true;
    }

    public void TogglePause()
    {
        if (Paused)
        {
            runningSince = clock.Elapsed;
        }
        else
        {
            banked = Elapsed;
            runningSince = null;
        }
    }

    public void Skip() => Advance(completed: false);

    public void Restart()
    {
        banked = TimeSpan.Zero;
        if (!Paused)
        {
            runningSince = clock.Elapsed;
        }
    }

    public void Adjust(int minutes)
    {
        var floor = Math.Min(1, Duration.TotalMinutes);
        Duration = TimeSpan.FromMinutes(Math.Clamp(Duration.TotalMinutes + minutes, floor, 99));
    }

    void Advance(bool completed)
    {
        History.Add(new Segment(Phase, completed ? Duration : Elapsed, completed));
        LastFinished = Phase;
        LastCompleted = completed;
        (Phase, Round) = Following(Phase, Round);
        Duration = Settings.LengthOf(Phase);
        banked = TimeSpan.Zero;
        if (!Paused)
        {
            runningSince = clock.Elapsed;
        }

        changedAt = clock.Elapsed;
    }

    (Phase, int) Following(Phase phase, int round) => phase switch
    {
        Phase.Focus => (round + 1 >= Settings.Rounds ? Phase.LongBreak : Phase.ShortBreak, round),
        Phase.ShortBreak => (Phase.Focus, round + 1),
        _ => (Phase.Focus, 0),
    };

    TimeSpan SumOf(Func<Phase, bool> match) =>
        History.Where(x => match(x.Phase)).Aggregate(TimeSpan.Zero, (sum, x) => sum + x.Length)
        + (match(Phase) ? Elapsed : TimeSpan.Zero);
}
