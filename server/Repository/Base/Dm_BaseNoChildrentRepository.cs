using Dapper;
using Microsoft.Extensions.Logging;
using server.Dtos.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server.Repository.Base
{
    public class Dm_BaseNoChildrentRepository<T> where T : class
    {
        protected readonly IDbConnection _dbConnection;
        protected readonly ILogger _logger;

        public Dm_BaseNoChildrentRepository(IDbConnection dbConnection, ILogger logger)
        {
            _dbConnection = dbConnection;
            _logger = logger;
        }

        /// <summary>
        /// Get paged data with filtering, searching, and sorting
        /// </summary>
        /// <param name="request">Paging and sorting parameters</param>
        /// <param name="tableName">Database table name</param>
        /// <param name="columns">Columns to select (default: "*")</param>
        /// <param name="searchColumns">Columns to include in text search</param>
        /// <param name="additionalWhereClause">Additional WHERE conditions</param>
        /// <param name="customSortMappings">Custom sort expressions for specific columns</param>
        /// <param name="defaultSortColumn">Default column to sort by if not specified</param>
        /// <returns>Paged result with items and count</returns>
        public async Task<PagedResult<T>> GetPagedAsync(
            PagedRequest request,
            string tableName,
            string columns = "*",
            string[] searchColumns = null,
            string additionalWhereClause = null,
            Dictionary<string, string> customSortMappings = null,
            string defaultSortColumn = "CreatedDate")
        {
            var whereClause = new StringBuilder("WHERE \"IsDelete\" = false");
            var parameters = new DynamicParameters();

            // Add additional WHERE conditions if provided
            if (!string.IsNullOrWhiteSpace(additionalWhereClause))
            {
                whereClause.Append($" AND ({additionalWhereClause})");
            }

            // Add search term conditions
            if (!string.IsNullOrWhiteSpace(request.SearchTerm) && searchColumns != null && searchColumns.Length > 0)
            {
                whereClause.Append(" AND (");
                for (int i = 0; i < searchColumns.Length; i++)
                {
                    if (i > 0) whereClause.Append(" OR ");
                    whereClause.Append($"\"{searchColumns[i]}\" ILIKE @SearchTerm");
                }
                whereClause.Append(")");
                parameters.Add("SearchTerm", $"%{request.SearchTerm}%");
            }

            // Determine sorting
            var sortColumn = !string.IsNullOrWhiteSpace(request.SortBy) ? request.SortBy : defaultSortColumn;
            var sortDirection = request.SortDescending ? "DESC" : "ASC";

            // Build ORDER BY clause
            string orderByClause;

            // Check for custom sort expression
            if (customSortMappings != null && customSortMappings.TryGetValue(sortColumn.ToLower(), out string customSort))
            {
                // Use custom sort expression with direction placeholder
                orderByClause = string.Format(customSort, sortDirection);
            }
            else if (sortColumn.ToLower() == "ma")
            {
                // Default natural sorting for "Ma" field
                orderByClause = $@"
                    CASE 
                        -- Check if code is numeric
                        WHEN ""Ma"" ~ '^[0-9]+$' THEN 0
                        -- Check if code is hierarchical numeric format
                        WHEN ""Ma"" ~ '^[0-9]+\.[0-9.]+$' THEN 1
                        -- Otherwise text
                        ELSE 2
                    END {sortDirection},
                    CASE 
                        -- For simple numeric codes: sort by numeric value
                        WHEN ""Ma"" ~ '^[0-9]+$' THEN (""Ma"")::numeric
                        ELSE 0
                    END {sortDirection},
                    CASE 
                        -- For hierarchical codes: split and sort each part
                        WHEN ""Ma"" ~ '^[0-9]+\.[0-9.]+$' THEN 
                            array_to_string(array(
                                SELECT lpad(split_part(""Ma"", '.', generate_series(1, regexp_count(""Ma"", '\\.')+1))::text, 10, '0')
                                FROM generate_series(1, regexp_count(""Ma"", '\\.')+1)
                            ), '.')
                        ELSE ""Ma""
                    END {sortDirection},
                    -- Finally sort text codes alphabetically
                    ""Ma"" {sortDirection}";
            }
            else if (sortColumn.ToLower() == "ten")
            {
                // Special sorting for "Ten" column - without diacritics
                orderByClause = $@"unaccent(LOWER(""Ten"")) {sortDirection}";
            }
            else
            {
                // Default column sorting - FIXED to ensure proper case for column names
                // Ensure exact case match for column name by preserving original case
                orderByClause = $"\"{sortColumn}\" {sortDirection}";
            }

            // Calculate pagination
            var offset = (request.PageNumber - 1) * request.PageSize;
            parameters.Add("Offset", offset);
            parameters.Add("PageSize", request.PageSize);

            // Build and execute query
            var sql = $@"
                -- Ensure unaccent extension is enabled
                CREATE EXTENSION IF NOT EXISTS unaccent;

                -- Count total records
                SELECT COUNT(*) 
                FROM ""{tableName}"" 
                {whereClause};

                -- Get paged data
                SELECT {columns}
                FROM ""{tableName}"" 
                {whereClause}
                ORDER BY {orderByClause}
                LIMIT @PageSize OFFSET @Offset";

            _logger.LogInformation("Executing paged query: {Sql}", sql);

            using var multi = await _dbConnection.QueryMultipleAsync(sql, parameters);
            
            var totalCount = await multi.ReadSingleAsync<int>();
            var items = await multi.ReadAsync<T>();

            return new PagedResult<T>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }
    }
}
