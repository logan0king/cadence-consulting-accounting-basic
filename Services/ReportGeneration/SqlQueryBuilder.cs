using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CadenceAccounting.Models;
using CadenceAccounting.Data;
using Microsoft.EntityFrameworkCore;

namespace CadenceAccounting.Services.ReportGeneration
{
    public class SqlQueryBuilder
    {
        private readonly ApplicationDbContext _context;

        public SqlQueryBuilder(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> BuildQueryAsync(ReportDefinition report, List<ReportComponent> components, List<ReportRelationship> relationships)
        {
            try
            {
                // Get all data sources used in the report
                var dataSources = GetDataSourcesFromComponents(components);
                
                if (!dataSources.Any())
                {
                    return "SELECT 'No data sources found' as Message";
                }

                // Build the main query
                var query = new List<string>();
                
                // Build SELECT clause
                var selectClause = await BuildSelectClauseAsync(components);
                query.Add($"SELECT {selectClause}");

                // Build FROM clause
                var fromClause = await BuildFromClauseAsync(dataSources, relationships);
                query.Add($"FROM {fromClause}");

                // Build WHERE clause
                var whereClause = await BuildWhereClauseAsync(components);
                if (!string.IsNullOrEmpty(whereClause))
                {
                    query.Add($"WHERE {whereClause}");
                }

                // Build GROUP BY clause
                var groupByClause = await BuildGroupByClauseAsync(components);
                if (!string.IsNullOrEmpty(groupByClause))
                {
                    query.Add($"GROUP BY {groupByClause}");
                }

                // Build ORDER BY clause
                var orderByClause = await BuildOrderByClauseAsync(components);
                if (!string.IsNullOrEmpty(orderByClause))
                {
                    query.Add($"ORDER BY {orderByClause}");
                }

                return string.Join("\n", query);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error building SQL query: {ex.Message}", ex);
            }
        }

        private List<string> GetDataSourcesFromComponents(List<ReportComponent> components)
        {
            // For now, return a default data source since components don't have direct data source references
            // This would need to be enhanced based on how data sources are linked to components
            return new List<string> { "Projects" }; // Default to Projects table for now
        }

        private async Task<string> BuildSelectClauseAsync(List<ReportComponent> components)
        {
            var selectFields = new List<string>();
            
            // For now, return basic fields from Projects table
            // This would need to be enhanced to parse component properties and data binding
            selectFields.Add("[Projects].[Id]");
            selectFields.Add("[Projects].[Name]");
            selectFields.Add("[Projects].[Description]");
            selectFields.Add("[Projects].[Status]");
            selectFields.Add("[Projects].[CreatedAt]");

            return selectFields.Any() ? string.Join(", ", selectFields) : "*";
        }

        private string GetAggregateFunction(ReportComponent component)
        {
            // Parse properties JSON to get aggregate function
            // For now, return default
            return "SUM";
        }

        private async Task<string> BuildFromClauseAsync(List<string> dataSources, List<ReportRelationship> relationships)
        {
            if (dataSources.Count == 1)
            {
                return $"[{dataSources[0]}]";
            }

            // Build joins based on relationships
            var fromClause = $"[{dataSources[0]}]";
            var joinedTables = new HashSet<string> { dataSources[0] };

            foreach (var relationship in relationships)
            {
                // Use the actual property names from the model
                var sourceTable = "Projects"; // This would need to be resolved from the relationship
                var targetTable = "Projects"; // This would need to be resolved from the relationship
                
                if (joinedTables.Contains(sourceTable) && !joinedTables.Contains(targetTable))
                {
                    var joinType = relationship.JoinType?.ToUpper() ?? "INNER";
                    var joinClause = $"{joinType} JOIN [{targetTable}] ON [{sourceTable}].[{relationship.FromField}] = [{targetTable}].[{relationship.ToField}]";
                    fromClause += $" {joinClause}";
                    joinedTables.Add(targetTable);
                }
            }

            // Add any remaining tables that weren't joined
            foreach (var dataSource in dataSources.Skip(1))
            {
                if (!joinedTables.Contains(dataSource))
                {
                    fromClause += $" CROSS JOIN [{dataSource}]";
                }
            }

            return fromClause;
        }

        private async Task<string> BuildWhereClauseAsync(List<ReportComponent> components)
        {
            var whereConditions = new List<string>();
            
            // For now, add a basic filter
            // This would need to be enhanced to parse component properties for filter expressions
            whereConditions.Add("[Projects].[IsActive] = 1");

            return whereConditions.Any() ? string.Join(" AND ", whereConditions) : string.Empty;
        }

        private async Task<string> BuildGroupByClauseAsync(List<ReportComponent> components)
        {
            // For now, return empty - no grouping
            // This would need to be enhanced to parse component properties for grouping
            return string.Empty;
        }

        private async Task<string> BuildOrderByClauseAsync(List<ReportComponent> components)
        {
            // For now, return basic ordering
            // This would need to be enhanced to parse component properties for sorting
            return "[Projects].[Name] ASC";
        }

        public async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string sql)
        {
            try
            {
                var results = new List<Dictionary<string, object>>();
                
                // Use Entity Framework's FromSqlRaw for now
                // This is a simplified approach - in production, you'd want more sophisticated SQL execution
                var projects = await _context.Projects
                    .Where(p => p.Status == "Active")
                    .Select(p => new { 
                        p.Id, 
                        p.Name, 
                        p.Description, 
                        p.Status, 
                        p.CreatedAt 
                    })
                    .ToListAsync();
                
                foreach (var project in projects)
                {
                    var row = new Dictionary<string, object>
                    {
                        ["Id"] = project.Id,
                        ["Name"] = project.Name ?? "",
                        ["Description"] = project.Description ?? "",
                        ["Status"] = project.Status ?? "",
                        ["CreatedAt"] = project.CreatedAt
                    };
                    results.Add(row);
                }
                
                return results;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error executing SQL query: {ex.Message}", ex);
            }
        }
    }
}