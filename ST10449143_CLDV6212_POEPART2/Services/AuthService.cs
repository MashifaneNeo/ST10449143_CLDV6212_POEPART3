using Microsoft.Data.SqlClient;
using ST10449143_CLDV6212_POEPART1.Models;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace ST10449143_CLDV6212_POEPART1.Services
{
    public class AuthService : IAuthService
    {
        private readonly string _connectionString;

        public AuthService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("AzureSQL")
                ?? throw new InvalidOperationException("AzureSQL connection string is missing");
        }

        public async Task<bool> RegisterAsync(RegisterViewModel model)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Check if username or email already exists
            var checkCmd = new SqlCommand(
                "SELECT COUNT(*) FROM Users WHERE Username = @Username OR Email = @Email",
                connection);
            checkCmd.Parameters.AddWithValue("@Username", model.Username);
            checkCmd.Parameters.AddWithValue("@Email", model.Email);

            var exists = (int)await checkCmd.ExecuteScalarAsync();
            if (exists > 0) return false;

            // Hash password
            var passwordHash = HashPassword(model.Password);

            // Insert new user
            var insertCmd = new SqlCommand(@"
                INSERT INTO Users (Username, Email, PasswordHash, FirstName, LastName, Role)
                VALUES (@Username, @Email, @PasswordHash, @FirstName, @LastName, 'Customer')",
                connection);

            insertCmd.Parameters.AddWithValue("@Username", model.Username);
            insertCmd.Parameters.AddWithValue("@Email", model.Email);
            insertCmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
            insertCmd.Parameters.AddWithValue("@FirstName", model.FirstName);
            insertCmd.Parameters.AddWithValue("@LastName", model.LastName);

            var result = await insertCmd.ExecuteNonQueryAsync();
            return result > 0;
        }

        public async Task<(bool success, User user)> LoginAsync(LoginViewModel model)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = new SqlCommand(
                "SELECT UserId, Username, Email, FirstName, LastName, Role, PasswordHash FROM Users WHERE (Username = @Username OR Email = @Username) AND IsActive = 1",
                connection);
            cmd.Parameters.AddWithValue("@Username", model.Username);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var storedHash = reader["PasswordHash"].ToString();
                if (VerifyPassword(model.Password, storedHash))
                {
                    var user = new User
                    {
                        UserId = reader["UserId"].ToString() ?? string.Empty,
                        Username = reader["Username"].ToString() ?? string.Empty,
                        Email = reader["Email"].ToString() ?? string.Empty,
                        FirstName = reader["FirstName"].ToString() ?? string.Empty,
                        LastName = reader["LastName"].ToString() ?? string.Empty,
                        Role = reader["Role"].ToString() ?? string.Empty
                    };

                    // Update last login date
                    await reader.CloseAsync();
                    await UpdateLastLogin(user.UserId);

                    return (true, user);
                }
            }
            return (false, null);
        }

        public async Task<User> GetUserAsync(string username)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = new SqlCommand(
                "SELECT UserId, Username, Email, FirstName, LastName, Role FROM Users WHERE Username = @Username",
                connection);
            cmd.Parameters.AddWithValue("@Username", username);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new User
                {
                    UserId = reader["UserId"].ToString() ?? string.Empty,
                    Username = reader["Username"].ToString() ?? string.Empty,
                    Email = reader["Email"].ToString() ?? string.Empty,
                    FirstName = reader["FirstName"].ToString() ?? string.Empty,
                    LastName = reader["LastName"].ToString() ?? string.Empty,
                    Role = reader["Role"].ToString() ?? string.Empty
                };
            }
            return null;
        }

        private async Task UpdateLastLogin(string userId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = new SqlCommand(
                "UPDATE Users SET LastLoginDate = GETUTCDATE() WHERE UserId = @UserId",
                connection);
            cmd.Parameters.AddWithValue("@UserId", userId);
            await cmd.ExecuteNonQueryAsync();
        }

        private string HashPassword(string password)
        {
            // Generate a 128-bit salt
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Derive a 256-bit subkey
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            return $"{Convert.ToBase64String(salt)}.{hashed}";
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            try
            {
                var parts = storedHash.Split('.', 2);
                if (parts.Length != 2) return false;

                var salt = Convert.FromBase64String(parts[0]);
                var expectedHash = parts[1];

                string actualHash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                    password: password,
                    salt: salt,
                    prf: KeyDerivationPrf.HMACSHA256,
                    iterationCount: 10000,
                    numBytesRequested: 256 / 8));

                return actualHash == expectedHash;
            }
            catch
            {
                return false;
            }
        }
    }
}