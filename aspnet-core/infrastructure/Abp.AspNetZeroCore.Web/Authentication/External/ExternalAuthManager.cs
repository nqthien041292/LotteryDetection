using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;

namespace Abp.AspNetZeroCore.Web.Authentication.External;

public class ExternalAuthManager : IExternalAuthManager, ITransientDependency
{
    private readonly IExternalAuthConfiguration _externalAuthConfiguration;
    private readonly IIocResolver _iocResolver;

    public ExternalAuthManager(
        IIocResolver iocResolver,
        IExternalAuthConfiguration externalAuthConfiguration)
    {
        _iocResolver = iocResolver;
        _externalAuthConfiguration = externalAuthConfiguration;
    }

    public Task<bool> IsValidUser(
        string provider,
        string providerKey,
        string providerAccessCode)
    {
        using var providerApi = CreateProviderApi(provider);
        return providerApi.Object.IsValidUser(providerKey, providerAccessCode);
    }

    public Task<ExternalAuthUserInfo> GetUserInfo(
        string provider,
        string accessCode)
    {
        using var providerApi = CreateProviderApi(provider);
        return providerApi.Object.GetUserInfo(accessCode);
    }

    public IDisposableDependencyObjectWrapper<IExternalAuthProviderApi> CreateProviderApi(
        string provider)
    {
        var providerInfo =
            _externalAuthConfiguration.ExternalLoginInfoProviders.All(infoProvider => infoProvider.Name != provider)
#pragma warning disable CS0618 // Type or member is obsolete
                ? _externalAuthConfiguration.Providers.FirstOrDefault(p => p.Name == provider)
#pragma warning restore CS0618 // Type or member is obsolete
                : _externalAuthConfiguration.ExternalLoginInfoProviders
                    .Single(infoProvider => infoProvider.Name == provider).GetExternalLoginInfo();
        if (providerInfo == null)
            throw new Exception("Unknown external auth provider: " + provider);
        var dependencyObjectWrapper =
            _iocResolver.ResolveAsDisposable<IExternalAuthProviderApi>(providerInfo.ProviderApiType);
        dependencyObjectWrapper.Object.Initialize(providerInfo);
        return dependencyObjectWrapper;
    }
}