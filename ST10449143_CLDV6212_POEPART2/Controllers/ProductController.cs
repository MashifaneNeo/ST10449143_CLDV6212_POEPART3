using Microsoft.AspNetCore.Mvc;
using ST10449143_CLDV6212_POEPART1.Models;
using ST10449143_CLDV6212_POEPART1.Services;
using ST10449143_CLDV6212_POEPART1.Helpers;

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

        private void CheckAuthentication()
        {
            if (!AuthorizationHelper.IsAuthenticated(HttpContext))
            {
                TempData["Error"] = "Please login to view products.";
                throw new UnauthorizedAccessException("Authentication required.");
            }
        }

        private void CheckAdminAccess()
        {
            CheckAuthentication();
            if (!AuthorizationHelper.IsAdmin(HttpContext))
            {
                TempData["Error"] = "Admin privileges required to manage products.";
                throw new UnauthorizedAccessException("Admin access required.");
            }
        }

        public async Task<IActionResult> Index(string searchString)
        {
            try
            {
                CheckAuthentication();

                var products = await _api.GetProductsAsync();

                if (!string.IsNullOrEmpty(searchString))
                {
                    products = products.Where(p =>
                        p.ProductName.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                        p.Description.Contains(searchString, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                ViewBag.IsAdmin = AuthorizationHelper.IsAdmin(HttpContext);
                return View(products);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account");
            }
        }

        public IActionResult Create()
        {
            try
            {
                CheckAdminAccess();
                return View();
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            try
            {
                CheckAdminAccess();

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
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
        }

        public async Task<IActionResult> Edit(string id)
        {
            try
            {
                CheckAdminAccess();

                if (string.IsNullOrEmpty(id))
                    return NotFound();

                var product = await _api.GetProductAsync(id);
                if (product == null)
                    return NotFound();

                return View(product);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product, IFormFile? imageFile)
        {
            try
            {
                CheckAdminAccess();

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
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
        }

        public async Task<IActionResult> Details(string id)
        {
            try
            {
                CheckAuthentication();

                if (string.IsNullOrEmpty(id))
                    return NotFound();

                var product = await _api.GetProductAsync(id);
                if (product == null)
                    return NotFound();

                ViewBag.IsAdmin = AuthorizationHelper.IsAdmin(HttpContext);
                return View(product);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                CheckAdminAccess();

                if (string.IsNullOrEmpty(id))
                {
                    TempData["Error"] = "Product ID is required.";
                    return RedirectToAction(nameof(Index));
                }

                try
                {
                    await _api.DeleteProductAsync(id);
                    TempData["Success"] = "Product deleted successfully!";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting product {ProductId}", id);
                    TempData["Error"] = $"Error deleting product: {ex.Message}";
                }
                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
        }
    }
}