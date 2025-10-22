using Microsoft.EntityFrameworkCore;
using CadenceAccounting.Data;
using CadenceAccounting.Models;
using BCrypt.Net;

namespace CadenceAccounting.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(ApplicationDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<User?> GetUserByIdAsync(Guid id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> ValidateUserAsync(string username, string password)
        {
            try
            {
                var user = await GetUserByUsernameAsync(username);
                if (user == null) return false;

                return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user: {Username}", username);
                return false;
            }
        }

        public async Task<User> CreateUserAsync(User user)
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {Username} created successfully", user.Username);
            return user;
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            user.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {Username} updated successfully", user.Username);
            return user;
        }

        public async Task<bool> DeleteUserAsync(Guid id)
        {
            var user = await GetUserByIdAsync(id);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {Username} deleted successfully", user.Username);
            return true;
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null) return false;

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Password changed for user {Username}", user.Username);
            return true;
        }
    }

    public class ProjectService : IProjectService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProjectService> _logger;

        public ProjectService(ApplicationDbContext context, ILogger<ProjectService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Project>> GetAllProjectsAsync()
        {
            return await _context.Projects
                .Include(p => p.Company)
                .Include(p => p.CreatedByUser)
                .Include(p => p.TimeEntries)
                .Include(p => p.Expenses)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Project?> GetProjectByIdAsync(Guid id)
        {
            return await _context.Projects
                .Include(p => p.Company)
                .Include(p => p.CreatedByUser)
                .Include(p => p.TimeEntries)
                .Include(p => p.Expenses)
                .Include(p => p.Invoices)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Project> CreateProjectAsync(Project project)
        {
            project.CreatedAt = DateTime.UtcNow;
            project.UpdatedAt = DateTime.UtcNow;

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Project {ProjectName} created successfully", project.Name);
            return project;
        }

        public async Task<Project> UpdateProjectAsync(Project project)
        {
            project.UpdatedAt = DateTime.UtcNow;
            _context.Projects.Update(project);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Project {ProjectName} updated successfully", project.Name);
            return project;
        }

        public async Task<bool> DeleteProjectAsync(Guid id)
        {
            var project = await GetProjectByIdAsync(id);
            if (project == null) return false;

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Project {ProjectName} deleted successfully", project.Name);
            return true;
        }

        public async Task<IEnumerable<Project>> GetProjectsByStatusAsync(string status)
        {
            return await _context.Projects
                .Include(p => p.Company)
                .Where(p => p.Status == status)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<decimal> GetProjectBudgetUtilizationAsync(Guid projectId)
        {
            var project = await GetProjectByIdAsync(projectId);
            if (project == null || project.Budget == 0) return 0;

            return (project.TotalAmount / project.Budget) * 100;
        }

        public async Task<bool> IsProjectOverBudgetAsync(Guid projectId)
        {
            var project = await GetProjectByIdAsync(projectId);
            if (project == null) return false;

            return project.TotalAmount > project.Budget;
        }
    }

    public class TimeEntryService : ITimeEntryService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TimeEntryService> _logger;

        public TimeEntryService(ApplicationDbContext context, ILogger<TimeEntryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<TimeEntry>> GetTimeEntriesByProjectAsync(Guid projectId)
        {
            return await _context.TimeEntries
                .Include(t => t.Project)
                .Include(t => t.User)
                .Where(t => t.ProjectId == projectId)
                .OrderByDescending(t => t.Date)
                .ToListAsync();
        }

        public async Task<IEnumerable<TimeEntry>> GetTimeEntriesByUserAsync(Guid userId)
        {
            return await _context.TimeEntries
                .Include(t => t.Project)
                .Include(t => t.User)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Date)
                .ToListAsync();
        }

        public async Task<IEnumerable<TimeEntry>> GetTimeEntriesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.TimeEntries
                .Include(t => t.Project)
                .Include(t => t.User)
                .Where(t => t.Date >= startDate && t.Date <= endDate)
                .OrderByDescending(t => t.Date)
                .ToListAsync();
        }

        public async Task<TimeEntry?> GetTimeEntryByIdAsync(Guid id)
        {
            return await _context.TimeEntries
                .Include(t => t.Project)
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<TimeEntry> CreateTimeEntryAsync(TimeEntry timeEntry)
        {
            timeEntry.Amount = timeEntry.Hours * timeEntry.Rate;
            timeEntry.CreatedAt = DateTime.UtcNow;
            timeEntry.UpdatedAt = DateTime.UtcNow;

            _context.TimeEntries.Add(timeEntry);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Time entry created for project {ProjectId}", timeEntry.ProjectId);
            return timeEntry;
        }

        public async Task<TimeEntry> UpdateTimeEntryAsync(TimeEntry timeEntry)
        {
            timeEntry.Amount = timeEntry.Hours * timeEntry.Rate;
            timeEntry.UpdatedAt = DateTime.UtcNow;

            _context.TimeEntries.Update(timeEntry);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Time entry {Id} updated successfully", timeEntry.Id);
            return timeEntry;
        }

        public async Task<bool> DeleteTimeEntryAsync(Guid id)
        {
            var timeEntry = await GetTimeEntryByIdAsync(id);
            if (timeEntry == null) return false;

            _context.TimeEntries.Remove(timeEntry);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Time entry {Id} deleted successfully", id);
            return true;
        }

        public async Task<decimal> GetTotalHoursByProjectAsync(Guid projectId)
        {
            return await _context.TimeEntries
                .Where(t => t.ProjectId == projectId && t.IsBillable)
                .SumAsync(t => t.Hours);
        }

        public async Task<decimal> GetTotalAmountByProjectAsync(Guid projectId)
        {
            return await _context.TimeEntries
                .Where(t => t.ProjectId == projectId && t.IsBillable)
                .SumAsync(t => t.Amount);
        }
    }

    public class ExpenseService : IExpenseService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ExpenseService> _logger;

        public ExpenseService(ApplicationDbContext context, ILogger<ExpenseService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Expense>> GetExpensesByProjectAsync(Guid projectId)
        {
            return await _context.Expenses
                .Include(e => e.Project)
                .Include(e => e.User)
                .Where(e => e.ProjectId == projectId)
                .OrderByDescending(e => e.Date)
                .ToListAsync();
        }

        public async Task<IEnumerable<Expense>> GetExpensesByUserAsync(Guid userId)
        {
            return await _context.Expenses
                .Include(e => e.Project)
                .Include(e => e.User)
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.Date)
                .ToListAsync();
        }

        public async Task<IEnumerable<Expense>> GetExpensesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Expenses
                .Include(e => e.Project)
                .Include(e => e.User)
                .Where(e => e.Date >= startDate && e.Date <= endDate)
                .OrderByDescending(e => e.Date)
                .ToListAsync();
        }

        public async Task<Expense?> GetExpenseByIdAsync(Guid id)
        {
            return await _context.Expenses
                .Include(e => e.Project)
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<Expense> CreateExpenseAsync(Expense expense)
        {
            expense.CreatedAt = DateTime.UtcNow;
            expense.UpdatedAt = DateTime.UtcNow;

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Expense created for project {ProjectId}", expense.ProjectId);
            return expense;
        }

        public async Task<Expense> UpdateExpenseAsync(Expense expense)
        {
            expense.UpdatedAt = DateTime.UtcNow;

            _context.Expenses.Update(expense);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Expense {Id} updated successfully", expense.Id);
            return expense;
        }

        public async Task<bool> DeleteExpenseAsync(Guid id)
        {
            var expense = await GetExpenseByIdAsync(id);
            if (expense == null) return false;

            _context.Expenses.Remove(expense);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Expense {Id} deleted successfully", id);
            return true;
        }

        public async Task<decimal> GetTotalAmountByProjectAsync(Guid projectId)
        {
            return await _context.Expenses
                .Where(e => e.ProjectId == projectId && e.IsBillable)
                .SumAsync(e => e.Amount);
        }

        public async Task<IEnumerable<string>> GetExpenseCategoriesAsync()
        {
            return await _context.Expenses
                .Select(e => e.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }
    }
}
