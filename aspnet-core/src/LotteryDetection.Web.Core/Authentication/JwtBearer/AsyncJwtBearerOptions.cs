using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace LotteryDetection.Web.Authentication.JwtBearer;

public class AsyncJwtBearerOptions : JwtBearerOptions
{
    public readonly List<IAsyncSecurityTokenValidator> AsyncSecurityTokenValidators;

    private readonly LotteryDetectionAsyncJwtSecurityTokenHandler _defaultAsyncHandler = new LotteryDetectionAsyncJwtSecurityTokenHandler();

    public AsyncJwtBearerOptions()
    {
        AsyncSecurityTokenValidators = new List<IAsyncSecurityTokenValidator>() { _defaultAsyncHandler };
    }
}


