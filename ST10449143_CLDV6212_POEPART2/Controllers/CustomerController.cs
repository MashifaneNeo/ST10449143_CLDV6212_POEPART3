using Microsoft.AspNetCore.Mvc;
using ST10449143_CLDV6212_POEPART1.Models;
using ST10449143_CLDV6212_POEPART1.Services;

namespace ST10449143_CLDV6212_POEPART1.Controllers
{
    public class CustomerController : Controller
    {
        private readonly IFunctionsApi _api;

        public CustomerController(IFunctionsApi api)
        {
            _api = api;
        }

        // Authentication helpers
        private bool IsAuthenticated => !string.IsNullOrEmpty(HttpContext.Session.GetString("UserId"));
        private bool IsAdmin => HttpContext.Session.GetString("Role") == "Admin";
        private string CurrentUserId => HttpContext.Session.GetString("UserId") ?? string.Empty;

        public async Task<IActionResult> Index(string searchString)
        {
            if (!IsAuthenticated)
            {
                TempData["Error"] = "Please login to access customer management.";
                return RedirectToAction("Login", "Account");
            }

            if (!IsAdmin)
            {
                TempData["Error"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Index", "Home");
            }

            var customers = await _api.GetCustomersAsync();

            if (!string.IsNullOrEmpty(searchString))
            {
                customers = customers.Where(c =>
                    c.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                    c.Surname.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                    c.Email.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                    c.Username.Contains(searchString, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            return View(customers);
        }

        public IActionResult Create()
        {
            if (!IsAuthenticated)
            {
                TempData["Error"] = "Please login to create customers.";
                return RedirectToAction("Login", "Account");
            }

            if (!IsAdmin)
            {
                TempData["Error"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
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
                    await _api.CreateCustomerAsync(customer);
                    TempData["Success"] = "Customer created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating customer: {ex.Message}");
                }
            }
            return View(customer);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (!IsAuthenticated)
            {
                TempData["Error"] = "Please login to edit customers.";
                return RedirectToAction("Login", "Account");
            }

            if (!IsAdmin)
            {
                TempData["Error"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var customer = await _api.GetCustomerAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Customer customer)
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
                    await _api.UpdateCustomerAsync(customer.Id, customer);
                    TempData["Success"] = "Customer updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating customer: {ex.Message}");
                }
            }
            return View(customer);
        }

        public async Task<IActionResult> Detail(string id)
        {
            if (!IsAuthenticated)
            {
                TempData["Error"] = "Please login to view customer details.";
                return RedirectToAction("Login", "Account");
            }

            if (!IsAdmin)
            {
                TempData["Error"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrEmpty(id))
                return NotFound();

            var customer = await _api.GetCustomerAsync(id);
            if (customer == null)
                return NotFound();

            return View(customer);
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
                await _api.DeleteCustomerAsync(id);
                TempData["Success"] = "Customer deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting customer: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}