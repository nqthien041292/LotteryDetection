using System.Collections.Generic;
using System.Threading.Tasks;
using Abp;
using LotteryDetection.Dto;

namespace LotteryDetection.Gdpr;

public interface IUserCollectedDataProvider
{
    Task<List<FileDto>> GetFiles(UserIdentifier user);
}
