using Abp.Events.Bus;

namespace LotteryDetection.MultiTenancy.Subscription;

public class RecurringPaymentsDisabledEventData : EventData
{
    public RecurringPaymentsDisabledEventData(int tenantId, int daysUntilDue)
    {
        TenantId = tenantId;
        DaysUntilDue = daysUntilDue;
    }

    public int TenantId { get; set; }

    public int DaysUntilDue { get; set; }
}