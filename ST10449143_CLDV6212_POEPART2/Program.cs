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

// Use the typed client (replaces IAzureStorageService)
builder.Services.AddScoped<IFunctionsApi, FunctionsApiClient>();

// Allow larger multipart uploads
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50 MB
});

builder.Services.AddLogging();

var app = builder.Build();

// Culture for decimal handling
var culture = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();