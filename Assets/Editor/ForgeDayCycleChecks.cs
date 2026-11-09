using System;
public static class ForgeDayCycleChecks
{
    static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static string Run()
    {
        var clock = new ForgeClock(23.5); int notifications = 0, notifiedDay = 0;
        clock.DayChanged += day => { notifications++; notifiedDay = day; };
        clock.Advance(.5, 24);
        Require(clock.Day == 2 && clock.Hour == 0 && notifications == 1 && notifiedDay == 2, "exact midnight");
        clock.Advance(49.25, 24);
        Require(clock.Day == 4 && clock.Hour == 1.25 && notifications == 2 && notifiedDay == 4, "multiple days and remainder");
        clock.Advance(0, 600); Require(notifications == 2, "paused clock");
        var normal = new ForgeClock(); normal.Advance(600, 600);
        Require(normal.Day == 2 && normal.Hour == 8, "configured day length");
        try { normal.Advance(-1, 600); throw new Exception("negative accepted"); } catch (ArgumentOutOfRangeException) { }
        try { normal.Advance(1, 0); throw new Exception("zero duration accepted"); } catch (ArgumentOutOfRangeException) { }
        Require(ForgeDayCycle.Fade(0) == 0 && ForgeDayCycle.Fade(.4f) > 0 && ForgeDayCycle.Fade(.8f) == 1 && ForgeDayCycle.Fade(2) == 1 && ForgeDayCycle.Fade(3) < 1 && ForgeDayCycle.Fade(4) == 0, "announcement envelope");
        return "PASS day cycle: midnight, long frame, pause, duration, invalid input, fade";
    }
#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/MakeYourItem/Validate Day Cycle")]
    public static void ValidateInEditor() { UnityEngine.Debug.Log(Run()); }
#endif
}
