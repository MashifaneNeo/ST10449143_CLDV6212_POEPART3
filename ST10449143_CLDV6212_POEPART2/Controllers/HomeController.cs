using Microsoft.AspNetCore.Mvc;
using ST10449143_CLDV6212_POEPART1.Models;
using ST10449143_CLDV6212_POEPART1.Models.ViewModels;
using ST10449143_CLDV6212_POEPART1.Services;
using System.Diagnostics;
using ST10449143_CLDV6212_POEPART1.Helpers;

namespace ST10449143_CLDV6212_POEPART1.Controllers
{
    public class HomeController : Controller
    {
        private readonly IFunctionsApi _api;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IFunctionsApi api, ILogger<HomeController> logger)
        {
            _api = api;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var productsTask = _api.GetProductsAsync();
                var customersTask = _api.GetCustomersAsync();
                var ordersTask = _api.GetOrdersAsync();

                await Task.WhenAll(productsTask, customersTask, ordersTask);

                var products = productsTask.Result ?? new List<Product>();
                var customers = customersTask.Result ?? new List<Customer>();
                var orders = ordersTask.Result ?? new List<Order>();

                var vm = new HomeViewModel
                {
                    FeaturedProducts = products.Take(5).ToList(),
                    ProductCount = products.Count,
                    CustomerCount = customers.Count,
                    OrderCount = orders.Count
                };

                // Add user info to ViewBag for display
                ViewBag.UserName = AuthorizationHelper.GetUserName(HttpContext);
                ViewBag.UserRole = AuthorizationHelper.GetUserRole(HttpContext);
                ViewBag.IsAdmin = AuthorizationHelper.IsAdmin(HttpContext);

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load dashboard data from Functions API.");
                TempData["Error"] = "Could not load dashboard data. Please try again.";
                return View(new HomeViewModel());
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
       
    }
}