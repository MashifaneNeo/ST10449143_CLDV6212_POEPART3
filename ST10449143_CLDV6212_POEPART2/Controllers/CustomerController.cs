using Microsoft.AspNetCore.Mvc;
using ST10449143_CLDV6212_POEPART1.Models;
using ST10449143_CLDV6212_POEPART1.Services;
using ST10449143_CLDV6212_POEPART1.Helpers;

namespace ST10449143_CLDV6212_POEPART1.Controllers
{
    public class CustomerController : Controller
    {
        private readonly IFunctionsApi _api;

        public CustomerController(IFunctionsApi api)
        {
            _api = api;
        }

        private void CheckAdminAccess()
        {
            if (!AuthorizationHelper.IsAuthenticated(HttpContext))
            {
                TempData["Error"] = "Please login to access this page.";
                throw new UnauthorizedAccessException("Authentication required.");
            }

            if (!AuthorizationHelper.IsAdmin(HttpContext))
            {
                TempData["Error"] = "Admin privileges required to access customer management.";
                throw new UnauthorizedAccessException("Admin access required.");
            }
        }

        public async Task<IActionResult> Index(string searchString)
        {
            try
            {
                CheckAdminAccess();

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
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("AccessDenied", "Account");
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
        public async Task<IActionResult> Create(Customer customer)
        {
            try
            {
                CheckAdminAccess();

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
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Customer customer)
        {
            try
            {
                CheckAdminAccess();

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
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
        }

        public async Task<IActionResult> Detail(string id)
        {
            try
            {
                CheckAdminAccess();

                if (string.IsNullOrEmpty(id))
                    return NotFound();

                var customer = await _api.GetCustomerAsync(id);
                if (customer == null)
                    return NotFound();

                return View(customer);
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
                    await _api.DeleteCustomerAsync(id);
                    TempData["Success"] = "Customer deleted successfully!";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error deleting customer: {ex.Message}";
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