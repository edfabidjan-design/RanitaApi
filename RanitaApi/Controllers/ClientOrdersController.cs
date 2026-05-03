using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RanitaApi.Data;

namespace RanitaApi.Controllers
{
    [ApiController]
    [Route("api/client-orders")]
    public class ClientOrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ClientOrdersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("{clientId}")]
        public async Task<IActionResult> GetClientOrders(int clientId)
        {
            var orders = await _context.Orders
                .Where(o => o.ClientId == clientId)
                .OrderByDescending(o => o.Id)
                .Select(o => new
                {
                    o.Id,
                    o.CustomerName,
                    o.CustomerPhone,
                    o.CustomerAddress,
                    o.PaymentMethod,
                    Total = o.Items.Sum(i => i.Quantity * i.Price), // ✔ correction
                    o.Status,
                    o.CreatedAt,
                    Items = o.Items.Select(i => new
                    {
                        i.ProductId,
                        ProductName = i.ProductName, // ✔ correction
                        i.Quantity,
                        Price = i.Price, // ✔ correction
                        Total = i.Quantity * i.Price
                    })
                })
                .ToListAsync();

            return Ok(orders);
        }
    }
}