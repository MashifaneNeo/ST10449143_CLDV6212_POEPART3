// Controllers/CartController.cs
using Microsoft.AspNetCore.Mvc;
using ST10449143_CLDV6212_POEPART1.Models;
using ST10449143_CLDV6212_POEPART1.Services;
using ST10449143_CLDV6212_POEPART1.Helpers;
using System.Text.Json;

namespace ST10449143_CLDV6212_POEPART1.Controllers
{
    public class CartController : Controller
    {
        private readonly IFunctionsApi _functionsApi;
        private readonly ILogger<CartController> _logger;

        public CartController(IFunctionsApi functionsApi, ILogger<CartController> logger)
        {
            _functionsApi = functionsApi;
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

        private Cart GetOrCreateCart()
        {
            var customerId = HttpContext.Session.GetString("UserId") ?? string.Empty;
            var username = AuthorizationHelper.GetUserName(HttpContext);
            var cartKey = $"Cart_{customerId}";

            var cartJson = HttpContext.Session.GetString(cartKey);
            if (!string.IsNullOrEmpty(cartJson))
            {
                try
                {
                    return JsonSerializer.Deserialize<Cart>(cartJson) ?? CreateNewCart(customerId, username);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deserializing cart from session");
                }
            }

            return CreateNewCart(customerId, username);
        }

        private Cart CreateNewCart(string customerId, string username)
        {
            return new Cart
            {
                Id = $"cart_{Guid.NewGuid()}_{customerId}",
                CustomerId = customerId,
                Username = username,
                CreatedDate = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow,
                IsActive = true,
                Items = new List<CartItem>()
            };
        }

        private void SaveCart(Cart cart)
        {
            var cartKey = $"Cart_{cart.CustomerId}";
            var cartJson = JsonSerializer.Serialize(cart);
            HttpContext.Session.SetString(cartKey, cartJson);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                CheckCustomerAuthentication();

                var cart = GetOrCreateCart();

                // Enrich cart items with product details
                foreach (var item in cart.Items)
                {
                    try
                    {
                        var product = await _functionsApi.GetProductAsync(item.ProductId);
                        if (product != null)
                        {
                            item.Product = product;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not load product details for {ProductId}", item.ProductId);
                    }
                }

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

                // Get product details
                var product = await _functionsApi.GetProductAsync(productId);
                if (product == null)
                {
                    TempData["Error"] = "Product not found.";
                    return RedirectToAction("Index", "Product");
                }

                if (product.StockAvailable < quantity)
                {
                    TempData["Error"] = $"Only {product.StockAvailable} items available in stock.";
                    return RedirectToAction("Index", "Product");
                }

                var cart = GetOrCreateCart();

                // Check if item already exists in cart
                var existingItem = cart.Items.FirstOrDefault(item => item.ProductId == productId);

                if (existingItem != null)
                {
                    // Update quantity
                    var newQuantity = existingItem.Quantity + quantity;
                    if (product.StockAvailable < newQuantity)
                    {
                        TempData["Error"] = $"Cannot add more than {product.StockAvailable} items to cart.";
                        return RedirectToAction("Index", "Product");
                    }
                    existingItem.Quantity = newQuantity;
                }
                else
                {
                    // Add new item
                    var newItem = new CartItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        CartId = cart.Id,
                        ProductId = productId,
                        ProductName = product.ProductName,
                        UnitPrice = product.Price,
                        Quantity = quantity
                    };
                    cart.Items.Add(newItem);
                }

                cart.LastUpdated = DateTime.UtcNow;
                SaveCart(cart);

                TempData["Success"] = $"{product.ProductName} added to cart successfully!";
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

                var cart = GetOrCreateCart();
                var existingItem = cart.Items.FirstOrDefault(item => item.ProductId == productId);

                if (existingItem != null)
                {
                    // Check stock if increasing quantity
                    if (quantity > existingItem.Quantity)
                    {
                        var product = await _functionsApi.GetProductAsync(productId);
                        if (product != null && product.StockAvailable < quantity)
                        {
                            TempData["Error"] = $"Only {product.StockAvailable} items available in stock.";
                            return RedirectToAction("Index");
                        }
                    }

                    if (quantity <= 0)
                    {
                        cart.Items.Remove(existingItem);
                        TempData["Success"] = "Item removed from cart.";
                    }
                    else
                    {
                        existingItem.Quantity = quantity;
                        TempData["Success"] = "Cart updated successfully!";
                    }

                    cart.LastUpdated = DateTime.UtcNow;
                    SaveCart(cart);
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

                var cart = GetOrCreateCart();
                var itemToRemove = cart.Items.FirstOrDefault(item => item.ProductId == productId);

                if (itemToRemove != null)
                {
                    cart.Items.Remove(itemToRemove);
                    cart.LastUpdated = DateTime.UtcNow;
                    SaveCart(cart);
                    TempData["Success"] = "Item removed from cart.";
                }

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

                var customerId = HttpContext.Session.GetString("UserId") ?? string.Empty;
                var cartKey = $"Cart_{customerId}";
                HttpContext.Session.Remove(cartKey);

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

                var cart = GetOrCreateCart();

                if (cart.Items.Count == 0)
                {
                    TempData["Error"] = "Your cart is empty. Add some products before checkout.";
                    return RedirectToAction("Index");
                }

                // Validate stock before checkout
                foreach (var item in cart.Items)
                {
                    var product = await _functionsApi.GetProductAsync(item.ProductId);
                    if (product == null)
                    {
                        TempData["Error"] = $"Product {item.ProductName} is no longer available.";
                        return RedirectToAction("Index");
                    }

                    if (product.StockAvailable < item.Quantity)
                    {
                        TempData["Error"] = $"Only {product.StockAvailable} items of {item.ProductName} available in stock.";
                        return RedirectToAction("Index");
                    }
                }

                // Create orders for each cart item
                Order? mainOrder = null;
                var customerId = HttpContext.Session.GetString("UserId") ?? string.Empty;

                foreach (var item in cart.Items)
                {
                    try
                    {
                        var order = await _functionsApi.CreateOrderAsync(
                            customerId,
                            item.ProductId,
                            item.Quantity
                        );

                        if (mainOrder == null)
                        {
                            mainOrder = order;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating order for product {ProductId}", item.ProductId);
                        throw new Exception($"Failed to create order for {item.ProductName}. Please try again.");
                    }
                }

                // Clear cart after successful checkout
                var cartKey = $"Cart_{customerId}";
                HttpContext.Session.Remove(cartKey);

                TempData["Success"] = $"Order placed successfully! Order ID: {mainOrder?.Id.Substring(0, 8)}... Total: {cart.TotalAmount:C}";
                return RedirectToAction("Details", "Order", new { id = mainOrder?.Id });
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during checkout");
                TempData["Error"] = $"Error during checkout: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public IActionResult GetCartSummary()
        {
            try
            {
                if (!AuthorizationHelper.IsAuthenticated(HttpContext) || AuthorizationHelper.IsAdmin(HttpContext))
                {
                    return Json(new { itemCount = 0, totalAmount = 0 });
                }

                var cart = GetOrCreateCart();

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