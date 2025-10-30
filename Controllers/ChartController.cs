using System.Data;
using System.Data.Common;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using CadenceAccounting.Data;

namespace CadenceAccounting.Controllers
{
    [ApiController]
    [Route("api/chart")] // /api/chart/preview
    public class ChartController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<ChartController> _logger;

        public ChartController(ApplicationDbContext db, ILogger<ChartController> logger)
        {
            _db = db;
            _logger = logger;
        }

        public class ChartPreviewRequest
        {
            public Binding? DataBinding { get; set; }
            public Dictionary<string, string>? Params { get; set; }
            public int MaxRows { get; set; } = 1000;
        }

        public class Binding
        {
            public string? Type { get; set; } // sql|rest|csv (sql only for now)
            public string? Sql { get; set; }
        }

        [HttpPost("preview")]
        public async Task<IActionResult> Preview([FromBody] ChartPreviewRequest request)
        {
            var startedAt = DateTime.UtcNow;
            try
            {
                if (request?.DataBinding == null || string.IsNullOrWhiteSpace(request.DataBinding.Type))
                {
                    return Ok(new { success = false, error = "Missing dataBinding" });
                }

                if (request.DataBinding.Type?.Equals("sql", StringComparison.OrdinalIgnoreCase) == true)
                {
                    var result = await ExecuteSqlPreviewAsync(request.DataBinding.Sql ?? string.Empty, request.MaxRows);
                    return Ok(new { success = true, rows = result.rows, columns = result.columns });
                }

                return Ok(new { success = false, error = "Binding type not supported yet" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chart preview failed");
                return Ok(new { success = false, error = ex.Message });
            }
            finally
            {
                var elapsed = DateTime.UtcNow - startedAt;
                _logger.LogInformation("Chart preview elapsed {Ms} ms", elapsed.TotalMilliseconds);
            }
        }

        private async Task<(List<Dictionary<string, object?>> rows, List<string> columns)> ExecuteSqlPreviewAsync(string sql, int maxRows)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new InvalidOperationException("SQL is empty");
            }

            // Basic safety: only allow SELECT
            var normalized = sql.TrimStart();
            if (!normalized.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Only SELECT statements are allowed for charts.");
            }

            // Provider-specific limit
            var provider = _db.Database.ProviderName ?? string.Empty;
            string limitedSql = sql;
            if (provider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                // Inject TOP if not already limited
                if (!normalized.StartsWith("SELECT TOP", StringComparison.OrdinalIgnoreCase))
                {
                    limitedSql = normalized.Insert(6, $" TOP {maxRows} ");
                }
            }
            else
            {
                // Assume LIMIT support (Postgres/SQLite); append if not present
                if (!normalized.Contains(" LIMIT ", StringComparison.OrdinalIgnoreCase))
                {
                    limitedSql = sql + $" LIMIT {maxRows}";
                }
            }

            await using var conn = _db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync();
            }

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = limitedSql;
            cmd.CommandType = CommandType.Text;

            var rows = new List<Dictionary<string, object?>>();
            var columns = new List<string>();

            await using var reader = await cmd.ExecuteReaderAsync();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                columns.Add(reader.GetName(i));
            }
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var val = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row[columns[i]] = val;
                }
                rows.Add(row);
            }

            return (rows, columns);
        }
    }
}


