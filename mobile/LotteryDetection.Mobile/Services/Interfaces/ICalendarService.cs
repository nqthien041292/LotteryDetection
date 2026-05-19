using LotteryDetection.Mobile.Models.Family;

namespace LotteryDetection.Mobile.Services.Interfaces;

public interface ICalendarService
{
    Task<IEnumerable<CalendarEvent>> GetUpcomingEventsAsync();
}