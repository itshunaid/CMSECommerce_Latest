using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace CMSECommerce.Services
{
 public class NameUserIdProvider : IUserIdProvider
 {
 public string GetUserId(HubConnectionContext connection)
 {
 // Use NameIdentifier if present, otherwise fall back to Name
 var nameId = connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
 if (!string.IsNullOrEmpty(nameId)) return nameId;
 return connection.User?.Identity?.Name;
 }
 }
}
