using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using CMSEcommerce.Infrastructure;
using CMSEcommerce.Controllers;
using CMSEcommerce.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Moq;
using System.Collections.Generic;
using CMSEcommerce.Models.ViewModels;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace CMSEcommerce.Tests
{
 public class ProductsControllerTests
 {
 [Fact]
 public async Task Index_HidesCurrentUserFromUserList_WithRealUserManager()
 {
 var services = new ServiceCollection();
 services.AddDbContext<DataContext>(opts => opts.UseInMemoryDatabase("TestDb_Products_Index2"));
 services.AddIdentity<IdentityUser, IdentityRole>().AddEntityFrameworkStores<DataContext>().AddDefaultTokenProviders();
 var provider = services.BuildServiceProvider();

 using var scope = provider.CreateScope();
 var context = scope.ServiceProvider.GetRequiredService<DataContext>();
 var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

 // create two users
 var u1 = new IdentityUser { UserName = "user1", Email = "u1@example.com" };
 var u2 = new IdentityUser { UserName = "user2", Email = "u2@example.com" };
 await userManager.CreateAsync(u1, "Password123!");
 await userManager.CreateAsync(u2, "Password123!");

 // Create service for status
 var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
 var statusService = new UserStatusService(context, userManager, config);

 var envMock = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
 envMock.Setup(e => e.WebRootPath).Returns("wwwroot");

 var controller = new ProductsController(context, envMock.Object, userManager, statusService);

 // Simulate authenticated user u1
 var httpContext = new DefaultHttpContext();
 var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, u1.Id), new Claim(ClaimTypes.Name, u1.UserName) };
 httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
 controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

 // Add a product to avoid exceptions
 context.Products.Add(new CMSEcommerce.Models.Product { Name = "P1", Slug = "p1", Price =1.0M, Status = CMSEcommerce.Models.ProductStatus.Approved });
 await context.SaveChangesAsync();

 var result = await controller.Index();
 var vm = (result as ViewResult).Model as ProductListViewModel;

 Assert.DoesNotContain(vm.AllUsers, u => u.User.Id == u1.Id);
 Assert.Contains(vm.AllUsers, u => u.User.UserName == "user2");
 }
 }
}
