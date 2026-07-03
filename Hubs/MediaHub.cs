using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace ADS2026.Hubs
{
    public class MediaHub : Hub
    {
        // Tracks which screen name each connection represents, so we know
        // who to mark "offline" when that connection drops.
        private static readonly ConcurrentDictionary<string, string> ConnectionScreens = new();

        public async Task JoinScreen(string screenName)
        {
            if (string.IsNullOrWhiteSpace(screenName)) return;
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(screenName));
            ConnectionScreens[Context.ConnectionId] = screenName;
        }

        public async Task LeaveScreen(string screenName)
        {
            if (string.IsNullOrWhiteSpace(screenName)) return;
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(screenName));
            ConnectionScreens.TryRemove(Context.ConnectionId, out _);
            await Clients.All.SendAsync("ScreenOffline", screenName);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Fires when a tab is closed, kiosk browser is killed, or the connection drops
            if (ConnectionScreens.TryRemove(Context.ConnectionId, out var screenName))
            {
                await Clients.All.SendAsync("ScreenOffline", screenName);
            }
            await base.OnDisconnectedAsync(exception);
        }
        public static string GroupName(string screenName) => $"screen:{screenName}";
    }
}