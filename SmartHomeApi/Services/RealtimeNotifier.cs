using Microsoft.AspNetCore.SignalR;
using SmartHomeApi.Hubs;

namespace SmartHomeApi.Services;

/// <inheritdoc cref="IRealtimeNotifier"/>
public class RealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<SmartHomeHub> _hub;

    public RealtimeNotifier(IHubContext<SmartHomeHub> hub)
    {
        _hub = hub;
    }

    public Task NotifyHouseholdAsync(int householdId, string eventName, object payload) =>
        _hub.Clients
            .Group(SmartHomeHub.GroupName(householdId))
            .SendAsync(eventName, payload);
}
