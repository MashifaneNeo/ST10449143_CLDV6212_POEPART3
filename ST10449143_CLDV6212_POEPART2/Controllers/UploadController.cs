using Microsoft.AspNetCore.Mvc;
using ST10449143_CLDV6212_POEPART1.Models;
using ST10449143_CLDV6212_POEPART1.Services;
using ST10449143_CLDV6212_POEPART1.Helpers;

namespace ST10449143_CLDV6212_POEPART1.Controllers
{
    public class UploadController : Controller
    {
        private readonly IFunctionsApi _api;

        public UploadController(IFunctionsApi api)
        {
            _api = api;
        }

        private void CheckAdminAccess()
        {
            if (!AuthorizationHelper.IsAuthenticated(HttpContext))
            {
                TempData["Error"] = "Please login to access uploads.";
                throw new UnauthorizedAccessException("Authentication required.");
            }

            if (!AuthorizationHelper.IsAdmin(HttpContext))
            {
                TempData["Error"] = "Admin privileges required to access file uploads.";
                throw new UnauthorizedAccessException("Admin access required.");
            }
        }

        public IActionResult Index()
        {
            try
            {
                CheckAdminAccess();
                return View(new FileUploadModel());
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(FileUploadModel model)
        {
            try
            {
                CheckAdminAccess();

                if (ModelState.IsValid)
                {
                    try
                    {
                        if (model.ProofOfPayment != null && model.ProofOfPayment.Length > 0)
                        {
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
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
        }
    }
}