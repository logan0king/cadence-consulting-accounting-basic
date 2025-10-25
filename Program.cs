using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using CadenceAccounting.Data;
using CadenceAccounting.Services;
using CadenceAccounting.Models;
using BCrypt.Net;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure to use HTTP only (no HTTPS)
builder.WebHost.UseUrls("http://localhost:5001");

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

// Add anti-forgery token validation
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.SuppressXFrameOptionsHeader = false;
});

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
builder.Services.AddScoped<IReportDesignerService, ReportDesignerService>();
builder.Services.AddScoped<IReportExportService, ReportExportService>();

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
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.Path = "/";
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

// Add anti-forgery token validation
app.UseAntiforgery();

app.UseSession();

app.MapRazorPages();

// Add a fallback route to test connection page
app.MapFallback("/", (HttpContext context) =>
{
    // Redirect to test connection page to help diagnose database issues
    context.Response.Redirect("/TestConnection");
});


// Database initialization with improved error handling
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        // Test database connection first
        logger.LogInformation("Testing database connection...");
        await context.Database.OpenConnectionAsync();
        await context.Database.CloseConnectionAsync();
        logger.LogInformation("Database connection successful");
        
        // Ensure database and tables are created
        logger.LogInformation("Ensuring database is created...");
        await context.Database.EnsureCreatedAsync();
        logger.LogInformation("Database creation completed");
        
        // Create or update default admin user
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var existingUsers = await userService.GetAllUsersAsync();
        var adminUser = existingUsers.FirstOrDefault(u => u.Username == "admin");
        
        if (adminUser == null)
        {
            logger.LogInformation("No admin user found, creating default admin user...");
            var defaultAdmin = new User
            {
                Id = Guid.NewGuid(),
                Username = "admin",
                Email = "admin@cadenceconsulting.com",
                FirstName = "System",
                LastName = "Administrator",
                Role = "Administrator",
                IsActive = true,
                EmailVerified = true,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            await userService.CreateUserAsync(defaultAdmin);
            logger.LogInformation("Default admin user created - Username: admin, Password: Admin123!");
        }
               else
               {
                   logger.LogInformation("Admin user found, updating password...");
                   // Use raw SQL to avoid trigger conflicts
                   var newPasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!");
                   await context.Database.ExecuteSqlRawAsync(
                       "UPDATE Users SET PasswordHash = {0}, IsActive = 1, EmailVerified = 1, UpdatedAt = {1} WHERE Username = 'admin'",
                       newPasswordHash, DateTime.UtcNow);
                   logger.LogInformation("Admin user password updated - Username: admin, Password: Admin123!");
               }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to connect to database. Please check the connection string in appsettings.json and ensure the SQL Server is running and accessible.");
        logger.LogError("Connection string: {ConnectionString}", builder.Configuration.GetConnectionString("DefaultConnection"));
        // Continue running - user can configure database via setup page
    }
}

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
