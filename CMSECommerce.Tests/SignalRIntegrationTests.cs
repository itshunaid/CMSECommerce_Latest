using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using CMSECommerce;
using Microsoft.Extensions.DependencyInjection;
using CMSECommerce.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore;

namespace CMSECommerce.Tests
{
 public class SignalRIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
 {
 private readonly WebApplicationFactory<Program> _factory;
 public SignalRIntegrationTests(WebApplicationFactory<Program> factory)
 {
 _factory = factory.WithWebHostBuilder(builder =>
 {
 builder.ConfigureServices(services =>
 {
 // Replace DB context with in-memory for tests
 services.AddDbContext<DataContext>(opts => opts.UseInMemoryDatabase("TestDb_SignalR"));
 });
 });
 }

 [Fact]
 public async Task HubConnectUpdatesUserStatus()
 {
 var client = _factory.CreateDefaultClient();
 var serverUri = new Uri(_factory.Server.BaseAddress, "/chatHub");

 // Create HubConnection to TestServer
 var connection = new HubConnectionBuilder()
 .WithUrl(serverUri.ToString(), options =>
 {
 options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
 })
 .WithAutomaticReconnect()
 .Build();

 await connection.StartAsync();
 // Wait briefly for OnConnectedAsync to run in hub
 await Task.Delay(500);
 await connection.StopAsync();

 // Check DB status was created/updated
 using var scope = _factory.Services.CreateScope();
 var db = scope.ServiceProvider.GetRequiredService<DataContext>();
 var any = await db.UserStatuses.AnyAsync();
 Assert.True(any);
 }
 }
}
