using Abp.Application.Services.Dto;

namespace LotteryDetection.Authorization.Users.Dto;

public interface IGetLoginAttemptsInput : ISortedResultRequest
{
    string Filter { get; set; }
}

