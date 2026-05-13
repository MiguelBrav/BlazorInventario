using BlazorInventario.Components;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using BlazorInventario.Data;
using BlazorInventario.Repositories;
using BlazorInventario.Services;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost;Database=mininventary;User=root;Password=pass;";

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// HTTP client for calling sign-in/sign-out endpoints from components
builder.Services.AddHttpClient();

// Register server authentication state provider so Blazor interactive components
// can receive the authentication state via CascadingAuthenticationState.
builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

// DB connection factory and repositories/services
builder.Services.AddSingleton<IDbConnectionFactory>(new MySqlConnectionFactory(connectionString));
// User repository registration
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICategoriesRepository, CategoriesRepository>();
builder.Services.AddScoped<IProductsRepository, ProductsRepository>();
builder.Services.AddScoped<IMovementsRepository, MovementsRepository>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
// Allow services to access current HttpContext for server-side role checks
builder.Services.AddHttpContextAccessor();

// Cookie authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/signout";
    });

builder.Services.AddAuthorization();
// Provide the Task<AuthenticationState> cascading parameter for AuthorizeView and related components
builder.Services.AddCascadingAuthenticationState();

// Export service for movements CSV
builder.Services.AddScoped<BlazorInventario.Services.IMovementsExportService, BlazorInventario.Services.MovementsExportService>();
// Export services for products and categories
builder.Services.AddScoped<BlazorInventario.Services.IProductsExportService, BlazorInventario.Services.ProductsExportService>();
builder.Services.AddScoped<BlazorInventario.Services.ICategoriesExportService, BlazorInventario.Services.CategoriesExportService>();
// Users export
builder.Services.AddScoped<BlazorInventario.Services.IUsersExportService, BlazorInventario.Services.UsersExportService>();

// Application build
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

// Authentication / Authorization
app.UseAuthentication();
app.UseAuthorization();

// Sign-in endpoint 
app.MapPost("/signin", async (HttpContext http, IAuthService authService) =>
{
    var req = http.Request;
    var form = await req.ReadFormAsync();
    var username = form["username"].ToString();
    var password = form["password"].ToString();

    if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
    {
        return Results.Redirect("/login?failed=1");
    }

    var user = await authService.ValidateCredentialsAsync(username, password);
    if (user is null)
    {
        return Results.Redirect("/login?failed=1");
    }

    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.id.ToString()),
        new Claim(ClaimTypes.Name, user.name ?? user.email ?? string.Empty),
        new Claim(ClaimTypes.Email, user.email ?? string.Empty),
    };

    if (!string.IsNullOrEmpty(user.role))
    {
        claims = claims.Append(new Claim(ClaimTypes.Role, user.role)).ToArray();
    }

    var id = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(id);
    await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    return Results.Redirect("/");
});

app.MapGet("/signout", async (HttpContext http) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/");
});



app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
