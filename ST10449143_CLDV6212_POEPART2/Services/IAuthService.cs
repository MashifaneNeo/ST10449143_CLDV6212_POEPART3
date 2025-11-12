using ST10449143_CLDV6212_POEPART1.Models;

namespace ST10449143_CLDV6212_POEPART1.Services
{
    public interface IAuthService
    {
        Task<bool> RegisterAsync(RegisterViewModel model);
        Task<(bool success, User user)> LoginAsync(LoginViewModel model);
        Task<User> GetUserAsync(string username);
    }

    public class User
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}