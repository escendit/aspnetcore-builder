// Licensed to the Escendit GmbH under one or more agreements.
// The Escendit GmbH licenses this file to you under the Apache License 2.0.

namespace Microsoft.AspNetCore.Builder;

using System.Net;
using Diagnostics;
using Mvc;

/// <summary>
/// Contains extension methods for configuring and extending a web application.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Provides extension methods for configuring gateway API functionality within a web application.
    /// </summary>
    extension<TApp>(TApp app)
    where TApp : IApplicationBuilder
    {
        /// <summary>
        /// Configures the web application to use a gateway API.
        /// </summary>
        /// <param name="mountPoint">
        /// The optional base path to mount the gateway API. If not provided, the method checks the configuration
        /// for the "DOTNET_GATEWAY_MOUNTPOINT" section to determine the base path.
        /// </param>
        /// <returns>
        /// The configured <see cref="WebApplication"/> instance for chaining further setup calls.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the <see cref="WebApplication"/> instance is null.
        /// </exception>
        public TApp MapGatewayApi(PathString? mountPoint = null)
        {
            ArgumentNullException.ThrowIfNull(app);

            if (mountPoint.HasValue)
            {
                app.UsePathBase(mountPoint.Value);
                return app;
            }

            var configuration = app.ApplicationServices.GetService<IConfiguration>();
            var section = configuration?.GetSection("DOTNET_GATEWAY_MOUNTPOINT");

            if ((section is null && !section.Exists()) || string.IsNullOrEmpty(section.Value))
            {
                return app;
            }

            app.UsePathBase(section.Value);

            return app;
        }

        /// <summary>
        /// Configures exception handling middleware for the web application to handle errors and provide detailed responses
        /// in compliance with HTTP problem details specifications.
        /// </summary>
        /// <returns>
        /// The configured <see cref="WebApplication"/> instance for chaining further setup calls.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the <see cref="WebApplication"/> instance is null.
        /// </exception>
        public TApp UseExceptionHandling()
        {
            ArgumentNullException.ThrowIfNull(app);
            app.UseExceptionHandler(
                new ExceptionHandlerOptions
                {
                    ExceptionHandler = ExceptionAsyncHandler,
                    StatusCodeSelector = ExceptionStatusCodeSelector,
                });
            return app;
        }
    }

    private static async Task ExceptionAsyncHandler(HttpContext context)
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>();
        var environment = context.RequestServices.GetRequiredService<IWebHostEnvironment>();

        switch (exception?.Error)
        {
            case HttpRequestException httpRequestException:
                {
                    var request = context.Request;
                    var instance = $"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}";
                    await context
                        .Response
                        .WriteAsJsonAsync(
                            new ProblemDetails
                            {
                                Status = (int?)httpRequestException.StatusCode,
                                Detail = environment.IsProduction() ? null : httpRequestException.StackTrace,
                                Type = $"https://httpstatuses.io/{httpRequestException.StatusCode!}",
                                Instance = instance,
                                Title = httpRequestException.Message,
                            },
                            cancellationToken: context.RequestAborted)
                        .ConfigureAwait(false);
                    break;
                }

            case { } genericException:
                {
                    var request = context.Request;
                    var instance = $"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}";
                    await context
                        .Response
                        .WriteAsJsonAsync(
                            new ProblemDetails
                            {
                                Status = (int)HttpStatusCode.InternalServerError,
                                Detail = environment.IsProduction() ? null : genericException.StackTrace,
                                Type = $"https://httpstatuses.io/{(int)HttpStatusCode.InternalServerError}",
                                Instance = instance,
                                Title = genericException.Message,
                            },
                            cancellationToken: context.RequestAborted)
                        .ConfigureAwait(false);
                    break;
                }
        }
    }

    private static int ExceptionStatusCodeSelector(Exception exception)
    {
        if (exception is HttpRequestException { StatusCode: not null } httpRequestException)
        {
            return (int)httpRequestException.StatusCode;
        }

        return (int)HttpStatusCode.InternalServerError;
    }
}
