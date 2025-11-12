using Microsoft.AspNetCore.Mvc;
using ST10449143_CLDV6212_POEPART1.Models;
using ST10449143_CLDV6212_POEPART1.Services;

namespace ST10449143_CLDV6212_POEPART1.Controllers
{
    public class UploadController : Controller
    {
        private readonly IFunctionsApi _api;

        public UploadController(IFunctionsApi api)
        {
            _api = api;
        }

        // Authentication helpers
        private bool IsAuthenticated => !string.IsNullOrEmpty(HttpContext.Session.GetString("UserId"));
        private bool IsAdmin => HttpContext.Session.GetString("Role") == "Admin";
        private string CurrentUserId => HttpContext.Session.GetString("UserId") ?? string.Empty;
        private string CurrentUsername => HttpContext.Session.GetString("Username") ?? string.Empty;

        public IActionResult Index()
        {
            if (!IsAuthenticated)
            {
                TempData["Error"] = "Please login to upload files.";
                return RedirectToAction("Login", "Account");
            }

            return View(new FileUploadModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(FileUploadModel model)
        {
            if (!IsAuthenticated)
            {
                TempData["Error"] = "Please login to upload files.";
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (model.ProofOfPayment != null && model.ProofOfPayment.Length > 0)
                    {
                        // Auto-populate customer name if not provided
                        if (string.IsNullOrEmpty(model.CustomerName))
                        {
                            model.CustomerName = HttpContext.Session.GetString("FullName") ?? CurrentUsername;
                        }

                        var fileName = await _api.UploadProofOfPaymentAsync(
                            model.ProofOfPayment,
                            model.OrderId,
                            model.CustomerName
                        );

                        TempData["Success"] = $"File uploaded successfully! File name: {fileName}";
                        return View(new FileUploadModel());
                    }
                    else
                    {
                        ModelState.AddModelError("ProofOfPayment", "Please select a file to upload.");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error uploading file: {ex.Message}");
                }
            }

            return View(model);
        }
    }
}