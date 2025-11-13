using Microsoft.AspNetCore.Mvc;
using ST10449143_CLDV6212_POEPART1.Models;
using ST10449143_CLDV6212_POEPART1.Models.ViewModels;
using ST10449143_CLDV6212_POEPART1.Services;
using ST10449143_CLDV6212_POEPART1.Helpers;
using Microsoft.Extensions.Logging;

namespace ST10449143_CLDV6212_POEPART1.Controllers
{
    public class OrderController : Controller
    {
        private readonly IFunctionsApi _functionsApi;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IFunctionsApi functionsApi, ILogger<OrderController> logger)
        {
            _functionsApi = functionsApi;
            _logger = logger;
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

        public async Task<IActionResult> Index(string searchString, string statusFilter = "")
        {
            try
            {
                CheckAuthentication();

                var orders = await _functionsApi.GetOrdersAsync();
                var currentUsername = AuthorizationHelper.GetUserName(HttpContext);
                var isAdmin = AuthorizationHelper.IsAdmin(HttpContext);

                // If user is customer, only show their orders
                if (!isAdmin)
                {
                    orders = orders.Where(o => o.Username == currentUsername).ToList();
                    _logger.LogInformation("Customer view - Showing {OrderCount} orders for user: {Username}", orders.Count, currentUsername);
                }
                else
                {
                    _logger.LogInformation("Admin view - Showing all {OrderCount} orders", orders.Count);
                }

                // Apply search filter
                if (!string.IsNullOrEmpty(searchString))
                {
                    orders = orders.Where(o =>
                        o.CustomerId.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                        o.ProductName.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                        o.Status.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                        o.Username.Contains(searchString, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                // Apply status filter
                if (!string.IsNullOrEmpty(statusFilter))
                {
                    orders = orders.Where(o => o.Status.Equals(statusFilter, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                ViewBag.IsAdmin = isAdmin;
                ViewBag.SearchString = searchString;
                ViewBag.StatusFilter = statusFilter;
                ViewBag.AllStatuses = new List<string> { "Submitted", "Processing", "Processed", "Completed", "Cancelled" };

                return View(orders.OrderByDescending(o => o.OrderDate).ToList());
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

                // For customers, redirect to cart system
                if (!AuthorizationHelper.IsAdmin(HttpContext))
                {
                    TempData["Info"] = "Please use the shopping cart to place orders.";
                    return RedirectToAction("Index", "Product");
                }

                var customers = await _functionsApi.GetCustomersAsync();
                var products = await _functionsApi.GetProductsAsync();

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

                if (!AuthorizationHelper.IsAdmin(HttpContext))
                {
                    TempData["Error"] = "Only administrators can create orders directly. Please use the shopping cart.";
                    return RedirectToAction("Index", "Product");
                }

                if (ModelState.IsValid)
                {
                    try
                    {
                        var order = await _functionsApi.CreateOrderAsync(model.CustomerId, model.ProductId, model.Quantity);
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

                var order = await _functionsApi.GetOrderAsync(id);
                if (order == null)
                {
                    return NotFound();
                }

                var currentUsername = AuthorizationHelper.GetUserName(HttpContext);
                var isAdmin = AuthorizationHelper.IsAdmin(HttpContext);

                // Check if customer is viewing their own order
                if (!isAdmin && order.Username != currentUsername)
                {
                    TempData["Error"] = "You can only view your own orders.";
                    return RedirectToAction("AccessDenied", "Account");
                }

                ViewBag.IsAdmin = isAdmin;
                ViewBag.CanEditStatus = isAdmin;
                ViewBag.AllStatuses = new List<string> { "Submitted", "Processing", "Processed", "Completed", "Cancelled" };

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

                var order = await _functionsApi.GetOrderAsync(id);
                if (order == null)
                {
                    return NotFound();
                }

                ViewBag.AllStatuses = new List<string> { "Submitted", "Processing", "Processed", "Completed", "Cancelled" };
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
                        await _functionsApi.UpdateOrderStatusAsync(order.Id, order.Status);
                        TempData["Success"] = $"Order status updated to {order.Status} successfully!";
                        return RedirectToAction(nameof(Details), new { id = order.Id });
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", $"Error updating order: {ex.Message}");
                    }
                }

                ViewBag.AllStatuses = new List<string> { "Submitted", "Processing", "Processed", "Completed", "Cancelled" };
                return View(order);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(string id, string status)
        {
            try
            {
                CheckAdminAccess();

                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(status))
                {
                    TempData["Error"] = "Order ID and status are required.";
                    return RedirectToAction(nameof(Index));
                }

                try
                {
                    await _functionsApi.UpdateOrderStatusAsync(id, status);
                    TempData["Success"] = $"Order status updated to {status} successfully!";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error updating order status: {ex.Message}";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
        }

        [HttpPost]
        public async Task<JsonResult> UpdateStatusAjax(string id, string newStatus)
        {
            try
            {
                CheckAdminAccess();

                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(newStatus))
                {
                    return Json(new { success = false, message = "Order ID and status are required." });
                }

                try
                {
                    await _functionsApi.UpdateOrderStatusAsync(id, newStatus);
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

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                CheckAdminAccess();

                try
                {
                    await _functionsApi.DeleteOrderAsync(id);
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

                var product = await _functionsApi.GetProductAsync(productId);
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

        [HttpGet]
        public async Task<IActionResult> CustomerOrders()
        {
            try
            {
                CheckAuthentication();

                if (AuthorizationHelper.IsAdmin(HttpContext))
                {
                    return RedirectToAction("Index");
                }

                var currentUsername = AuthorizationHelper.GetUserName(HttpContext);
                var orders = await _functionsApi.GetOrdersAsync();

                var customerOrders = orders
                    .Where(o => o.Username == currentUsername)
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();

                ViewBag.IsAdmin = false;
                return View("Index", customerOrders);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account");
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetOrderStats()
        {
            try
            {
                CheckAdminAccess();

                var orders = await _functionsApi.GetOrdersAsync();

                var stats = new
                {
                    totalOrders = orders.Count,
                    submittedOrders = orders.Count(o => o.Status == "Submitted"),
                    processingOrders = orders.Count(o => o.Status == "Processing"),
                    processedOrders = orders.Count(o => o.Status == "Processed"),
                    completedOrders = orders.Count(o => o.Status == "Completed"),
                    cancelledOrders = orders.Count(o => o.Status == "Cancelled"),
                    totalRevenue = orders.Where(o => o.Status != "Cancelled").Sum(o => o.TotalPrice)
                };

                return Json(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task PopulateDropdowns(OrderCreateViewModel model)
        {
            model.Customers = await _functionsApi.GetCustomersAsync();
            model.Products = await _functionsApi.GetProductsAsync();
        }
    }
}