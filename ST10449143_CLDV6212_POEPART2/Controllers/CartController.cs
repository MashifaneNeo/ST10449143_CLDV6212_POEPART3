using Microsoft.AspNetCore.Mvc;
using ST10449143_CLDV6212_POEPART1.Models;
using ST10449143_CLDV6212_POEPART1.Services;
using ST10449143_CLDV6212_POEPART1.Helpers;
using Microsoft.Extensions.Logging;

namespace ST10449143_CLDV6212_POEPART1.Controllers
{
    public class CartController : Controller
    {
        private readonly IFunctionsApi _functionsApi;
        private readonly ICartService _cartService;
        private readonly ILogger<CartController> _logger;

        public CartController(IFunctionsApi functionsApi, ICartService cartService, ILogger<CartController> logger)
        {
            _functionsApi = functionsApi;
            _cartService = cartService;
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

                var userId = HttpContext.Session.GetString("UserId");
                var username = AuthorizationHelper.GetUserName(HttpContext);

                var cart = await _cartService.GetOrCreateCartAsync(userId, username);

                // Enrich cart items with product details
                foreach (var item in cart.Items)
                {
                    try
                    {
                        var product = await _functionsApi.GetProductAsync(item.ProductId);
                        if (product != null)
                        {
                            // Create a temporary product object for display
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

                var userId = HttpContext.Session.GetString("UserId");

                await _cartService.AddToCartAsync(userId, productId, product.ProductName, (double)product.Price, quantity);

                TempData["Success"] = $"{product.ProductName} added to cart successfully!";
                return RedirectToAction("Index", "Cart"); // Explicitly specify controller
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

                var userId = HttpContext.Session.GetString("UserId");

                if (quantity > 0)
                {
                    // Check stock if increasing quantity
                    var currentCart = await _cartService.GetCartAsync(userId);
                    var currentItem = currentCart?.Items.FirstOrDefault(item => item.ProductId == productId);

                    if (currentItem != null && quantity > currentItem.Quantity)
                    {
                        var product = await _functionsApi.GetProductAsync(productId);
                        if (product != null && product.StockAvailable < quantity)
                        {
                            TempData["Error"] = $"Only {product.StockAvailable} items available in stock.";
                            return RedirectToAction("Index", "Cart");
                        }
                    }
                }

                await _cartService.UpdateCartItemQuantityAsync(userId, productId, quantity);

                TempData["Success"] = quantity <= 0 ? "Item removed from cart." : "Cart updated successfully!";
                return RedirectToAction("Index", "Cart");
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart quantity");
                TempData["Error"] = "Error updating cart. Please try again.";
                return RedirectToAction("Index", "Cart");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem(string productId)
        {
            try
            {
                CheckCustomerAuthentication();

                var userId = HttpContext.Session.GetString("UserId");
                await _cartService.RemoveFromCartAsync(userId, productId);

                TempData["Success"] = "Item removed from cart.";

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Item removed successfully" });
                }

                return RedirectToAction("Index", "Cart");
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from cart");
                TempData["Error"] = "Error removing item from cart. Please try again.";
                return RedirectToAction("Index", "Cart");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            try
            {
                CheckCustomerAuthentication();

                var userId = HttpContext.Session.GetString("UserId");
                await _cartService.ClearCartAsync(userId);

                TempData["Success"] = "Cart cleared successfully!";

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Cart cleared successfully" });
                }

                return RedirectToAction("Index", "Cart");
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart");
                TempData["Error"] = "Error clearing cart. Please try again.";
                return RedirectToAction("Index", "Cart");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout()
        {
            try
            {
                CheckCustomerAuthentication();

                var userId = HttpContext.Session.GetString("UserId");
                var username = AuthorizationHelper.GetUserName(HttpContext);

                var cart = await _cartService.GetCartAsync(userId);
                _logger.LogInformation("Checkout started - Cart ID: {CartId}, Items: {ItemCount}", cart?.CartId, cart?.Items.Count);

                if (cart == null || !cart.Items.Any())
                {
                    TempData["Error"] = "Your cart is empty. Add some products before checkout.";
                    return RedirectToAction("Index", "Cart");
                }

                // Get or create customer record first
                var customerId = await GetOrCreateCustomerId(userId, username);
                if (string.IsNullOrEmpty(customerId))
                {
                    TempData["Error"] = "Could not create customer record. Please contact support.";
                    return RedirectToAction("Index", "Cart");
                }

                // Validate stock before checkout
                var validationErrors = new List<string>();

                foreach (var item in cart.Items)
                {
                    try
                    {
                        var product = await _functionsApi.GetProductAsync(item.ProductId);

                        if (product == null)
                        {
                            validationErrors.Add($"{item.ProductName} is no longer available.");
                        }
                        else if (product.StockAvailable < item.Quantity)
                        {
                            validationErrors.Add($"{item.ProductName} - Only {product.StockAvailable} available, but {item.Quantity} requested.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error validating stock for product {ProductId}", item.ProductId);
                        validationErrors.Add($"Error checking availability for {item.ProductName}.");
                    }
                }

                if (validationErrors.Any())
                {
                    var errorMessage = "Cannot proceed with checkout:\n" + string.Join("\n", validationErrors);
                    TempData["Error"] = errorMessage;
                    return RedirectToAction("Index", "Cart");
                }

                // Create orders for each cart item
                var successfulOrders = new List<Order>();
                var failedOrders = new List<string>();

                foreach (var item in cart.Items)
                {
                    try
                    {
                        var order = await _functionsApi.CreateOrderAsync(customerId, item.ProductId, item.Quantity);

                        if (order != null && !string.IsNullOrEmpty(order.Id))
                        {
                            successfulOrders.Add(order);
                        }
                        else
                        {
                            failedOrders.Add(item.ProductName);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Exception creating order for product: {ProductId}", item.ProductId);
                        failedOrders.Add($"{item.ProductName} - {ex.Message}");
                    }
                }

                // Handle results
                if (failedOrders.Count > 0)
                {
                    var errorMsg = $"Failed to create orders for: {string.Join(", ", failedOrders)}. Please try again.";
                    TempData["Error"] = errorMsg;
                    return RedirectToAction("Index", "Cart");
                }

                if (successfulOrders.Count == 0)
                {
                    TempData["Error"] = "No orders were created. Please try again.";
                    return RedirectToAction("Index", "Cart");
                }

                // Clear cart after successful checkout
                try
                {
                    await _cartService.ClearCartAsync(userId);
                    _logger.LogInformation("Cart cleared after successful checkout");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error clearing cart after checkout");
                    
                }

                var mainOrder = successfulOrders.First();
                var successMessage = $"Order placed successfully! {successfulOrders.Count} item(s) ordered. Order ID: {mainOrder.Id}";

                TempData["Success"] = successMessage;

                // Redirect customers to products page, admins to orders page
                if (AuthorizationHelper.IsAdmin(HttpContext))
                {
                    return RedirectToAction("Index", "Order");
                }
                else
                {
                    return RedirectToAction("Index", "Product");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access during checkout");
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during checkout process");
                TempData["Error"] = $"Checkout failed: {ex.Message}. Please contact support if this continues.";
                return RedirectToAction("Index", "Cart");
            }
        }

        // Helper method to get or create customer ID
        private async Task<string> GetOrCreateCustomerId(string userId, string username)
        {
            try
            {
               
                var customers = await _functionsApi.GetCustomersAsync();
                var existingCustomer = customers.FirstOrDefault(c => c.Username == username);

                if (existingCustomer != null)
                {
                    _logger.LogInformation("Found existing customer: {CustomerId}", existingCustomer.Id);
                    return existingCustomer.Id;
                }

                // Create new customer
                _logger.LogInformation("Creating new customer for user: {Username}", username);

                // Extract first and last name from username or use defaults
                var nameParts = username.Split(' ');
                var firstName = nameParts.Length > 0 ? nameParts[0] : "Customer";
                var lastName = nameParts.Length > 1 ? nameParts[1] : "User";

                var newCustomer = new Customer
                {
                    Name = firstName,
                    Surname = lastName,
                    Username = username,
                    Email = $"{username}@example.com", 
                    ShippingAddress = "Address to be provided" 
                };

                var createdCustomer = await _functionsApi.CreateCustomerAsync(newCustomer);
                _logger.LogInformation("Created new customer with ID: {CustomerId}", createdCustomer.Id);

                return createdCustomer.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting or creating customer for user: {Username}", username);
                return null;
            }
        }

        [HttpGet]
        public async Task<IActionResult> TestApiConnection()
        {
            try
            {
                _logger.LogInformation("Testing API connection...");

                
                var products = await _functionsApi.GetProductsAsync();
                _logger.LogInformation("Products API test - Found {Count} products", products?.Count ?? 0);

                
                var customers = await _functionsApi.GetCustomersAsync();
                _logger.LogInformation("Customers API test - Found {Count} customers", customers?.Count ?? 0);

                return Json(new
                {
                    success = true,
                    productsCount = products?.Count ?? 0,
                    customersCount = customers?.Count ?? 0,
                    message = "API connection test completed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API connection test failed");
                return Json(new
                {
                    success = false,
                    message = $"API test failed: {ex.Message}"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCartSummary()
        {
            try
            {
                if (!AuthorizationHelper.IsAuthenticated(HttpContext) || AuthorizationHelper.IsAdmin(HttpContext))
                {
                    return Json(new { itemCount = 0, totalAmount = 0 });
                }

                var userId = HttpContext.Session.GetString("UserId");
                var cart = await _cartService.GetCartAsync(userId);

                if (cart == null)
                {
                    return Json(new { itemCount = 0, totalAmount = 0 });
                }

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

        // Debug method to check customer creation
        [HttpGet]
        public async Task<IActionResult> DebugCheckout()
        {
            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                var username = AuthorizationHelper.GetUserName(HttpContext);

                var customerId = await GetOrCreateCustomerId(userId, username);

                return Json(new
                {
                    userId,
                    username,
                    customerId,
                    message = customerId != null ? "Customer ID created/retrieved successfully" : "Failed to create customer ID"
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
    }
}