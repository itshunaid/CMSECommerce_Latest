using CMSECommerce.Areas.Admin.Controllers;
using CMSECommerce.Infrastructure;
using CMSECommerce.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;
using System.Threading.Tasks;
using System.Linq;

namespace CMSECommerce.Tests.Unit
{
    public class CategoriesControllerTests
    {
        private DataContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;
            return new DataContext(options);
        }

        [Fact]
        public async Task WouldCreateCycle_Prevents_Cycle()
        {
            var context = CreateContext();
            // seed cat A -> B -> C
            var a = new Category { Id = 1, Name = "A", Slug = "a", Level = 0 };
            var b = new Category { Id = 2, Name = "B", Slug = "b", ParentId = 1, Level = 1 };
            var c = new Category { Id = 3, Name = "C", Slug = "c", ParentId = 2, Level = 2 };
            context.Categories.AddRange(a, b, c);
            await context.SaveChangesAsync();

            var logger = new LoggerFactory().CreateLogger<CategoriesController>();
            var controller = new CategoriesController(context, logger);

            // Attempt to set A's parent to C -> should detect cycle
            var result = await controller.TestWouldCreateCycle(1, 3);
            Assert.True(result);
        }

        [Fact]
        public async Task WouldCreateCycle_Allows_NonCycle()
        {
            var context = CreateContext();
            var a = new Category { Id = 1, Name = "A", Slug = "a", Level = 0 };
            var b = new Category { Id = 2, Name = "B", Slug = "b", Level = 0 };
            context.Categories.AddRange(a, b);
            await context.SaveChangesAsync();

            var logger = new LoggerFactory().CreateLogger<CategoriesController>();
            var controller = new CategoriesController(context, logger);

            var result = await controller.TestWouldCreateCycle(1, 2);
            Assert.False(result);
        }
    }
}
