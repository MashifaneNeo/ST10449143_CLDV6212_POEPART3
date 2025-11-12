using ST10449143_CLDV6212_POEPART1.Services;
using Microsoft.AspNetCore.Http.Features;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// Typed HttpClient for Azure Functions
builder.Services.AddHttpClient("Functions", (sp, client) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var baseUrl = cfg["Functions:BaseUrl"] ?? "st10449143-function-b8eke0ceh6aaf7c6.southafricanorth-01.azurewebsites.net";
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/api/");
    client.Timeout = TimeSpan.FromSeconds(100);
});

// Register services
builder.Services.AddScoped<IFunctionsApi, FunctionsApiClient>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Session configuration for authentication
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session timeout
    options.Cookie.HttpOnly = true; // Prevent XSS
    options.Cookie.IsEssential = true; // GDPR compliance
    options.Cookie.SameSite = SameSiteMode.Strict; // CSRF protection
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Require HTTPS in production
});

// Allow larger multipart uploads
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50 MB
});

// Logging
builder.Services.AddLogging();

var app = builder.Build();

// Culture for decimal handling
var culture = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Add session middleware - MUST be after UseRouting and before UseEndpoints
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();