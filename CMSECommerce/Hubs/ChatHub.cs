using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace CMSECommerce.Hubs
{
 [Authorize]
 public class ChatHub : Hub
 {
 public override Task OnConnectedAsync()
 {
 // Optionally: log or perform actions when a user connects
 return base.OnConnectedAsync();
 }

 public override Task OnDisconnectedAsync(Exception exception)
 {
 // Optionally: log or perform actions when a user disconnects
 return base.OnDisconnectedAsync(exception);
 }

 // Broadcast to everyone (one-to-all)
 public Task SendMessageToAll(string message)
 {
 var sender = Context.User?.Identity?.Name ?? "Unknown";
 return Clients.All.SendAsync("ReceiveMessage", sender, message);
 }

 // Send a private message to a specific user (by user id)
 public Task SendPrivateMessage(string recipientUserId, string message)
 {
 var sender = Context.User?.Identity?.Name ?? "Unknown";
 return Clients.User(recipientUserId).SendAsync("ReceiveMessage", sender, message);
 }

 // Join a named group
 public async Task JoinGroup(string groupName)
 {
 await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
 var sender = Context.User?.Identity?.Name ?? "System";
 await Clients.Group(groupName).SendAsync("ReceiveMessage", "System", $"{sender} has joined group '{groupName}'.");
 }

 // Leave a named group
 public async Task LeaveGroup(string groupName)
 {
 await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
 var sender = Context.User?.Identity?.Name ?? "System";
 await Clients.Group(groupName).SendAsync("ReceiveMessage", "System", $"{sender} has left group '{groupName}'.");
 }

 // Send message to a specific group
 public Task SendMessageToGroup(string groupName, string message)
 {
 var sender = Context.User?.Identity?.Name ?? "Unknown";
 return Clients.Group(groupName).SendAsync("ReceiveMessage", sender, message);
 }
 }
}
