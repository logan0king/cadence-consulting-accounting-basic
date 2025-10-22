using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CadenceAccounting.Services;
using CadenceAccounting.Models;
using CadenceAccounting.Data;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace CadenceAccounting.Pages.Account
{
    [IgnoreAntiforgeryToken]
    public class LoginModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(IUserService userService, ILogger<LoginModel> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [BindProperty]
        public LoginInputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public class LoginInputModel
        {
            [Required]
            [Display(Name = "Username")]
            public string Username { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Remember me")]
            public bool RememberMe { get; set; }
        }

        public IActionResult OnGet(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToPage("/Dashboard/Index");
            }

            ReturnUrl = returnUrl;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;

            _logger.LogInformation("Login POST received. ModelState.IsValid: {IsValid}", ModelState.IsValid);
            _logger.LogInformation("Request Content-Type: {ContentType}", Request.ContentType);
            _logger.LogInformation("Request Method: {Method}", Request.Method);
            _logger.LogInformation("Form data: Username={Username}, Password={Password}, RememberMe={RememberMe}", 
                Request.Form["Input.Username"], 
                Request.Form["Input.Password"], 
                Request.Form["Input.RememberMe"]);
            
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid. Errors: {Errors}", 
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return Page();
            }

            try
            {
                _logger.LogInformation("Attempting login for user: {Username}", Input.Username);
                
                var isValid = await _userService.ValidateUserAsync(Input.Username, Input.Password);
                _logger.LogInformation("Password validation result: {IsValid}", isValid);
                
                if (!isValid)
                {
                    _logger.LogWarning("Login failed for user: {Username} - Invalid credentials", Input.Username);
                    ModelState.AddModelError(string.Empty, "Invalid username or password.");
                    return Page();
                }

                var user = await _userService.GetUserByUsernameAsync(Input.Username);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "User not found.");
                    return Page();
                }

                if (!user.IsActive)
                {
                    ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact an administrator.");
                    return Page();
                }

                // Skip last login update for now due to database trigger conflicts
                // TODO: Fix database trigger issue
                _logger.LogInformation("Skipping last login update due to database trigger conflicts");

                // Create claims
                _logger.LogInformation("Creating claims for user: {Username}", user.Username);
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new(ClaimTypes.Name, user.Username),
                    new(ClaimTypes.Email, user.Email),
                    new(ClaimTypes.GivenName, user.FirstName),
                    new(ClaimTypes.Surname, user.LastName),
                    new(ClaimTypes.Role, user.Role)
                };
                _logger.LogInformation("Claims created successfully");

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = Input.RememberMe,
                    ExpiresUtc = Input.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddMinutes(30)
                };
                _logger.LogInformation("Authentication properties created");

                _logger.LogInformation("Creating authentication cookie for user: {Username}", user.Username);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, 
                    new ClaimsPrincipal(claimsIdentity), authProperties);
                _logger.LogInformation("Authentication cookie created successfully");

                _logger.LogInformation("User {Username} logged in successfully", user.Username);

                _logger.LogInformation("Attempting redirect to dashboard");
                
                if (!string.IsNullOrEmpty(ReturnUrl))
                {
                    _logger.LogInformation("Redirecting to return URL: {ReturnUrl}", ReturnUrl);
                    return LocalRedirect(ReturnUrl);
                }
                
                _logger.LogInformation("Redirecting to dashboard page");
                return RedirectToPage("/Dashboard/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user {Username}", Input.Username);
                
                // Check if it's a database connection error
                if (ex.Message.Contains("Instance failure") || ex.Message.Contains("connection") || ex.Message.Contains("database"))
                {
                    ModelState.AddModelError(string.Empty, "Database connection error. Please check if the database server is running and accessible. Contact your administrator if the problem persists.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
                }
                
                return Page();
            }
        }
    }

    public class LogoutModel : PageModel
    {
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(ILogger<LogoutModel> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var username = User.Identity?.Name;
            
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            if (!string.IsNullOrEmpty(username))
            {
                _logger.LogInformation("User {Username} logged out", username);
            }

            return RedirectToPage("/Account/Login");
        }
    }

    public class AccessDeniedModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
