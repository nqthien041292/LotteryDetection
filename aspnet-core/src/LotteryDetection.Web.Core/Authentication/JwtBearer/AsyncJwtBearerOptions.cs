using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace LotteryDetection.Web.Authentication.JwtBearer;

public class AsyncJwtBearerOptions : JwtBearerOptions
{
    private readonly LotteryDetectionAsyncJwtSecurityTokenHandler _defaultAsyncHandler = new();
    public readonly List<IAsyncSecurityTokenValidator> AsyncSecurityTokenValidators;

    public AsyncJwtBearerOptions()
    {
        AsyncSecurityTokenValidators = new List<IAsyncSecurityTokenValidator> { _defaultAsyncHandler };
    }
}