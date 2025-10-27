using Microsoft.AspNetCore.Mvc;
using CadenceAccounting.Data;
using CadenceAccounting.Models;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace CadenceAccounting.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportWizardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReportWizardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateReport([FromBody] WizardData data)
        {
            try
            {
                Console.WriteLine($"CreateReport called. Data: {JsonSerializer.Serialize(data)}");
                
                if (data == null)
                {
                    return BadRequest(new { success = false, error = "No data provided" });
                }

                // Get the first admin user
                var userId = _context.Users
                    .Where(u => u.Username == "admin")
                    .Select(u => u.Id)
                    .FirstOrDefault();
                
                Console.WriteLine($"Current user ID: {userId}");

                if (userId == Guid.Empty)
                {
                    return BadRequest(new { success = false, error = "Admin user not found" });
                }

                var report = new ReportDefinition
                {
                    Id = Guid.NewGuid(),
                    Title = data.Title,
                    Description = data.Description ?? string.Empty,
                    ReportGroup = data.Group ?? "General",
                    CreatedBy = userId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _context.ReportDefinitions.AddAsync(report);
                await _context.SaveChangesAsync();

                Console.WriteLine($"Report created successfully. ID: {report.Id}");

                // Create components based on wizard data
                var components = new List<ReportComponent>();
                
                // Add title component
                var titleComponent = new ReportComponent
                {
                    Id = Guid.NewGuid(),
                    ReportId = report.Id,
                    ComponentType = "label",
                    PositionX = 50,
                    PositionY = 50,
                    Width = 400,
                    Height = 30,
                    Properties = JsonSerializer.Serialize(new
                    {
                        Text = data.Title,
                        FontSize = 18,
                        FontWeight = "bold",
                        TextAlign = "left"
                    }),
                    ZIndex = 1,
                    IsVisible = true,
                    CreatedAt = DateTime.UtcNow
                };
                components.Add(titleComponent);

                // Add table component for the selected data source
                if (!string.IsNullOrEmpty(data.DataSource) && data.Fields != null && data.Fields.Count > 0)
                {
                    var tableComponent = new ReportComponent
                    {
                        Id = Guid.NewGuid(),
                        ReportId = report.Id,
                        ComponentType = "table",
                        PositionX = 50,
                        PositionY = 100,
                        Width = 700,
                        Height = 200,
                        Properties = JsonSerializer.Serialize(new
                        {
                            DataSource = data.DataSource,
                            Fields = data.Fields,
                            ShowHeader = true,
                            AlternatingRowColor = "#f5f5f5"
                        }),
                        DataBinding = JsonSerializer.Serialize(new
                        {
                            DataSource = data.DataSource,
                            Fields = data.Fields
                        }),
                        ZIndex = 0,
                        IsVisible = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    components.Add(tableComponent);
                }

                // Save components
                if (components.Any())
                {
                    await _context.ReportComponents.AddRangeAsync(components);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"Created {components.Count} components");
                }

                return Ok(new { success = true, reportId = report.Id });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateReport: {ex.Message}\n{ex.StackTrace}");
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        public class WizardData
        {
            [JsonPropertyName("template")]
            public string Template { get; set; } = string.Empty;
            
            [JsonPropertyName("dataSource")]
            public string DataSource { get; set; } = string.Empty;
            
            [JsonPropertyName("fields")]
            public List<string> Fields { get; set; } = new();
            
            [JsonPropertyName("title")]
            public string Title { get; set; } = string.Empty;
            
            [JsonPropertyName("description")]
            public string Description { get; set; } = string.Empty;
            
            [JsonPropertyName("group")]
            public string Group { get; set; } = string.Empty;
        }
    }
}

