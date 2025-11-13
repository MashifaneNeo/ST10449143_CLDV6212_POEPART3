using Microsoft.AspNetCore.Mvc;
using ST10449143_CLDV6212_POEPART1.Models;
using ST10449143_CLDV6212_POEPART1.Models.ViewModels;
using ST10449143_CLDV6212_POEPART1.Services;
using ST10449143_CLDV6212_POEPART1.Helpers;

namespace ST10449143_CLDV6212_POEPART1.Controllers
{
    public class OrderController : Controller
    {
        private readonly IFunctionsApi _api;

        public OrderController(IFunctionsApi api)
        {
            _api = api;
        }

        private void CheckAuthentication()
        {
            if (!AuthorizationHelper.IsAuthenticated(HttpContext))
            {
                TempData["Error"] = "Please login to access orders.";
                throw new UnauthorizedAccessException("Authentication required.");
            }
        }

        private void CheckAdminAccess()
        {
            CheckAuthentication();
            if (!AuthorizationHelper.IsAdmin(HttpContext))
            {
                TempData["Error"] = "Admin privileges required to manage all orders.";
                throw new UnauthorizedAccessException("Admin access required.");
            }
        }

        public async Task<IActionResult> Index(string searchString)
        {
            try
            {
                CheckAuthentication();

                var orders = await _api.GetOrdersAsync();

                // If user is customer, only show their orders
                if (!AuthorizationHelper.IsAdmin(HttpContext))
                {
                    var currentUsername = AuthorizationHelper.GetUserName(HttpContext);
                    orders = orders.Where(o => o.Username == currentUsername).ToList();
                }

                if (!string.IsNullOrEmpty(searchString))
                {
                    orders = orders.Where(o =>
                        o.CustomerId.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                        o.ProductName.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                        o.Status.Contains(searchString, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                ViewBag.IsAdmin = AuthorizationHelper.IsAdmin(HttpContext);
                return View(orders);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account");
            }
        }

        public async Task<IActionResult> Create()
        {
            try
            {
                CheckAuthentication();

                var customers = await _api.GetCustomersAsync();
                var products = await _api.GetProductsAsync();

                var viewModel = new OrderCreateViewModel
                {
                    Customers = customers,
                    Products = products
                };

                ViewBag.IsAdmin = AuthorizationHelper.IsAdmin(HttpContext);
                return View(viewModel);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderCreateViewModel model)
        {
            try
            {
                CheckAuthentication();

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
                ViewBag.IsAdmin = AuthorizationHelper.IsAdmin(HttpContext);
                return View(model);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account");
            }
        }

        public async Task<IActionResult> Details(string id)
        {
            try
            {
                CheckAuthentication();

                if (string.IsNullOrEmpty(id))
                {
                    return NotFound();
                }

                var order = await _api.GetOrderAsync(id);
                if (order == null)
                {
                    return NotFound();
                }

                // Check if customer is viewing their own order
                if (!AuthorizationHelper.IsAdmin(HttpContext) && order.Username != AuthorizationHelper.GetUserName(HttpContext))
                {
                    TempData["Error"] = "You can only view your own orders.";
                    return RedirectToAction("AccessDenied", "Account");
                }

                ViewBag.IsAdmin = AuthorizationHelper.IsAdmin(HttpContext);
                return View(order);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account");
            }
        }

        public async Task<IActionResult> Edit(string id)
        {
            try
            {
                CheckAdminAccess();

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
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Order order)
        {
            try
            {
                CheckAdminAccess();

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
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                CheckAdminAccess();

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
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetProductPrice(string productId)
        {
            try
            {
                CheckAuthentication();

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
            try
            {
                CheckAdminAccess();

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
            catch (UnauthorizedAccessException)
            {
                return Json(new { success = false, message = "Admin privileges required" });
            }
        }

        private async Task PopulateDropdowns(OrderCreateViewModel model)
        {
            model.Customers = await _api.GetCustomersAsync();
            model.Products = await _api.GetProductsAsync();
        }
    }
}