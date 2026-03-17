using System.Reflection;
using CareHub.Api.Entities;
using CareHub.Api.Services;
using Xunit;

namespace CareHub.Tests;

/// <summary>
/// Tests for MarSeeder's private helper methods via reflection.
/// These methods contain core scheduling logic used to generate MAR data.
/// </summary>
public class MarSeederHelperTests
{
    private static readonly Type SeederType = typeof(MarSeeder);

    private static bool IsReminderDay(Medication med, DayOfWeek dow)
    {
        var method = SeederType.GetMethod("IsReminderDay", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (bool)method.Invoke(null, [med, dow])!;
    }

    private static List<(int hour, int minute)> GetScheduleTimes(Medication med, DayOfWeek dow)
    {
        var method = SeederType.GetMethod("GetScheduleTimes", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (List<(int, int)>)method.Invoke(null, [med, dow])!;
    }

    private static TimeSpan GetTimeSpan(Medication med, DayOfWeek dow, int slot)
    {
        var method = SeederType.GetMethod("GetTimeSpan", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (TimeSpan)method.Invoke(null, [med, dow, slot])!;
    }

    [Fact]
    public void IsReminderDay_AllDaysEnabled_ReturnsTrue()
    {
        var med = new Medication(); // all days default to true

        foreach (DayOfWeek dow in Enum.GetValues<DayOfWeek>())
            Assert.True(IsReminderDay(med, dow));
    }

    [Fact]
    public void IsReminderDay_WeekendDisabled_ReturnsFalse()
    {
        var med = new Medication { ReminderSat = false, ReminderSun = false };

        Assert.False(IsReminderDay(med, DayOfWeek.Saturday));
        Assert.False(IsReminderDay(med, DayOfWeek.Sunday));
        Assert.True(IsReminderDay(med, DayOfWeek.Monday));
    }

    [Fact]
    public void GetScheduleTimes_ThreeTimesPerDay_ReturnsThreeSlots()
    {
        var med = new Medication
        {
            TimesPerDay = 3,
            MonTime1 = new TimeSpan(8, 0, 0),
            MonTime2 = new TimeSpan(14, 0, 0),
            MonTime3 = new TimeSpan(20, 0, 0),
        };

        var times = GetScheduleTimes(med, DayOfWeek.Monday);

        Assert.Equal(3, times.Count);
        Assert.Equal((8, 0), times[0]);
        Assert.Equal((14, 0), times[1]);
        Assert.Equal((20, 0), times[2]);
    }

    [Fact]
    public void GetScheduleTimes_OneTimePerDay_ReturnsSingleSlot()
    {
        var med = new Medication
        {
            TimesPerDay = 1,
            WedTime1 = new TimeSpan(9, 30, 0),
        };

        var times = GetScheduleTimes(med, DayOfWeek.Wednesday);

        Assert.Single(times);
        Assert.Equal((9, 30), times[0]);
    }

    [Fact]
    public void GetScheduleTimes_TwoTimesPerDay_ReturnsTwoSlots()
    {
        var med = new Medication
        {
            TimesPerDay = 2,
            FriTime1 = new TimeSpan(7, 0, 0),
            FriTime2 = new TimeSpan(19, 0, 0),
        };

        var times = GetScheduleTimes(med, DayOfWeek.Friday);

        Assert.Equal(2, times.Count);
        Assert.Equal((7, 0), times[0]);
        Assert.Equal((19, 0), times[1]);
    }

    [Fact]
    public void GetTimeSpan_ReturnsCorrectDaySlotCombination()
    {
        var med = new Medication
        {
            TueTime1 = new TimeSpan(6, 0, 0),
            TueTime2 = new TimeSpan(12, 0, 0),
            TueTime3 = new TimeSpan(18, 0, 0),
        };

        Assert.Equal(new TimeSpan(6, 0, 0), GetTimeSpan(med, DayOfWeek.Tuesday, 1));
        Assert.Equal(new TimeSpan(12, 0, 0), GetTimeSpan(med, DayOfWeek.Tuesday, 2));
        Assert.Equal(new TimeSpan(18, 0, 0), GetTimeSpan(med, DayOfWeek.Tuesday, 3));
    }

    [Fact]
    public void GetTimeSpan_InvalidSlot_ReturnsDefault()
    {
        var med = new Medication();
        var result = GetTimeSpan(med, DayOfWeek.Monday, 99);
        Assert.Equal(new TimeSpan(8, 0, 0), result);
    }

    [Theory]
    [InlineData(DayOfWeek.Monday)]
    [InlineData(DayOfWeek.Tuesday)]
    [InlineData(DayOfWeek.Wednesday)]
    [InlineData(DayOfWeek.Thursday)]
    [InlineData(DayOfWeek.Friday)]
    [InlineData(DayOfWeek.Saturday)]
    [InlineData(DayOfWeek.Sunday)]
    public void GetScheduleTimes_AllDays_DefaultsConsistent(DayOfWeek dow)
    {
        var med = new Medication { TimesPerDay = 3 };
        var times = GetScheduleTimes(med, dow);

        // Default times are 08:00, 14:00, 20:00 for all days
        Assert.Equal(3, times.Count);
        Assert.Equal((8, 0), times[0]);
        Assert.Equal((14, 0), times[1]);
        Assert.Equal((20, 0), times[2]);
    }

    [Fact]
    public void GetScheduleTimes_CapsAtThreeEvenIfTimesPerDayHigher()
    {
        var med = new Medication { TimesPerDay = 5 };
        var times = GetScheduleTimes(med, DayOfWeek.Monday);

        // MarSeeder caps at Math.Min(TimesPerDay, 3)
        Assert.Equal(3, times.Count);
    }
}
