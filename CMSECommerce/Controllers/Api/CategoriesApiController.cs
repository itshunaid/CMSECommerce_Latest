using CMSECommerce.Infrastructure;
using CMSECommerce.Models;
using CMSECommerce.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMSECommerce.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesApiController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IAuditService _auditService;

        public CategoriesApiController(DataContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        [HttpGet("tree")]
        public async Task<IActionResult> Tree()
        {
            var categories = await _context.Categories.AsNoTracking().ToListAsync();

            var tree = BuildTree(categories, null);

            return Ok(tree);
        }

        private List<TreeNode> BuildTree(List<Category> categories, int? parentId)
        {
            var nodes = categories.Where(c => c.ParentId == parentId).OrderBy(c => c.Name).ToList();

            return nodes.Select(c => new TreeNode
            {
                id = c.Id,
                text = c.Name,
                children = BuildTree(categories, c.Id)
            }).ToList();
        }

        private class TreeNode
        {
            public int id { get; set; }
            public string text { get; set; }
            public List<TreeNode> children { get; set; } = new List<TreeNode>();
        }
    }
}
