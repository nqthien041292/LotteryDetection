using LotteryDetectionMobile.Models.Family;

namespace LotteryDetectionMobile.Services.Interfaces;

public interface ICalendarService
{
    Task<IEnumerable<CalendarEvent>> GetUpcomingEventsAsync();
}