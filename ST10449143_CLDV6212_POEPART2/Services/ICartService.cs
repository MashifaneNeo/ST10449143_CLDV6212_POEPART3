using Microsoft.Data.SqlClient;
using ST10449143_CLDV6212_POEPART1.Models;
using System.Data;

namespace ST10449143_CLDV6212_POEPART1.Services
{
    public interface ICartService
    {
        Task<Cart> GetOrCreateCartAsync(string userId, string username);
        Task<Cart> GetCartAsync(string userId);
        Task AddToCartAsync(string userId, string productId, string productName, double unitPrice, int quantity);
        Task UpdateCartItemQuantityAsync(string userId, string productId, int quantity);
        Task RemoveFromCartAsync(string userId, string productId);
        Task ClearCartAsync(string userId);
        Task<int> GetCartItemCountAsync(string userId);
    }

    public class CartService : ICartService
    {
        private readonly string _connectionString;
        private readonly ILogger<CartService> _logger;

        public CartService(IConfiguration configuration, ILogger<CartService> logger)
        {
            _connectionString = configuration.GetConnectionString("AzureSQL")
                ?? throw new InvalidOperationException("AzureSQL connection string is missing");
            _logger = logger;
        }

        public async Task<Cart> GetOrCreateCartAsync(string userId, string username)
        {
            _logger.LogInformation("GetOrCreateCartAsync - UserId: {UserId}, Username: {Username}", userId, username);

            var cart = await GetCartAsync(userId);
            if (cart == null)
            {
                _logger.LogInformation("No cart found, creating new cart for user: {UserId}", userId);
                cart = new Cart(userId, username);
                await CreateCartAsync(cart);
                _logger.LogInformation("New cart created with ID: {CartId}", cart.CartId);
            }
            else
            {
                _logger.LogInformation("Existing cart found with ID: {CartId}, Items: {ItemCount}", cart.CartId, cart.Items.Count);
            }

            return cart;
        }

        public async Task<Cart> GetCartAsync(string userId)
        {
            _logger.LogInformation("GetCartAsync - UserId: {UserId}", userId);

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("GetCartAsync called with null or empty userId");
                return null;
            }

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                
                var userGuid = await GetUserGuid(userId, connection);
                if (userGuid == Guid.Empty)
                {
                    _logger.LogWarning("User not found in database: {UserId}", userId);
                    return null;
                }

                var cartCmd = new SqlCommand(@"
                    SELECT CartId, UserId, Username, CreatedDate, LastUpdated, IsActive 
                    FROM Cart 
                    WHERE UserId = @UserId AND IsActive = 1", connection);

                cartCmd.Parameters.AddWithValue("@UserId", userGuid);

                using var cartReader = await cartCmd.ExecuteReaderAsync();
                if (await cartReader.ReadAsync())
                {
                    var cart = new Cart
                    {
                        CartId = cartReader.GetGuid("CartId"),
                        UserId = cartReader.GetGuid("UserId").ToString(),
                        Username = cartReader.GetString("Username"),
                        CreatedDate = cartReader.GetDateTime("CreatedDate"),
                        LastUpdated = cartReader.GetDateTime("LastUpdated"),
                        IsActive = cartReader.GetBoolean("IsActive")
                    };

                    await cartReader.CloseAsync();
                    _logger.LogInformation("Cart found - ID: {CartId}, Username: {Username}", cart.CartId, cart.Username);

                    // Load cart items
                    var itemsCmd = new SqlCommand(@"
                        SELECT CartItemId, ProductId, ProductName, UnitPrice, Quantity, CreatedDate
                        FROM CartItem 
                        WHERE CartId = @CartId", connection);
                    itemsCmd.Parameters.AddWithValue("@CartId", cart.CartId);

                    using var itemsReader = await itemsCmd.ExecuteReaderAsync();
                    while (await itemsReader.ReadAsync())
                    {
                        var cartItem = new CartItem
                        {
                            CartItemId = itemsReader.GetGuid("CartItemId"),
                            CartId = cart.CartId,
                            ProductId = itemsReader.GetString("ProductId"),
                            ProductName = itemsReader.GetString("ProductName"),
                            UnitPrice = itemsReader.GetDecimal("UnitPrice"),
                            Quantity = itemsReader.GetInt32("Quantity"),
                            CreatedDate = itemsReader.GetDateTime("CreatedDate")
                        };
                        cart.Items.Add(cartItem);

                        _logger.LogDebug("Cart item loaded - Product: {ProductName}, Quantity: {Quantity}",
                            cartItem.ProductName, cartItem.Quantity);
                    }

                    _logger.LogInformation("Loaded {ItemCount} items for cart {CartId}", cart.Items.Count, cart.CartId);
                    return cart;
                }

                _logger.LogInformation("No active cart found for user: {UserId}", userId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart for user: {UserId}", userId);
                throw;
            }
        }

        private async Task<Guid> GetUserGuid(string userId, SqlConnection connection)
        {
            try
            {
                
                if (Guid.TryParse(userId, out var userGuid))
                {
                    // Verify the user exists
                    var userCmd = new SqlCommand("SELECT COUNT(*) FROM Users WHERE UserId = @UserId", connection);
                    userCmd.Parameters.AddWithValue("@UserId", userGuid);
                    var userCount = (int)await userCmd.ExecuteScalarAsync();
                    return userCount > 0 ? userGuid : Guid.Empty;
                }
                else
                {
                    
                    var userCmd = new SqlCommand("SELECT UserId FROM Users WHERE Username = @Username", connection);
                    userCmd.Parameters.AddWithValue("@Username", userId);
                    var result = await userCmd.ExecuteScalarAsync();
                    return result != null ? (Guid)result : Guid.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user GUID for: {UserId}", userId);
                return Guid.Empty;
            }
        }

        private async Task CreateCartAsync(Cart cart)
        {
            _logger.LogInformation("CreateCartAsync - Creating cart for user: {UserId}", cart.UserId);

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var userGuid = await GetUserGuid(cart.UserId, connection);
                if (userGuid == Guid.Empty)
                {
                    throw new InvalidOperationException($"User not found: {cart.UserId}");
                }

                var cmd = new SqlCommand(@"
                    INSERT INTO Cart (CartId, UserId, Username, CreatedDate, LastUpdated, IsActive)
                    VALUES (@CartId, @UserId, @Username, @CreatedDate, @LastUpdated, @IsActive)", connection);

                cmd.Parameters.AddWithValue("@CartId", cart.CartId);
                cmd.Parameters.AddWithValue("@UserId", userGuid);
                cmd.Parameters.AddWithValue("@Username", cart.Username);
                cmd.Parameters.AddWithValue("@CreatedDate", cart.CreatedDate);
                cmd.Parameters.AddWithValue("@LastUpdated", cart.LastUpdated);
                cmd.Parameters.AddWithValue("@IsActive", cart.IsActive);

                await cmd.ExecuteNonQueryAsync();
                _logger.LogInformation("Cart created successfully - ID: {CartId}", cart.CartId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating cart for user: {UserId}", cart.UserId);
                throw;
            }
        }

        public async Task AddToCartAsync(string userId, string productId, string productName, double unitPrice, int quantity)
        {
            _logger.LogInformation("AddToCartAsync - User: {UserId}, Product: {ProductId}, Quantity: {Quantity}",
                userId, productId, quantity);

            try
            {
                var cart = await GetOrCreateCartAsync(userId, "unknown");

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Check if item already exists
                var checkCmd = new SqlCommand(@"
                    SELECT CartItemId, Quantity FROM CartItem 
                    WHERE CartId = @CartId AND ProductId = @ProductId", connection);
                checkCmd.Parameters.AddWithValue("@CartId", cart.CartId);
                checkCmd.Parameters.AddWithValue("@ProductId", productId);

                using var reader = await checkCmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    // Update existing item
                    var existingQuantity = reader.GetInt32("Quantity");
                    var cartItemId = reader.GetGuid("CartItemId");
                    await reader.CloseAsync();

                    _logger.LogInformation("Updating existing item - Current quantity: {ExistingQuantity}, Adding: {Quantity}",
                        existingQuantity, quantity);

                    var updateCmd = new SqlCommand(@"
                        UPDATE CartItem 
                        SET Quantity = @Quantity 
                        WHERE CartItemId = @CartItemId", connection);
                    updateCmd.Parameters.AddWithValue("@Quantity", existingQuantity + quantity);
                    updateCmd.Parameters.AddWithValue("@CartItemId", cartItemId);
                    await updateCmd.ExecuteNonQueryAsync();
                }
                else
                {
                    await reader.CloseAsync();
                    // Add new item
                    _logger.LogInformation("Adding new item to cart");

                    var insertCmd = new SqlCommand(@"
                        INSERT INTO CartItem (CartItemId, CartId, ProductId, ProductName, UnitPrice, Quantity, CreatedDate)
                        VALUES (@CartItemId, @CartId, @ProductId, @ProductName, @UnitPrice, @Quantity, @CreatedDate)", connection);

                    insertCmd.Parameters.AddWithValue("@CartItemId", Guid.NewGuid());
                    insertCmd.Parameters.AddWithValue("@CartId", cart.CartId);
                    insertCmd.Parameters.AddWithValue("@ProductId", productId);
                    insertCmd.Parameters.AddWithValue("@ProductName", productName);
                    insertCmd.Parameters.AddWithValue("@UnitPrice", (decimal)unitPrice);
                    insertCmd.Parameters.AddWithValue("@Quantity", quantity);
                    insertCmd.Parameters.AddWithValue("@CreatedDate", DateTime.UtcNow);

                    await insertCmd.ExecuteNonQueryAsync();
                }

                // Update cart last updated
                await UpdateCartLastUpdated(cart.CartId);
                _logger.LogInformation("AddToCartAsync completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to cart - User: {UserId}, Product: {ProductId}", userId, productId);
                throw;
            }
        }

        public async Task UpdateCartItemQuantityAsync(string userId, string productId, int quantity)
        {
            _logger.LogInformation("UpdateCartItemQuantityAsync - User: {UserId}, Product: {ProductId}, Quantity: {Quantity}",
                userId, productId, quantity);

            var cart = await GetCartAsync(userId);
            if (cart == null)
            {
                _logger.LogWarning("Cart not found for user: {UserId}", userId);
                return;
            }

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            if (quantity <= 0)
            {
                await RemoveFromCartAsync(userId, productId);
                return;
            }

            var cmd = new SqlCommand(@"
                UPDATE CartItem 
                SET Quantity = @Quantity 
                WHERE CartId = @CartId AND ProductId = @ProductId", connection);
            cmd.Parameters.AddWithValue("@Quantity", quantity);
            cmd.Parameters.AddWithValue("@CartId", cart.CartId);
            cmd.Parameters.AddWithValue("@ProductId", productId);

            await cmd.ExecuteNonQueryAsync();
            await UpdateCartLastUpdated(cart.CartId);
            _logger.LogInformation("Cart item quantity updated successfully");
        }

        public async Task RemoveFromCartAsync(string userId, string productId)
        {
            _logger.LogInformation("RemoveFromCartAsync - User: {UserId}, Product: {ProductId}", userId, productId);

            var cart = await GetCartAsync(userId);
            if (cart == null)
            {
                _logger.LogWarning("Cart not found for user: {UserId}", userId);
                return;
            }

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = new SqlCommand(@"
                DELETE FROM CartItem 
                WHERE CartId = @CartId AND ProductId = @ProductId", connection);
            cmd.Parameters.AddWithValue("@CartId", cart.CartId);
            cmd.Parameters.AddWithValue("@ProductId", productId);

            await cmd.ExecuteNonQueryAsync();
            await UpdateCartLastUpdated(cart.CartId);
            _logger.LogInformation("Cart item removed successfully");
        }

        public async Task ClearCartAsync(string userId)
        {
            _logger.LogInformation("ClearCartAsync - User: {UserId}", userId);

            var cart = await GetCartAsync(userId);
            if (cart == null)
            {
                _logger.LogWarning("Cart not found for user: {UserId}", userId);
                return;
            }

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = new SqlCommand(@"
                DELETE FROM CartItem 
                WHERE CartId = @CartId", connection);
            cmd.Parameters.AddWithValue("@CartId", cart.CartId);

            await cmd.ExecuteNonQueryAsync();
            await UpdateCartLastUpdated(cart.CartId);
            _logger.LogInformation("Cart cleared successfully");
        }

        public async Task<int> GetCartItemCountAsync(string userId)
        {
            var cart = await GetCartAsync(userId);
            if (cart == null) return 0;

            return cart.Items.Sum(item => item.Quantity);
        }

        private async Task UpdateCartLastUpdated(Guid cartId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = new SqlCommand(@"
                UPDATE Cart 
                SET LastUpdated = @LastUpdated 
                WHERE CartId = @CartId", connection);
            cmd.Parameters.AddWithValue("@LastUpdated", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@CartId", cartId);

            await cmd.ExecuteNonQueryAsync();
        }
    }
}