using Microsoft.AspNetCore.Mvc;
using ST10449143_CLDV6212_POEPART1.Models;
using ST10449143_CLDV6212_POEPART1.Services;

namespace ST10449143_CLDV6212_POEPART1.Controllers
{
    public class ProductController : Controller
    {
        private readonly IFunctionsApi _api;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IFunctionsApi api, ILogger<ProductController> logger)
        {
            _api = api;
            _logger = logger;
        }

        // Authentication helpers
        private bool IsAuthenticated => !string.IsNullOrEmpty(HttpContext.Session.GetString("UserId"));
        private bool IsAdmin => HttpContext.Session.GetString("Role") == "Admin";
        private string CurrentUserId => HttpContext.Session.GetString("UserId") ?? string.Empty;

        public async Task<IActionResult> Index(string searchString)
        {
            // Products are visible to all authenticated users
            if (!IsAuthenticated)
            {
                TempData["Error"] = "Please login to browse products.";
                return RedirectToAction("Login", "Account");
            }

            var products = await _api.GetProductsAsync();

            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p =>
                    p.ProductName.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                    p.Description.Contains(searchString, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            ViewBag.IsAdmin = IsAdmin;
            return View(products);
        }

        public IActionResult Create()
        {
            if (!IsAuthenticated)
            {
                TempData["Error"] = "Please login to create products.";
                return RedirectToAction("Login", "Account");
            }

            if (!IsAdmin)
            {
                TempData["Error"] = "Access denied. Admin privileges required to create products.";
                return RedirectToAction(nameof(Index));
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
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
                    if (product.Price <= 0)
                    {
                        ModelState.AddModelError("Price", "Price must be greater than zero.");
                        return View(product);
                    }

                    var saved = await _api.CreateProductAsync(product, imageFile);
                    TempData["Success"] = $"Product '{saved.ProductName}' created successfully with price {saved.Price:C}!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating product");
                    ModelState.AddModelError("", $"Error creating product: {ex.Message}");
                }
            }
            return View(product);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (!IsAuthenticated)
            {
                TempData["Error"] = "Please login to edit products.";
                return RedirectToAction("Login", "Account");
            }

            if (!IsAdmin)
            {
                TempData["Error"] = "Access denied. Admin privileges required to edit products.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrEmpty(id))
                return NotFound();

            var product = await _api.GetProductAsync(id);
            if (product == null)
                return NotFound();

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product, IFormFile? imageFile)
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
                    var updated = await _api.UpdateProductAsync(product.Id, product, imageFile);
                    TempData["Success"] = "Product updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating product: {Message}", ex.Message);
                    ModelState.AddModelError("", $"Error updating product: {ex.Message}");
                }
            }
            return View(product);
        }

        public async Task<IActionResult> Details(string id)
        {
            // Product details are visible to all authenticated users
            if (!IsAuthenticated)
            {
                TempData["Error"] = "Please login to view product details.";
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrEmpty(id))
                return NotFound();

            var product = await _api.GetProductAsync(id);
            if (product == null)
                return NotFound();

            ViewBag.IsAdmin = IsAdmin;
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            if (!IsAuthenticated || !IsAdmin)
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                await _api.DeleteProductAsync(id);
                TempData["Success"] = "Product deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting product: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}