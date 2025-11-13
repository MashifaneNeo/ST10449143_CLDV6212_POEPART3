// Helpers/AuthorizationHelper.cs
using ST10449143_CLDV6212_POEPART1.Services;

namespace ST10449143_CLDV6212_POEPART1.Helpers
{
    public static class AuthorizationHelper
    {
        public static bool IsAuthenticated(HttpContext context)
        {
            return !string.IsNullOrEmpty(context.Session.GetString("UserId"));
        }

        public static bool IsAdmin(HttpContext context)
        {
            return context.Session.GetString("Role") == "Admin";
        }

        public static bool IsCustomer(HttpContext context)
        {
            return context.Session.GetString("Role") == "Customer";
        }

        public static string GetUserName(HttpContext context)
        {
            return context.Session.GetString("Username") ?? string.Empty;
        }

        public static string GetUserRole(HttpContext context)
        {
            return context.Session.GetString("Role") ?? string.Empty;
        }

        public static void RequireAuthentication(HttpContext context)
        {
            if (!IsAuthenticated(context))
            {
                throw new UnauthorizedAccessException("User must be logged in to access this resource.");
            }
        }

        public static void RequireAdmin(HttpContext context)
        {
            RequireAuthentication(context);
            if (!IsAdmin(context))
            {
                throw new UnauthorizedAccessException("Admin privileges required to access this resource.");
            }
        }
    }
}