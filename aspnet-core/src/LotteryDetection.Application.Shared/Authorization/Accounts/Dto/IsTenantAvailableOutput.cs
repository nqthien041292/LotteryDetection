namespace LotteryDetection.Authorization.Accounts.Dto;

public class IsTenantAvailableOutput
{
    public IsTenantAvailableOutput()
    {
    }

    public IsTenantAvailableOutput(TenantAvailabilityState state, int? tenantId = null)
    {
        State = state;
        TenantId = tenantId;
    }

    public IsTenantAvailableOutput(TenantAvailabilityState state, int? tenantId, string serverRootAddress)
    {
        State = state;
        TenantId = tenantId;
        ServerRootAddress = serverRootAddress;
    }

    public TenantAvailabilityState State { get; set; }

    public int? TenantId { get; set; }

    public string ServerRootAddress { get; set; }
}