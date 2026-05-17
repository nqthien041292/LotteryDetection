/*
 *  Copied from https://github.com/dotnet/aspnetcore/blob/main/src/Security/Authentication/JwtBearer/src/JwtBearerExtensions.cs
 *  Updated to implement async token validation
 */

#nullable enable
using System;
using LotteryDetection.Web.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

/// <summary>
///     Extension methods to configure JWT bearer authentication.
/// </summary>
public static class LotteryDetectionJwtBearerExtensions
{
    /// <summary>
    ///     Enables JWT-bearer authentication using the default scheme <see cref="JwtBearerDefaults.AuthenticationScheme" />.
    ///     <para>
    ///         JWT bearer authentication performs authentication by extracting and validating a JWT token from the
    ///         <c>Authorization</c> request header.
    ///     </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder" />.</param>
    /// <returns>A reference to <paramref name="builder" /> after the operation has completed.</returns>
    public static AuthenticationBuilder AddAbpAsyncJwtBearer(this AuthenticationBuilder builder)
    {
        return builder.AddAbpAsyncJwtBearer(JwtBearerDefaults.AuthenticationScheme, _ => { });
    }

    /// <summary>
    ///     Enables JWT-bearer authentication using the default scheme <see cref="JwtBearerDefaults.AuthenticationScheme" />.
    ///     <para>
    ///         JWT bearer authentication performs authentication by extracting and validating a JWT token from the
    ///         <c>Authorization</c> request header.
    ///     </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder" />.</param>
    /// <param name="configureOptions">A delegate that allows configuring <see cref="JwtBearerOptions" />.</param>
    /// <returns>A reference to <paramref name="builder" /> after the operation has completed.</returns>
    public static AuthenticationBuilder AddAbpAsyncJwtBearer(this AuthenticationBuilder builder,
        Action<AsyncJwtBearerOptions> configureOptions)
    {
        return builder.AddAbpAsyncJwtBearer(JwtBearerDefaults.AuthenticationScheme, configureOptions);
    }

    /// <summary>
    ///     Enables JWT-bearer authentication using the specified scheme.
    ///     <para>
    ///         JWT bearer authentication performs authentication by extracting and validating a JWT token from the
    ///         <c>Authorization</c> request header.
    ///     </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder" />.</param>
    /// <param name="authenticationScheme">The authentication scheme.</param>
    /// <param name="configureOptions">A delegate that allows configuring <see cref="JwtBearerOptions" />.</param>
    /// <returns>A reference to <paramref name="builder" /> after the operation has completed.</returns>
    public static AuthenticationBuilder AddAbpAsyncJwtBearer(this AuthenticationBuilder builder,
        string authenticationScheme, Action<AsyncJwtBearerOptions> configureOptions)
    {
        return builder.AddAbpAsyncJwtBearer(authenticationScheme, null,
            configureOptions);
    }

    /// <summary>
    ///     Enables JWT-bearer authentication using the specified scheme.
    ///     <para>
    ///         JWT bearer authentication performs authentication by extracting and validating a JWT token from the
    ///         <c>Authorization</c> request header.
    ///     </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder" />.</param>
    /// <param name="authenticationScheme">The authentication scheme.</param>
    /// <param name="displayName">The display name for the authentication handler.</param>
    /// <param name="configureOptions">A delegate that allows configuring <see cref="JwtBearerOptions" />.</param>
    /// <returns>A reference to <paramref name="builder" /> after the operation has completed.</returns>
    public static AuthenticationBuilder AddAbpAsyncJwtBearer(this AuthenticationBuilder builder,
        string authenticationScheme, string? displayName, Action<AsyncJwtBearerOptions> configureOptions)
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor
            .Singleton<IPostConfigureOptions<AsyncJwtBearerOptions>, JwtBearerPostConfigureOptions>());
        return builder.AddScheme<AsyncJwtBearerOptions, LotteryDetectionAsyncJwtBearerHandler>(authenticationScheme,
            displayName, configureOptions);
    }
}