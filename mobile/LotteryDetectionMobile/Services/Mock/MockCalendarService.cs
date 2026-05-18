using LotteryDetectionMobile.Models.Family;
using LotteryDetectionMobile.Services.Interfaces;

namespace LotteryDetectionMobile.Services.Mock;

public class MockCalendarService : ICalendarService
{
    public static ICalendarService Instance { get; } = new MockCalendarService();

    public Task<IEnumerable<CalendarEvent>> GetUpcomingEventsAsync()
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var seeds = new (int dayOffset, double startHour, double durHours, string title, string member, string owner)[]
        {
            (-2, 9.0,  1.0, "Standup",         "sam",    "Sam"),
            (-2, 16.0, 1.5, "Practice",        "jordan", "Jordan"),
            (-1, 11.0, 1.0, "Vet appt",        "alex",   "Alex"),
            ( 0, 14.0, 0.5, "Bath",            "riley",  "Riley"),
            ( 0, 19.0, 1.0, "Family movie",    "home",   "Home"),
            ( 1, 10.0, 2.0, "Field trip",      "jordan", "Jordan"),
            ( 1, 17.0, 0.7, "Pickup Sam",      "alex",   "Alex"),
            ( 2, 18.0, 2.0, "Date night",      "home",   "Home"),
            ( 3, 10.0, 2.0, "Soccer game",     "sam",    "Sam"),
            ( 4,  9.0, 1.0, "Brunch",          "alex",   "Alex"),
            ( 5, 14.5, 1.0, "Dentist",         "riley",  "Riley"),
            ( 7, 16.0, 1.0, "Tutor",           "jordan", "Jordan"),
            ( 9, 15.0, 2.0, "Birthday party",  "riley",  "Riley"),
        };

        var events = seeds.Select((s, i) => new CalendarEvent
        {
            Id = $"mock-{i}",
            Title = s.title,
            Owner = s.owner,
            Member = s.member,
            Start = today.AddDays(s.dayOffset).AddHours(s.startHour),
            End = today.AddDays(s.dayOffset).AddHours(s.startHour + s.durHours),
            Status = s.dayOffset < 0 ? "Past" : "Scheduled",
            Location = string.Empty,
            Color = MemberHex(s.member)
        }).ToList();

        return Task.FromResult(events.AsEnumerable());
    }

    private static string MemberHex(string member) => member switch
    {
        "alex"   => "#FF8A65",
        "sam"    => "#42A5F5",
        "jordan" => "#AB47BC",
        "riley"  => "#26A69A",
        _        => "#6C63FF"
    };
}
