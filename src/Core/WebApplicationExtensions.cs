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
    extension(WebApplication app)
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
        public WebApplication MapGatewayApi(PathString? mountPoint = null)
        {
            ArgumentNullException.ThrowIfNull(app);

            if (mountPoint.HasValue)
            {
                app.UsePathBase(mountPoint.Value);
                return app;
            }

            var section = app.Configuration.GetSection("DOTNET_GATEWAY_MOUNTPOINT");

            if (!section.Exists() && string.IsNullOrEmpty(section.Value))
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
        public WebApplication UseExceptionHandling()
        {
            ArgumentNullException.ThrowIfNull(app);
            app.UseExceptionHandler(
                new ExceptionHandlerOptions
                {
                    ExceptionHandler = async (context) =>
                    {
                        var exception = context.Features.Get<IExceptionHandlerFeature>();
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
                                                Detail = httpRequestException.StackTrace,
                                                Type = $"https://httpstatuses.io/{httpRequestException.StatusCode!}",
                                                Instance = instance,
                                                Title = httpRequestException.Message,
                                            })
                                        .ConfigureAwait(false);
                                    break;
                                }

                            case { } genericException:
                                {
                                    var request = context.Request;
                                    var instance = $"{request.Scheme}://{request.Host}{request.Path}";
                                    await context
                                        .Response
                                        .WriteAsJsonAsync(
                                            new ProblemDetails
                                            {
                                                Status = (int)HttpStatusCode.InternalServerError,
                                                Detail = genericException.StackTrace,
                                                Type = $"https://httpstatuses.io/500",
                                                Instance = instance,
                                                Title = genericException.Message,
                                            })
                                        .ConfigureAwait(false);
                                    break;
                                }
                        }
                    },
                });
            return app;
        }
    }
}
