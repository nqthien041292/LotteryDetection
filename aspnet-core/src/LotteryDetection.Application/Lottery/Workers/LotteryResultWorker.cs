using System;
using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.Threading.BackgroundWorkers;
using Abp.Threading.Timers;

namespace LotteryDetection.Lottery.Workers;

public class LotteryResultWorker : PeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public LotteryResultWorker(
        AbpTimer timer,
        IUnitOfWorkManager unitOfWorkManager)
        : base(timer)
    {
        _unitOfWorkManager = unitOfWorkManager;
        
        // Run every 30 minutes
        Timer.Period = 30 * 60 * 1000; 
    }

    [UnitOfWork]
    protected override void DoWork()
    {
        using (var scope = IocManager.Instance.CreateScope())
        {
            var ticketAnalysisAppService = scope.Resolve<ITicketAnalysisAppService>();
            
            Logger.Info("LotteryResultWorker is starting to check pending results...");
            
            // We use AsyncHelper to call async method from sync DoWork
            Abp.Threading.AsyncHelper.RunSync(() => ticketAnalysisAppService.CheckPendingResultsAsync());
            
            Logger.Info("LotteryResultWorker finished checking pending results.");
        }
    }
}
