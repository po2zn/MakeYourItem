using System;

// Pure clock: game hours in [0, 24), independent of the blueprint language.
public sealed class ForgeClock
{
    public int Day { get; private set; } = 1;
    public double Hour { get; private set; }
    public event Action<int> DayChanged;
    public ForgeClock(double startHour = 8)
    {
        if (double.IsNaN(startHour) || double.IsInfinity(startHour) || startHour < 0 || startHour >= 24)
            throw new ArgumentOutOfRangeException(nameof(startHour));
        Hour = startHour;
    }
    public void Restore(int day,double hour)
    {
        if(day<1||double.IsNaN(hour)||double.IsInfinity(hour)||hour<0||hour>=24)throw new ArgumentOutOfRangeException();
        Day=day;Hour=hour;DayChanged?.Invoke(Day);
    }
    public void Advance(double realSeconds, double secondsPerDay)
    {
        if (double.IsNaN(realSeconds) || double.IsInfinity(realSeconds) || realSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(realSeconds));
        if (double.IsNaN(secondsPerDay) || double.IsInfinity(secondsPerDay) || secondsPerDay <= 0)
            throw new ArgumentOutOfRangeException(nameof(secondsPerDay));
        double total = Hour + realSeconds * (24 / secondsPerDay);
        if (double.IsInfinity(total) || total / 24 > int.MaxValue - Day)
            throw new ArgumentOutOfRangeException(nameof(realSeconds));
        int elapsedDays = (int)Math.Floor(total / 24);
        Hour = total % 24;
        Day += elapsedDays;
        // One notification of the current date, even after a long frame.
        if (elapsedDays > 0) DayChanged?.Invoke(Day);
    }
}
