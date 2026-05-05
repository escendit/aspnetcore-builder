// Licensed to the Escendit GmbH under one or more agreements.
// The Escendit GmbH licenses this file to you under the Apache License 2.0.

namespace Microsoft.AspNetCore.Builder;

using System.Text.Json;
using Escendit.AspNetCore.Builder;
using NATS.Client.Core;
using Orleans.Configuration;
using Orleans.Streaming.NATS.Hosting;

/// <summary>
/// Provides extension methods for configuring and enhancing the functionality
/// of <see cref="IClientBuilder"/> instances in Orleans-based applications.
/// </summary>
public static class ClientBuilderExtensions
{
    /// <summary>
    /// Provides extension methods for configuring client runtime behavior
    /// in Orleans-based applications using a <see cref="WebApplicationBuilder"/>.
    /// </summary>
    extension(WebApplicationBuilder builder)
    {
        /// <summary>
        /// Configures the Orleans client runtime for the application by applying
        /// default clustering settings and enabling activity propagation.
        /// </summary>
        /// <returns>
        /// The updated <see cref="WebApplicationBuilder"/> instance to support
        /// fluent configuration chaining.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the <see cref="WebApplicationBuilder"/> instance is null.
        /// </exception>
        public WebApplicationBuilder AddOrleansClientRuntime()
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder
                .AddClientHostingDefaults()
                .AddClientClusteringDefaults()
                .AddClientActivityPropagation();
            return builder;
        }

        /// <summary>
        /// Configures ADO.NET client clustering for Orleans in the host application builder.
        /// </summary>
        /// <param name="connectionStringName">
        /// The name of the connection string in the configuration to use for ADO.NET clustering. Defaults to "Clustering:AdoNet".
        /// </param>
        /// <param name="connectionStringInvariant">
        /// The invariant name of the database provider to use for ADO.NET clustering. Defaults to "Npgsql".
        /// </param>
        /// <returns>
        /// The modified <see cref="HostApplicationBuilder"/> instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the <paramref name="connectionStringName"/> or <paramref name="connectionStringInvariant"/> is null.
        /// </exception>
        public WebApplicationBuilder AddAdoNetClientClustering(
            string connectionStringName = ClusterConnectionAdoNetStringName,
            string connectionStringInvariant = ClusterInvariantName)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(connectionStringName);
            ArgumentNullException.ThrowIfNull(connectionStringInvariant);

            builder
                .UseOrleansClient(clientBuilder => clientBuilder
                    .UseAdoNetClustering(configureOptions =>
                    {
                        configureOptions.ConnectionString = builder.GetConnectionStringValue(connectionStringName);
                        configureOptions.Invariant = connectionStringInvariant;
                    }));
            return builder;
        }

        /// <summary>
        /// Configures the Orleans client runtime to use Redis clustering by connecting
        /// to a Redis instance specified by the connection string name.
        /// </summary>
        /// <param name="connectionStringName">
        /// The name of the connection string in the application's configuration
        /// that specifies the Redis instance for clustering. Defaults to "Clustering:Redis".
        /// </param>
        /// <returns>
        /// The updated <see cref="HostApplicationBuilder"/> instance to enable fluent
        /// configuration chaining.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the <see cref="HostApplicationBuilder"/> instance is null
        /// or when the specified connection string name is null.
        /// </exception>
        public WebApplicationBuilder AddRedisClientClustering(
            string connectionStringName = ClusterConnectionRedisStringName)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(connectionStringName);

            builder
                .UseOrleansClient(clientBuilder => clientBuilder
                    .UseRedisClustering(builder.GetConnectionStringValue(connectionStringName)));
            return builder;
        }

        /// <summary>
        /// Configures default hosting settings for the Orleans client runtime,
        /// including services and connection retry behavior, using the provided
        /// <see cref="WebApplicationBuilder"/> instance.
        /// </summary>
        /// <returns>
        /// The updated <see cref="WebApplicationBuilder"/> instance allowing for
        /// further configuration chaining.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the <see cref="WebApplicationBuilder"/> instance is null.
        /// </exception>
        public WebApplicationBuilder AddClientHostingDefaults()
        {
            builder
                .UseOrleansClient(clientBuilder =>
                {
                    clientBuilder
                        .ConfigureServices(services => services
                            .AddSingleton<ClusterConnectionStatusProvider>()
                            .AddSingleton<ConnectionToClusterLostHandler>(sp =>
                            {
                                var statusProvider = sp.GetRequiredService<ClusterConnectionStatusProvider>();
                                return statusProvider.OnClusterConnectionLost;
                            })
                            .AddSingleton<GatewayCountChangedHandler>(sp =>
                            {
                                var statusProvider = sp.GetRequiredService<ClusterConnectionStatusProvider>();
                                return statusProvider.OnGatewayCountChanged;
                            }))
                        .UseConnectionRetryFilter<ConnectionRetryFilter>()
                        .Services
                        .Configure<ClusterOptions>(options => clientBuilder
                            .Configuration
                            .Bind(options));
                });

            return builder;
        }

        /// <summary>
        /// Configures the Orleans client runtime to use NATS streams for streaming in the application.
        /// </summary>
        /// <param name="name">
        /// The logical name for the NATS stream. Defaults to a predefined stream name if not specified.
        /// </param>
        /// <param name="connectionStringName">
        /// The name of the connection string in the application configuration that specifies the NATS server connection details.
        /// </param>
        /// <returns>
        /// The updated <see cref="WebApplicationBuilder"/> instance to support fluent configuration chaining.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the <see cref="WebApplicationBuilder"/> instance, the <paramref name="name"/>, or the <paramref name="connectionStringName"/> is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the connection string associated with <paramref name="connectionStringName"/> is empty, null, or in an invalid format.
        /// </exception>
        public WebApplicationBuilder AddNatsClientStreams(
            string name = DefaultStreamName,
            string connectionStringName = StreamingConnectionNatsStringName)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(connectionStringName);
            builder
                .UseOrleansClient(clientBuilder =>
                {
                    var connectionString = clientBuilder
                        .Configuration
                        .GetConnectionString(connectionStringName);

                    try
                    {
                        if (string.IsNullOrEmpty(connectionString))
                        {
                            throw new InvalidOperationException(
                                $"The connection string with name '{connectionStringName}' is not configured.");
                        }

                        var uri = new Uri(connectionString);
                        clientBuilder
                            .AddNatsStreams(name, options =>
                            {
                                options.StreamName = name;
                                options.JsonSerializerOptions =
                                    new JsonSerializerOptions(JsonSerializerOptions.Default);
                                options.NatsClientOptions = NatsOpts.Default with
                                {
                                    Url = uri.OriginalString,
                                };
                            });
                    }
                    catch (UriFormatException)
                    {
                        throw new InvalidOperationException(
                            $"The connection string with name '{connectionStringName}' is in invalid format.");
                    }
                });
            return builder;
        }

        private WebApplicationBuilder AddClientClusteringDefaults()
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder
                .UseOrleansClient(clientBuilder => clientBuilder
                    .ConfigureServices(services => services
                        .Configure<ClusterOptions>(configureOptions => builder
                            .Configuration
                            .Bind(configureOptions))));

            return builder;
        }

        private WebApplicationBuilder AddClientActivityPropagation()
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder
                .UseOrleansClient(clientBuilder => clientBuilder
                    .AddActivityPropagation());

            return builder;
        }
    }

    private const string ClusterInvariantName = "Npgsql";
    private const string ClusterConnectionAdoNetStringName = "Clustering:AdoNet";
    private const string ClusterConnectionRedisStringName = "Clustering:Redis";
    private const string DefaultStreamName = "Platform";
    private const string StreamingConnectionNatsStringName = "Streaming:NATS";
}
