// Licensed to the Escendit GmbH under one or more agreements.
// The Escendit GmbH licenses this file to you under the Apache License 2.0.

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Provides extension methods for shared functionality within the application.
/// </summary>
internal static class SharedExtensions
{
    extension(WebApplicationBuilder builder)
    {
        internal string GetConnectionStringValue(string connectionStringName)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(connectionStringName);

            var configurationSection = builder
                .Configuration
                .GetRequiredSection("ConnectionStrings")
                .GetSection(connectionStringName);

            if (!configurationSection.Exists() || string.IsNullOrWhiteSpace(configurationSection.Value))
            {
                throw new InvalidOperationException($"Connection string with name {connectionStringName} is empty or invalid in configuration.");
            }

            return configurationSection.Get<string>()!;
        }
    }
}
