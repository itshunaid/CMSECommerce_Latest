using CMSECommerce.Controllers;
using CMSECommerce.Infrastructure;
using CMSECommerce.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Xunit;

namespace CMSECommerce.Tests
{
    public class UnlockRequestTests : IDisposable
    {
        private readonly DataContext _context;

        public UnlockRequestTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new DataContext(options);
        }

        [Fact]
        public async Task UnlockRequestModel_CanBeCreated()
        {
            // Arrange
            var unlockRequest = new UnlockRequest
            {
                UserId = "test-user-id",
                UserName = "testuser",
                Email = "test@example.com",
                Status = "Pending",
                RequestDate = DateTime.UtcNow
            };

            // Act
            _context.UnlockRequests.Add(unlockRequest);
            await _context.SaveChangesAsync();

            // Assert
            var savedRequest = await _context.UnlockRequests.FindAsync(unlockRequest.Id);
            Assert.NotNull(savedRequest);
            Assert.Equal("testuser", savedRequest.UserName);
            Assert.Equal("test@example.com", savedRequest.Email);
            Assert.Equal("Pending", savedRequest.Status);
        }

        [Fact]
        public async Task UnlockRequestModel_CanBeQueried()
        {
            // Arrange
            var unlockRequest1 = new UnlockRequest
            {
                UserId = "user1",
                UserName = "user1",
                Email = "user1@example.com",
                Status = "Pending",
                RequestDate = DateTime.UtcNow
            };

            var unlockRequest2 = new UnlockRequest
            {
                UserId = "user2",
                UserName = "user2",
                Email = "user2@example.com",
                Status = "Approved",
                RequestDate = DateTime.UtcNow
            };

            _context.UnlockRequests.AddRange(unlockRequest1, unlockRequest2);
            await _context.SaveChangesAsync();

            // Act
            var pendingRequests = await _context.UnlockRequests
                .Where(r => r.Status == "Pending")
                .ToListAsync();

            var allRequests = await _context.UnlockRequests
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            // Assert
            Assert.Single(pendingRequests);
            Assert.Equal(2, allRequests.Count);
            Assert.Equal("user2", allRequests.First().UserName); // Should be ordered by date descending
        }

        [Fact]
        public async Task UnlockRequestModel_StatusCanBeUpdated()
        {
            // Arrange
            var unlockRequest = new UnlockRequest
            {
                UserId = "test-user-id",
                UserName = "testuser",
                Email = "test@example.com",
                Status = "Pending",
                RequestDate = DateTime.UtcNow
            };

            _context.UnlockRequests.Add(unlockRequest);
            await _context.SaveChangesAsync();

            // Act
            unlockRequest.Status = "Approved";
            unlockRequest.AdminNotes = "Approved by admin";
            await _context.SaveChangesAsync();

            // Assert
            var updatedRequest = await _context.UnlockRequests.FindAsync(unlockRequest.Id);
            Assert.Equal("Approved", updatedRequest.Status);
            Assert.Equal("Approved by admin", updatedRequest.AdminNotes);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
