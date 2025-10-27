using Microsoft.EntityFrameworkCore;
using CadenceAccounting.Data;
using CadenceAccounting.Models;
using System.Text.Json;

namespace CadenceAccounting.Services
{
    public class ReportParameterService : IReportParameterService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReportParameterService> _logger;

        public ReportParameterService(ApplicationDbContext context, ILogger<ReportParameterService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<ReportParameter>> GetParametersAsync(Guid reportId)
        {
            try
            {
                return await _context.ReportParameters
                    .Where(p => p.ReportId == reportId)
                    .Include(p => p.Options)
                    .OrderBy(p => p.SortOrder)
                    .ThenBy(p => p.ParameterName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting parameters for report {ReportId}", reportId);
                return new List<ReportParameter>();
            }
        }

        public async Task<ReportParameter?> GetParameterAsync(Guid parameterId)
        {
            try
            {
                return await _context.ReportParameters
                    .Include(p => p.Options)
                    .FirstOrDefaultAsync(p => p.Id == parameterId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting parameter {ParameterId}", parameterId);
                return null;
            }
        }

        public async Task<ReportParameter> SaveParameterAsync(ReportParameter parameter)
        {
            try
            {
                if (parameter.Id == Guid.Empty)
                {
                    parameter.Id = Guid.NewGuid();
                    parameter.CreatedAt = DateTime.UtcNow;
                    parameter.UpdatedAt = DateTime.UtcNow;
                    _context.ReportParameters.Add(parameter);
                }
                else
                {
                    parameter.UpdatedAt = DateTime.UtcNow;
                    _context.ReportParameters.Update(parameter);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Parameter {ParameterId} saved for report {ReportId}", parameter.Id, parameter.ReportId);
                
                return parameter;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving parameter for report {ReportId}", parameter.ReportId);
                throw;
            }
        }

        public async Task DeleteParameterAsync(Guid parameterId)
        {
            try
            {
                var parameter = await _context.ReportParameters.FindAsync(parameterId);
                if (parameter != null)
                {
                    _context.ReportParameters.Remove(parameter);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Parameter {ParameterId} deleted", parameterId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting parameter {ParameterId}", parameterId);
                throw;
            }
        }

        public async Task<Dictionary<string, object>> CollectParametersAsync(Guid reportId, HttpRequest request)
        {
            var collectedParams = new Dictionary<string, object>();
            
            try
            {
                var parameters = await GetParametersAsync(reportId);
                
                foreach (var param in parameters)
                {
                    var value = request.Query[param.ParameterName].ToString();
                    
                    if (string.IsNullOrEmpty(value) && param.Required)
                    {
                        throw new InvalidOperationException($"Required parameter '{param.ParameterName}' is missing");
                    }
                    
                    if (!string.IsNullOrEmpty(value))
                    {
                        collectedParams[param.ParameterName] = ConvertParameterValue(value, param.ParameterType);
                    }
                    else if (!string.IsNullOrEmpty(param.DefaultValue))
                    {
                        collectedParams[param.ParameterName] = ConvertParameterValue(param.DefaultValue, param.ParameterType);
                    }
                }
                
                _logger.LogInformation("Collected {Count} parameters for report {ReportId}", collectedParams.Count, reportId);
                return collectedParams;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting parameters for report {ReportId}", reportId);
                throw;
            }
        }

        public async Task<string> ApplyParametersToSqlAsync(Guid reportId, string sql, Dictionary<string, object> parameters)
        {
            try
            {
                foreach (var param in parameters)
                {
                    sql = sql.Replace($"@{param.Key}", FormatParameterValue(param.Value));
                }
                
                _logger.LogInformation("Applied {Count} parameters to SQL for report {ReportId}", parameters.Count, reportId);
                return sql;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying parameters to SQL for report {ReportId}", reportId);
                throw;
            }
        }

        private object ConvertParameterValue(string value, ParameterType type)
        {
            return type switch
            {
                ParameterType.Integer => int.Parse(value),
                ParameterType.Decimal => decimal.Parse(value),
                ParameterType.DateTime => DateTime.Parse(value),
                ParameterType.Boolean => bool.Parse(value),
                _ => value
            };
        }

        private string FormatParameterValue(object value)
        {
            return value switch
            {
                string s => $"'{s.Replace("'", "''")}'",
                DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
                bool b => b ? "1" : "0",
                _ => value.ToString() ?? string.Empty
            };
        }
    }
}

