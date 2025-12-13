using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using CMSECommerce.Infrastructure;
using CMSECommerce.Controllers;
using CMSECommerce.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Moq;
using System.Collections.Generic;
using CMSECommerce.Models.ViewModels;
using System.Linq;

namespace CMSECommerce.Tests
{
 public class ProductsControllerTests
 {
 [Fact]
 public async Task Index_HidesCurrentUserFromUserList()
 {
 var options = new DbContextOptionsBuilder<DataContext>()
 .UseInMemoryDatabase(databaseName: "TestDb_Products_Index")
 .Options;

 await using var context = new DataContext(options);

 // Create two users
 var user1 = new IdentityUser { UserName = "user1", Email = "u1@example.com" };
 var user2 = new IdentityUser { UserName = "user2", Email = "u2@example.com" };

 var store = new Mock<IUserStore<IdentityUser>>();
 var userManager = new UserManager<IdentityUser>(store.Object, null, null, null, null, null, null, null, null);

 // For simplicity in this unit test, create a minimal IUserStatusService
 var mockStatus = new Mock<IUserStatusService>();
 mockStatus.Setup(s => s.GetAllOtherUsersStatusAsync(It.IsAny<string>()))
 .ReturnsAsync(new List<CMSECommerce.DTOs.UserStatusDto> { new CMSECommerce.DTOs.UserStatusDto { User = user2, IsOnline = true } });

 var envMock = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
 envMock.Setup(e => e.WebRootPath).Returns("wwwroot");

 // Create controller with mocked dependencies
 var controller = new ProductsController(context, envMock.Object, userManager, mockStatus.Object);

 // Simulate an authenticated user by setting HttpContext
 var httpContext = new DefaultHttpContext();
 var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "user1"), new Claim(ClaimTypes.Name, "user1") };
 httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
 controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

 // Add a product to avoid exceptions
 context.Products.Add(new CMSECommerce.Models.Product { Name = "P1", Slug = "p1", Price =1.0M, Status = CMSECommerce.Models.ProductStatus.Approved });
 await context.SaveChangesAsync();

 var result = await controller.Index();
 var vm = (result as ViewResult).Model as ProductListViewModel;

 // Expect the returned user list to not include the current user (user1)
 Assert.DoesNotContain(vm.AllUsers, u => u.User.Id == "user1");
 Assert.Contains(vm.AllUsers, u => u.User.UserName == "user2");
 }
 }
}
