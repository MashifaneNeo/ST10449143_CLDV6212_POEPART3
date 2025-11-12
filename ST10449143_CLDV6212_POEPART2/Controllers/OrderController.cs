using Microsoft.AspNetCore.Mvc;
using ST10449143_CLDV6212_POEPART1.Models;
using ST10449143_CLDV6212_POEPART1.Models.ViewModels;
using ST10449143_CLDV6212_POEPART1.Services;

namespace ST10449143_CLDV6212_POEPART1.Controllers
{
    public class OrderController : Controller
    {
        private readonly IFunctionsApi _api;

        public OrderController(IFunctionsApi api)
        {
            _api = api;
        }

        // Authentication helpers
        private bool IsAuthenticated => !string.IsNullOrEmpty(HttpContext.Session.GetString("UserId"));
        private bool IsAdmin => HttpContext.Session.GetString("Role") == "Admin";
        private string CurrentUserId => HttpContext.Session.GetString("UserId") ?? string.Empty;
        private string CurrentUsername => HttpContext.Session.GetString("Username") ?? string.Empty;

        public async Task<IActionResult> Index(string searchString)
        {
            if (!IsAuthenticated)
            {
                TempData["Error"] = "Please login to view orders.";
                return RedirectToAction("Login", "Account");
            }

            var orders = await _api.GetOrdersAsync();

            // If user is not admin, only show their orders
            if (!IsAdmin)
            {
                // Filter orders by current user (you may need to adjust this logic based on your data structure)
                orders = orders.Where(o => o.Username == CurrentUsername).ToList();
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                orders = orders.Where(o =>
                    o.CustomerId.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                    o.ProductName.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                    o.Status.Contains(searchString, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            ViewBag.IsAdmin = IsAdmin;
            return View(orders);
        }

        public async Task<IActionResult> Create()
        {
            if (!IsAuthenticated)
            {
                TempData["Error"] = "Please login to create orders.";
                return RedirectToAction("Login", "Account");
            }

            var customers = await _api.GetCustomersAsync();
            var products = await _api.GetProductsAsync();

            var viewModel = new OrderCreateViewModel
            {
                Customers = customers,
                Products = products
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderCreateViewModel model)
        {
            if (!IsAuthenticated)
            {
                TempData["Error"] = "Please login to create orders.";
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var order = await _api.CreateOrderAsync(model.CustomerId, model.ProductId, model.Quantity);
                    TempData["Success"] = "Order created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating order: {ex.Message}");
                }
            }

            await PopulateDropdowns(model);
            return View(model);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (!IsAuthenticated)
            {
                TempData["Error"] = "Please login to view order details.";
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var order = await _api.GetOrderAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            // Check if user has permission to view this order
            if (!IsAdmin && order.Username != CurrentUsername)
            {
                TempData["Error"] = "Access denied. You can only view your own orders.";
                return RedirectToAction(nameof(Index));
            }

            return View(order);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (!IsAuthenticated)
            {
                TempData["Error"] = "Please login to edit orders.";
                return RedirectToAction("Login", "Account");
            }

            if (!IsAdmin)
            {
                TempData["Error"] = "Access denied. Only administrators can edit orders.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var order = await _api.GetOrderAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Order order)
        {
            if (!IsAuthenticated || !IsAdmin)
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _api.UpdateOrderStatusAsync(order.Id, order.Status);
                    TempData["Success"] = "Order updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating order: {ex.Message}");
                }
            }
            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            if (!IsAuthenticated)
            {
                TempData["Error"] = "Please login to delete orders.";
                return RedirectToAction("Login", "Account");
            }

            if (!IsAdmin)
            {
                TempData["Error"] = "Access denied. Only administrators can delete orders.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _api.DeleteOrderAsync(id);
                TempData["Success"] = "Order deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting order: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<JsonResult> GetProductPrice(string productId)
        {
            if (!IsAuthenticated)
            {
                return Json(new { success = false, message = "Authentication required" });
            }

            try
            {
                var product = await _api.GetProductAsync(productId);
                if (product != null)
                {
                    return Json(new
                    {
                        success = true,
                        price = product.Price,
                        stock = product.StockAvailable,
                        productName = product.ProductName
                    });
                }
                return Json(new { success = false });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(string id, string newStatus)
        {
            if (!IsAuthenticated || !IsAdmin)
            {
                return Json(new { success = false, message = "Access denied" });
            }

            try
            {
                await _api.UpdateOrderStatusAsync(id, newStatus);
                return Json(new { success = true, message = $"Order status updated to {newStatus}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task PopulateDropdowns(OrderCreateViewModel model)
        {
            model.Customers = await _api.GetCustomersAsync();
            model.Products = await _api.GetProductsAsync();
        }
    }
}