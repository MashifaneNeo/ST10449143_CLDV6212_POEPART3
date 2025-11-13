// Controllers/CartController.cs
using Microsoft.AspNetCore.Mvc;
using ST10449143_CLDV6212_POEPART1.Models;
using ST10449143_CLDV6212_POEPART1.Services;
using ST10449143_CLDV6212_POEPART1.Helpers;

namespace ST10449143_CLDV6212_POEPART1.Controllers
{
    public class CartController : Controller
    {
        private readonly IFunctionsApi _api;
        private readonly ILogger<CartController> _logger;

        public CartController(IFunctionsApi api, ILogger<CartController> logger)
        {
            _api = api;
            _logger = logger;
        }

        private void CheckCustomerAuthentication()
        {
            if (!AuthorizationHelper.IsAuthenticated(HttpContext))
            {
                TempData["Error"] = "Please login to access your cart.";
                throw new UnauthorizedAccessException("Authentication required.");
            }

            if (AuthorizationHelper.IsAdmin(HttpContext))
            {
                TempData["Error"] = "Cart functionality is for customers only.";
                throw new UnauthorizedAccessException("Customers only.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                CheckCustomerAuthentication();

                var username = AuthorizationHelper.GetUserName(HttpContext);
                var customerId = HttpContext.Session.GetString("UserId") ?? string.Empty;

                var cart = await _api.GetOrCreateCartAsync(customerId, username);
                return View(cart);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cart");
                TempData["Error"] = "Error loading your cart. Please try again.";
                return RedirectToAction("Index", "Product");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(string productId, int quantity = 1)
        {
            try
            {
                CheckCustomerAuthentication();

                if (string.IsNullOrEmpty(productId) || quantity < 1)
                {
                    TempData["Error"] = "Invalid product or quantity.";
                    return RedirectToAction("Index", "Product");
                }

                var username = AuthorizationHelper.GetUserName(HttpContext);
                var customerId = HttpContext.Session.GetString("UserId") ?? string.Empty;

                // Get or create cart
                var cart = await _api.GetOrCreateCartAsync(customerId, username);

                // Add item to cart
                cart = await _api.AddToCartAsync(cart.Id, productId, quantity);

                TempData["Success"] = "Product added to cart successfully!";
                return RedirectToAction("Index");
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product to cart");
                TempData["Error"] = "Error adding product to cart. Please try again.";
                return RedirectToAction("Index", "Product");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(string productId, int quantity)
        {
            try
            {
                CheckCustomerAuthentication();

                var username = AuthorizationHelper.GetUserName(HttpContext);
                var customerId = HttpContext.Session.GetString("UserId") ?? string.Empty;

                var cart = await _api.GetOrCreateCartAsync(customerId, username);

                if (quantity <= 0)
                {
                    // Remove item if quantity is 0 or less
                    cart = await _api.RemoveFromCartAsync(cart.Id, productId);
                    TempData["Success"] = "Item removed from cart.";
                }
                else
                {
                    // Update quantity
                    cart = await _api.UpdateCartItemAsync(cart.Id, productId, quantity);
                    TempData["Success"] = "Cart updated successfully!";
                }

                return RedirectToAction("Index");
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart quantity");
                TempData["Error"] = "Error updating cart. Please try again.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem(string productId)
        {
            try
            {
                CheckCustomerAuthentication();

                var username = AuthorizationHelper.GetUserName(HttpContext);
                var customerId = HttpContext.Session.GetString("UserId") ?? string.Empty;

                var cart = await _api.GetOrCreateCartAsync(customerId, username);
                cart = await _api.RemoveFromCartAsync(cart.Id, productId);

                TempData["Success"] = "Item removed from cart.";
                return RedirectToAction("Index");
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from cart");
                TempData["Error"] = "Error removing item from cart. Please try again.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            try
            {
                CheckCustomerAuthentication();

                var username = AuthorizationHelper.GetUserName(HttpContext);
                var customerId = HttpContext.Session.GetString("UserId") ?? string.Empty;

                var cart = await _api.GetOrCreateCartAsync(customerId, username);
                await _api.ClearCartAsync(cart.Id);

                TempData["Success"] = "Cart cleared successfully!";
                return RedirectToAction("Index");
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart");
                TempData["Error"] = "Error clearing cart. Please try again.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout()
        {
            try
            {
                CheckCustomerAuthentication();

                var username = AuthorizationHelper.GetUserName(HttpContext);
                var customerId = HttpContext.Session.GetString("UserId") ?? string.Empty;

                var cart = await _api.GetOrCreateCartAsync(customerId, username);

                if (cart.Items.Count == 0)
                {
                    TempData["Error"] = "Your cart is empty. Add some products before checkout.";
                    return RedirectToAction("Index");
                }

                // Process checkout - this should create an order and clear the cart
                var order = await _api.CheckoutCartAsync(cart.Id);

                TempData["Success"] = $"Order placed successfully! Order ID: {order.Id.Substring(0, 8)}... Total: {order.TotalPrice:C}";
                return RedirectToAction("Details", "Order", new { id = order.Id });
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during checkout");
                TempData["Error"] = "Error during checkout. Please try again.";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetCartSummary()
        {
            try
            {
                if (!AuthorizationHelper.IsAuthenticated(HttpContext) || AuthorizationHelper.IsAdmin(HttpContext))
                {
                    return Json(new { itemCount = 0, totalAmount = 0 });
                }

                var username = AuthorizationHelper.GetUserName(HttpContext);
                var customerId = HttpContext.Session.GetString("UserId") ?? string.Empty;

                var cart = await _api.GetOrCreateCartAsync(customerId, username);

                return Json(new
                {
                    itemCount = cart.Items.Sum(item => item.Quantity),
                    totalAmount = cart.TotalAmount
                });
            }
            catch
            {
                return Json(new { itemCount = 0, totalAmount = 0 });
            }
        }
    }
}