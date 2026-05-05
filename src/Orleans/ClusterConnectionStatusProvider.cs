// Licensed to the Escendit GmbH under one or more agreements.
// The Escendit GmbH licenses this file to you under the Apache License 2.0.

namespace Escendit.AspNetCore.Builder;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Provides functionality to monitor and handle the connection status of a cluster.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated via DI")]
internal sealed class ClusterConnectionStatusProvider
{
    /// <summary>
    /// Gets a value indicating whether indicates whether the cluster is currently connected.
    /// </summary>
    /// <remarks>
    /// This property reflects the connection status of the cluster.
    /// It is set to <c>true</c> when there is at least one connected gateway,
    /// and <c>false</c> otherwise. The value is updated based on events
    /// such as cluster connection loss or changes in the number of connected gateways.
    /// </remarks>
    /// <value>
    /// A value indicating whether indicates whether the cluster is currently connected.
    /// </value>
    public bool IsConnected { get; private set; }

    /// <summary>
    /// Handles the event triggered when the cluster connection is lost.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The event arguments associated with the cluster disconnection.</param>
    public void OnClusterConnectionLost(object sender, EventArgs e)
        => IsConnected = false;

    /// <summary>
    /// Handles the event triggered when the number of connected gateways changes.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="eventArgs">The event arguments containing the updated gateway connection details.</param>
    /// <exception cref="ArgumentNullException">Thrown when the event arguments are null.</exception>
    public void OnGatewayCountChanged(object sender, GatewayCountChangedEventArgs eventArgs)
    {
        ArgumentNullException.ThrowIfNull(eventArgs);
        IsConnected = eventArgs.NumberOfConnectedGateways > 0;
    }
}
