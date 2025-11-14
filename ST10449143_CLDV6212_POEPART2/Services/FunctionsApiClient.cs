using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ST10449143_CLDV6212_POEPART1.Models;
using Microsoft.Extensions.Logging;

namespace ST10449143_CLDV6212_POEPART1.Services
{
    public class FunctionsApiClient : IFunctionsApi
    {
        private readonly HttpClient _http;
        private readonly ILogger<FunctionsApiClient> _logger;
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

        private const string CustomersRoute = "customers";
        private const string ProductsRoute = "products";
        private const string OrdersRoute = "orders";
        private const string UploadsRoute = "uploads/proof-of-payment";

        public FunctionsApiClient(IHttpClientFactory factory, ILogger<FunctionsApiClient> logger)
        {
            _http = factory.CreateClient("Functions");
            _logger = logger;
            _logger.LogInformation("FunctionsApiClient initialized with base address: {BaseAddress}", _http.BaseAddress);
        }

        private static HttpContent JsonBody(object obj)
            => new StringContent(JsonSerializer.Serialize(obj, _json), Encoding.UTF8, "application/json");

        private async Task<T> ReadJsonAsync<T>(HttpResponseMessage resp)
        {
            try
            {
                resp.EnsureSuccessStatusCode();
                var stream = await resp.Content.ReadAsStreamAsync();
                var data = await JsonSerializer.DeserializeAsync<T>(stream, _json);

                if (data == null)
                {
                    _logger.LogWarning("Deserialized JSON data is null for type {Type}", typeof(T).Name);
                    throw new Exception($"Deserialized data is null for type {typeof(T).Name}");
                }

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading JSON response for type {Type}", typeof(T).Name);
                throw;
            }
        }

        // Customers
        public async Task<List<Customer>> GetCustomersAsync()
        {
            _logger.LogInformation("Getting customers from API");
            try
            {
                var response = await _http.GetAsync(CustomersRoute);
                var customers = await ReadJsonAsync<List<Customer>>(response);
                _logger.LogInformation("Retrieved {Count} customers", customers.Count);
                return customers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customers");
                throw;
            }
        }

        public async Task<Customer?> GetCustomerAsync(string id)
        {
            _logger.LogInformation("Getting customer with ID: {CustomerId}", id);
            try
            {
                var resp = await _http.GetAsync($"{CustomersRoute}/{id}");
                if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Customer not found with ID: {CustomerId}", id);
                    return null;
                }
                return await ReadJsonAsync<Customer>(resp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer with ID: {CustomerId}", id);
                throw;
            }
        }

        public async Task<Customer> CreateCustomerAsync(Customer c)
        {
            _logger.LogInformation("Creating customer: {CustomerName}", $"{c.Name} {c.Surname}");
            try
            {
                var response = await _http.PostAsync(CustomersRoute, JsonBody(new
                {
                    name = c.Name,
                    surname = c.Surname,
                    username = c.Username,
                    email = c.Email,
                    shippingAddress = c.ShippingAddress
                }));
                return await ReadJsonAsync<Customer>(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer: {CustomerName}", $"{c.Name} {c.Surname}");
                throw;
            }
        }

        public async Task<Customer> UpdateCustomerAsync(string id, Customer c)
        {
            _logger.LogInformation("Updating customer with ID: {CustomerId}", id);
            try
            {
                var response = await _http.PutAsync($"{CustomersRoute}/{id}", JsonBody(new
                {
                    name = c.Name,
                    surname = c.Surname,
                    username = c.Username,
                    email = c.Email,
                    shippingAddress = c.ShippingAddress
                }));
                return await ReadJsonAsync<Customer>(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer with ID: {CustomerId}", id);
                throw;
            }
        }

        public async Task DeleteCustomerAsync(string id)
        {
            _logger.LogInformation("Deleting customer with ID: {CustomerId}", id);
            try
            {
                var response = await _http.DeleteAsync($"{CustomersRoute}/{id}");
                response.EnsureSuccessStatusCode();
                _logger.LogInformation("Customer deleted successfully: {CustomerId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer with ID: {CustomerId}", id);
                throw;
            }
        }

        // Products
        public async Task<List<Product>> GetProductsAsync()
        {
            _logger.LogInformation("Getting products from API");
            try
            {
                var response = await _http.GetAsync(ProductsRoute);
                var products = await ReadJsonAsync<List<Product>>(response);
                _logger.LogInformation("Retrieved {Count} products", products.Count);
                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products");
                throw;
            }
        }

        public async Task<Product?> GetProductAsync(string id)
        {
            _logger.LogInformation("Getting product with ID: {ProductId}", id);
            try
            {
                var resp = await _http.GetAsync($"{ProductsRoute}/{id}");
                if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Product not found with ID: {ProductId}", id);
                    return null;
                }
                var product = await ReadJsonAsync<Product>(resp);
                _logger.LogInformation("Retrieved product: {ProductName}, Stock: {Stock}", product.ProductName, product.StockAvailable);
                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product with ID: {ProductId}", id);
                throw;
            }
        }

        public async Task<Product> CreateProductAsync(Product p, IFormFile? imageFile)
        {
            _logger.LogInformation("Creating product: {ProductName}", p.ProductName);
            try
            {
                using var form = new MultipartFormDataContent();
                form.Add(new StringContent(p.ProductName), "ProductName");
                form.Add(new StringContent(p.Description ?? string.Empty), "Description");
                form.Add(new StringContent(p.Price.ToString(System.Globalization.CultureInfo.InvariantCulture)), "Price");
                form.Add(new StringContent(p.StockAvailable.ToString(System.Globalization.CultureInfo.InvariantCulture)), "StockAvailable");
                if (!string.IsNullOrWhiteSpace(p.ImageUrl)) form.Add(new StringContent(p.ImageUrl), "ImageUrl");
                if (imageFile is not null && imageFile.Length > 0)
                {
                    var file = new StreamContent(imageFile.OpenReadStream());
                    file.Headers.ContentType = new MediaTypeHeaderValue(imageFile.ContentType ?? "application/octet-stream");
                    form.Add(file, "ImageFile", imageFile.FileName);
                    _logger.LogInformation("Including image file: {FileName}", imageFile.FileName);
                }
                var response = await _http.PostAsync(ProductsRoute, form);
                return await ReadJsonAsync<Product>(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product: {ProductName}", p.ProductName);
                throw;
            }
        }

        public async Task<Product> UpdateProductAsync(string id, Product p, IFormFile? imageFile)
        {
            _logger.LogInformation("Updating product with ID: {ProductId}", id);
            try
            {
                using var form = new MultipartFormDataContent();
                form.Add(new StringContent(p.ProductName), "ProductName");
                form.Add(new StringContent(p.Description ?? string.Empty), "Description");
                form.Add(new StringContent(p.Price.ToString(System.Globalization.CultureInfo.InvariantCulture)), "Price");
                form.Add(new StringContent(p.StockAvailable.ToString(System.Globalization.CultureInfo.InvariantCulture)), "StockAvailable");
                if (!string.IsNullOrWhiteSpace(p.ImageUrl)) form.Add(new StringContent(p.ImageUrl), "ImageUrl");
                if (imageFile is not null && imageFile.Length > 0)
                {
                    var file = new StreamContent(imageFile.OpenReadStream());
                    file.Headers.ContentType = new MediaTypeHeaderValue(imageFile.ContentType ?? "application/octet-stream");
                    form.Add(file, "ImageFile", imageFile.FileName);
                }
                var response = await _http.PutAsync($"{ProductsRoute}/{id}", form);
                return await ReadJsonAsync<Product>(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product with ID: {ProductId}", id);
                throw;
            }
        }

        public async Task DeleteProductAsync(string id)
        {
            _logger.LogInformation("Deleting product with ID: {ProductId}", id);
            try
            {
                var response = await _http.DeleteAsync($"{ProductsRoute}/{id}");
                response.EnsureSuccessStatusCode();
                _logger.LogInformation("Product deleted successfully: {ProductId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product with ID: {ProductId}", id);
                throw;
            }
        }

        // Orders
        public async Task<List<Order>> GetOrdersAsync()
        {
            _logger.LogInformation("Getting orders from API");
            try
            {
                var response = await _http.GetAsync(OrdersRoute);
                var orders = await ReadJsonAsync<List<Order>>(response);
                _logger.LogInformation("Retrieved {Count} orders", orders.Count);
                return orders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders");
                throw;
            }
        }

        public async Task<Order?> GetOrderAsync(string id)
        {
            _logger.LogInformation("Getting order with ID: {OrderId}", id);
            try
            {
                var resp = await _http.GetAsync($"{OrdersRoute}/{id}");
                if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Order not found with ID: {OrderId}", id);
                    return null;
                }
                return await ReadJsonAsync<Order>(resp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order with ID: {OrderId}", id);
                throw;
            }
        }

        // Enhanced CreateOrderAsync method with detailed logging
        public async Task<Order> CreateOrderAsync(string customerId, string productId, int quantity)
        {
            _logger.LogInformation("Creating order - Customer: {CustomerId}, Product: {ProductId}, Quantity: {Quantity}",
                customerId, productId, quantity);

            try
            {
                var payload = new { customerId, productId, quantity };
                var jsonContent = JsonBody(payload);

                _logger.LogInformation("Sending order creation request to: {OrdersRoute}", OrdersRoute);
                _logger.LogInformation("Request payload: {Payload}", JsonSerializer.Serialize(payload));

                var response = await _http.PostAsync(OrdersRoute, jsonContent);

                _logger.LogInformation("Order API response status: {StatusCode}", response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Order creation failed with status {StatusCode}: {ErrorContent}",
                        response.StatusCode, errorContent);

                    throw new HttpRequestException($"Order creation failed with status {response.StatusCode}: {errorContent}");
                }

                var order = await ReadJsonAsync<Order>(response);

                if (order == null)
                {
                    _logger.LogError("Order creation returned null order object");
                    throw new Exception("Order creation returned null response");
                }

                if (string.IsNullOrEmpty(order.Id))
                {
                    _logger.LogError(" Order creation returned order with empty ID");
                    throw new Exception("Order creation returned order with empty ID");
                }

                _logger.LogInformation(" Order created successfully - Order ID: {OrderId}, Product: {ProductId}, Quantity: {Quantity}",
                    order.Id, productId, quantity);

                return order;
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, " HTTP error creating order for product {ProductId}", productId);
                throw new Exception($"Network error creating order: {httpEx.Message}", httpEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Failed to create order for product {ProductId}", productId);
                throw new Exception($"Failed to create order for product {productId}: {ex.Message}", ex);
            }
        }

        public async Task UpdateOrderStatusAsync(string id, string newStatus)
        {
            _logger.LogInformation("Updating order status - Order: {OrderId}, New Status: {NewStatus}", id, newStatus);
            try
            {
                var payload = new { status = newStatus };

                
                var endpointsToTry = new[]
                {
            $"{OrdersRoute}/{id}/status",  
            $"{OrdersRoute}/{id}",         
            $"update-order-status/{id}"    
        };

                Exception lastException = null;

                foreach (var endpoint in endpointsToTry)
                {
                    try
                    {
                        _logger.LogInformation("Trying to update order status via endpoint: {Endpoint}", endpoint);

                        var response = await _http.PatchAsync(endpoint, JsonBody(payload));

                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogInformation("Order status updated successfully via {Endpoint}: {OrderId} -> {NewStatus}",
                                endpoint, id, newStatus);
                            return; 
                        }

                        
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogWarning("Endpoint {Endpoint} failed with status {StatusCode}: {Error}",
                            endpoint, response.StatusCode, errorContent);
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        _logger.LogWarning("Endpoint {Endpoint} failed: {Message}", endpoint, ex.Message);
                        
                    }
                }

                
                throw lastException ?? new Exception("All order status update endpoints failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for order: {OrderId}", id);
                throw new Exception($"Failed to update order status for order {id}: {ex.Message}", ex);
            }
        }

        public async Task DeleteOrderAsync(string id)
        {
            _logger.LogInformation("Deleting order with ID: {OrderId}", id);
            try
            {
                var response = await _http.DeleteAsync($"{OrdersRoute}/{id}");
                response.EnsureSuccessStatusCode();
                _logger.LogInformation("Order deleted successfully: {OrderId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order with ID: {OrderId}", id);
                throw;
            }
        }

        // Uploads
        public async Task<string> UploadProofOfPaymentAsync(IFormFile file, string? orderId, string? customerName)
        {
            _logger.LogInformation("Uploading proof of payment - File: {FileName}, Size: {FileSize} bytes",
                file.FileName, file.Length);
            try
            {
                using var form = new MultipartFormDataContent();
                var sc = new StreamContent(file.OpenReadStream());
                sc.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
                form.Add(sc, "ProofOfPayment", file.FileName);
                if (!string.IsNullOrWhiteSpace(orderId)) form.Add(new StringContent(orderId), "OrderId");
                if (!string.IsNullOrWhiteSpace(customerName)) form.Add(new StringContent(customerName), "CustomerName");

                var resp = await _http.PostAsync(UploadsRoute, form);
                resp.EnsureSuccessStatusCode();

                var doc = await ReadJsonAsync<Dictionary<string, string>>(resp);
                var fileName = doc.TryGetValue("fileName", out var name) ? name : file.FileName;

                _logger.LogInformation("File uploaded successfully: {FileName}", fileName);
                return fileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading proof of payment: {FileName}", file.FileName);
                throw;
            }
        }       
        
    }

    // HttpClient PATCH extension
    internal static class HttpClientPatchExtensions
    {
        public static Task<HttpResponseMessage> PatchAsync(this HttpClient client, string requestUri, HttpContent content)
            => client.SendAsync(new HttpRequestMessage(HttpMethod.Patch, requestUri) { Content = content });
    }
}