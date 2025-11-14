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

        private void CheckAuthentication()
        {
            if (!AuthorizationHelper.IsAuthenticated(HttpContext))
            {
                TempData["Error"] = "Please login to upload files.";
                throw new UnauthorizedAccessException("Authentication required.");
            }
        }

        public IActionResult Index()
        {
            try
            {
                CheckAuthentication();
                return View(new FileUploadModel());
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(FileUploadModel model)
        {
            try
            {
                CheckAuthentication();

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
                return RedirectToAction("Login", "Account");
            }
        }
    }
}