using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FamilyOs.Api.Realtime;

[Authorize]
public sealed class NotificationsHub : Hub
{
    // Server pushes these to clients:
    // notificationCreated(NotificationDto)
    // reminderFired(ReminderDto)
    // aiSuggestionReady(SuggestionSummaryDto)
}
