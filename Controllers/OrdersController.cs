using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Webstore.Data;
using Webstore.Models;
using X.PagedList;

namespace Webstore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public OrdersController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        [Authorize]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Index(CancellationToken cancellationToken, string sortOrder = "", string searchString = "", int? page = 1 )
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.CurrentFilter = searchString;
            ViewBag.DateSortParm = String.IsNullOrEmpty(sortOrder) ? "date_desc" : "";

            var orders = from o in _context.Orders
                         .Include(o => o.OrderDetails)
                         .ThenInclude(od => od.Product)
                         .Where(o => o.Status == "Submitted" && o.UserId == userId)
                         select o;

            if (!String.IsNullOrEmpty(searchString))
            {
                if (int.TryParse(searchString, out int orderId))
                {
                    orders = orders.Where(o => o.Id == orderId);
                }
            }

            switch (sortOrder)
            {
                case "date_desc":
                    orders = orders.OrderByDescending(o => o.OrderDate);
                    break;
                case "date_asc":
                    orders = orders.OrderBy(o => o.OrderDate);
                    break;
                default:
                    orders = orders.OrderBy(o => o.OrderDate);
                    break;
            }

            int pageSize = 3;
            var ordersAsIPagedList = await orders.ToPagedListAsync(page ?? 1, pageSize);

            return View(ordersAsIPagedList);
        }






        // GET: OrdersCart
        [Authorize]
        [HttpGet("Cart")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> GetCart(CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                    .FirstOrDefaultAsync(o => o.Status == "Cart" && o.UserId == userId, cancellationToken);

                var viewModel = new CartViewModel
                {
                    Items = order?.OrderDetails.ToList() ?? new List<OrderDetail>(),
                    TotalPrice = order?.OrderDetails.Sum(od => od.Product.Price * od.Quantity) ?? 0
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                // Log the exception message
                Console.WriteLine(ex.Message);
                throw;
            }
        }


        // GET: Orders/Create
        [Authorize]
        [HttpGet("Create")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Orders/Create
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Create([FromForm] Order order, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                order.OrderDate = DateTime.Now;
                order.UserId = _userManager.GetUserId(User);
                _context.Add(order);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(order);
        }

        // GET: Orders/Edit/5
        [Authorize]
        [HttpGet("Edit/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }

        // POST: Orders/Edit/5
        [Authorize]
        [HttpPost("Edit/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [FromForm] Order order, CancellationToken cancellationToken)
        {
            if (id != order.Id)
            {
                return NotFound();
            }
            order.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            order.Status = "Submitted";
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(order);
        }

        // Add to Cart
        [Authorize]
        [HttpPost("AddToCart")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> AddToCart(int productId, int quantity, CancellationToken cancellationToken)
        {
            // Get the ID of the currently logged-in user
            string userId = _userManager.GetUserId(User);
            // Find the current user's shopping cart
            var cart = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Status == "Cart" && o.UserId == userId, cancellationToken);

            // If the cart doesn't exist, create it
            if (cart == null)
            {
                cart = new Order { Status = "Cart", UserId = userId, OrderDetails = new List<OrderDetail>(), OrderDate = DateTime.Now};
                _context.Orders.Add(cart);
            }

            // Check if the product is already in the cart
            var cartItem = cart.OrderDetails.FirstOrDefault(od => od.ProductId == productId);
            if (cartItem != null)
            {
                // If the product is already in the cart, increase the quantity
                cartItem.Quantity += quantity;
            }
            else
            {
                // If the product is not in the cart, add a new OrderDetail
                var orderDetail = new OrderDetail { ProductId = productId, Quantity = quantity };
                cart.OrderDetails.Add(orderDetail);
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new { success = true, message = "Product added to cart successfully." });
        }



        // Checkout
        [Authorize]
        [HttpPost("Checkout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Checkout(CancellationToken cancellationToken)
        {
            // Retrieve the user ID using UserManager
            string userId = _userManager.GetUserId(User);
            var cart = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Status == "Cart" && o.UserId == userId, cancellationToken);
            System.Diagnostics.Debug.WriteLine($"UserManager UserId: {userId}");
            System.Diagnostics.Debug.WriteLine($"Cart UserId: {cart.UserId}");
            if (cart != null)
            {
                // Update the UserId and Status
                cart.UserId = userId;
                System.Diagnostics.Debug.WriteLine($"Before status change: {cart.Status}");
                cart.Status = "Submitted";
                System.Diagnostics.Debug.WriteLine($"After status change: {cart.Status}");

                // Save the changes to the database
                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Exception: {ex.Message}");
                }
            }

            return RedirectToAction(nameof(Index));
        }


        // GET: Orders/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpGet("Delete/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Delete(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(m => m.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Orders/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost("Delete/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                _context.Orders.Remove(order);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }
    }
}
