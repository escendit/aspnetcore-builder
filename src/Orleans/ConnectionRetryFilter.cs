// Licensed to the Escendit GmbH under one or more agreements.
// The Escendit GmbH licenses this file to you under the Apache License 2.0.

namespace Escendit.AspNetCore.Builder;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Defines a filter that determines whether a connection retry should be attempted
/// in the event of a failed connection attempt.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated via DI")]
internal sealed class ConnectionRetryFilter : IClientConnectionRetryFilter
{
    private const int MaxRetry = 5;
    private const int Delay = 1_500;
    private int _retryCount;

    /// <inheritdoc/>
    public async Task<bool> ShouldRetryConnectionAttempt(Exception exception, CancellationToken cancellationToken)
    {
        if (_retryCount >= MaxRetry)
        {
            return false;
        }

        if (cancellationToken.IsCancellationRequested ||
            exception is not SiloUnavailableException)
        {
            return false;
        }

        await Task.Delay(++_retryCount * Delay, cancellationToken).ConfigureAwait(false);
        return true;
    }
}
