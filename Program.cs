using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using CadenceAccounting.Data;
using CadenceAccounting.Services;
using CadenceAccounting.Models;
using BCrypt.Net;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure to use HTTP only (no HTTPS)
builder.WebHost.UseUrls("http://localhost:5000");

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/cadence-accounting-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddRazorPages();

// Add Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add custom services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITimeEntryService, TimeEntryService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuditService, AuditService>();

// Add authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.None; // Allow HTTP cookies
        options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax; // Allow cross-site requests
    });

// Add authorization
builder.Services.AddAuthorization();

// Add session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.None; // Allow HTTP cookies
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // app.UseHsts(); // Disabled to prevent HTTPS enforcement
}

// app.UseHttpsRedirection(); // Disabled for local development
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.MapRazorPages();

// Add a fallback route to setup page (database initialization disabled)
app.MapFallback("/", (HttpContext context) =>
{
    // Redirect to setup page since database initialization is disabled
    context.Response.Redirect("/Setup");
});


// Database initialization temporarily disabled to resolve 400 error
// TODO: Re-enable after fixing Entity Framework connection issues
/*
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();
        
        // Create default admin user if no users exist
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var existingUsers = await userService.GetAllUsersAsync();
        if (!existingUsers.Any())
        {
            var defaultAdmin = new User
            {
                Id = Guid.NewGuid(),
                Username = "admin",
                Email = "admin@cadenceconsulting.com",
                FirstName = "System",
                LastName = "Administrator",
                Role = "Administrator",
                IsActive = true,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            await userService.CreateUserAsync(defaultAdmin);
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Default admin user created - Username: admin, Password: Admin123!");
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to connect to database. Please configure the database connection in the Setup page.");
        // Continue running - user can configure database via setup page
    }
}
*/

try
{
    Log.Information("Starting Cadence Accounting Application");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
