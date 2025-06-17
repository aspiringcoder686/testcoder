using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClosedXML.Excel;

namespace Tester
{
    public class QueryTest
    {
        private readonly string _connectionString;

        public QueryTest(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task RunTestsAsync(List<EntityDefinition> entities, string reportFolder)
        {
            var reportData = new List<QueryTestResult>();
            int sno = 1;

            foreach (var entity in entities)
            {
                foreach (var query in entity.Queries)
                {
                    var result = await TestQueryAsync(entity.Name, query);
                    result.SNo = sno++;
                    reportData.Add(result);
                }
            }

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string reportPath = System.IO.Path.Combine(reportFolder, $"QueryTestReport_{timestamp}.xlsx");
            GenerateExcelReport(reportData, reportPath);
            Console.WriteLine($"Report generated at: {reportPath}");
        }

        private async Task<QueryTestResult> TestQueryAsync(string entityName, QueryDefinition query)
        {
            var finalSql = ApplyTop1IfSelect(query.Sql);
            var commandType = DetermineCommandType(finalSql);
            var operationType = DetermineOperationType(finalSql);

            if (commandType == "PROCEDURE" || 
                operationType == "INSERT" ||
                operationType == "UPDATE" ||
                operationType == "DELETE")
            {
                return new QueryTestResult
                {
                    EntityName = entityName,
                    CommandName = query.Name,
                    Query = finalSql,
                    Status = "SKIPPED",
                    CommandType = commandType,
                    OperationType = operationType
                };
            }

            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand(finalSql, conn);
                AddMockParameters(cmd, finalSql);

                using var reader = await cmd.ExecuteReaderAsync();

                return new QueryTestResult
                {
                    EntityName = entityName,
                    CommandName = query.Name,
                    Query = finalSql,
                    Status = "PASSED",
                    CommandType = commandType,
                    OperationType = operationType
                };
            }
            catch (Exception ex)
            {
                return new QueryTestResult
                {
                    EntityName = entityName,
                    CommandName = query.Name,
                    Query = finalSql,
                    Status = $"FAILED - {ex.Message}",
                    CommandType = commandType,
                    OperationType = operationType
                };
            }
        }

        private void AddMockParameters(SqlCommand cmd, string sql)
        {
            var matches = Regex.Matches(sql, @"@\w+");
            var addedParams = new HashSet<string>();

            foreach (Match match in matches)
            {
                var paramName = match.Value;
                if (!addedParams.Contains(paramName))
                {
                    cmd.Parameters.AddWithValue(paramName, GetMockValue(paramName));
                    addedParams.Add(paramName);
                }
            }
        }

        private object GetMockValue(string paramName)
        {
            if (paramName.Contains("Id", StringComparison.OrdinalIgnoreCase))
                return 1;
            if (paramName.Contains("Name", StringComparison.OrdinalIgnoreCase))
                return "TestName";
            if (paramName.Contains("Date", StringComparison.OrdinalIgnoreCase))
                return DateTime.Now;

            return DBNull.Value;
        }

        private string ApplyTop1IfSelect(string sql)
        {
            var pattern = @"^\s*select\s+(?!top\s+\d+)";
            if (Regex.IsMatch(sql, pattern, RegexOptions.IgnoreCase))
            {
                return Regex.Replace(sql, @"^\s*select\s+", "SELECT TOP 1 ", RegexOptions.IgnoreCase);
            }
            return sql;
        }

        private string DetermineCommandType(string sql)
        {
            if (Regex.IsMatch(sql, @"^\s*exec", RegexOptions.IgnoreCase))
                return "PROCEDURE";
            if (Regex.IsMatch(sql, @"^\s*select\s+\w+\s*\(", RegexOptions.IgnoreCase))
                return "FUNCTION";
            if (Regex.IsMatch(sql, @"^\s*select", RegexOptions.IgnoreCase))
                return "QUERY";

            return "UNKNOWN";
        }

        private string DetermineOperationType(string sql)
        {
            if (Regex.IsMatch(sql, @"\bselect\b", RegexOptions.IgnoreCase))
                return "READ";
            if (Regex.IsMatch(sql, @"\binsert\b", RegexOptions.IgnoreCase))
                return "INSERT";
            if (Regex.IsMatch(sql, @"\bupdate\b", RegexOptions.IgnoreCase))
                return "UPDATE";
            if (Regex.IsMatch(sql, @"\bdelete\b", RegexOptions.IgnoreCase))
                return "DELETE";

            return "UNKNOWN";
        }

        private void GenerateExcelReport(List<QueryTestResult> data, string filePath)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("QueryTestReport");

            ws.Cell(1, 1).Value = "SNo";
            ws.Cell(1, 2).Value = "EntityName";
            ws.Cell(1, 3).Value = "CommandName";
            ws.Cell(1, 4).Value = "Query";
            ws.Cell(1, 5).Value = "Status";
            ws.Cell(1, 6).Value = "Assigned";
            ws.Cell(1, 7).Value = "CommandType";
            ws.Cell(1, 8).Value = "OperationType";

            string[] resources = { "R1", "R2", "R3" };
            int row = 2;
            int resourceIndex = 0;

            foreach (var item in data)
            {
                var assigned = resources[resourceIndex];
                resourceIndex = (resourceIndex + 1) % resources.Length;

                ws.Cell(row, 1).Value = item.SNo;
                ws.Cell(row, 2).Value = item.EntityName;
                ws.Cell(row, 3).Value = item.CommandName;
                ws.Cell(row, 4).Value = item.Query;
                ws.Cell(row, 5).Value = item.Status;
                ws.Cell(row, 6).Value = assigned;
                ws.Cell(row, 7).Value = item.CommandType;
                ws.Cell(row, 8).Value = item.OperationType;

                row++;
            }

            workbook.SaveAs(filePath);
        }
    }

    public class QueryTestResult
    {
        public int SNo { get; set; }
        public string EntityName { get; set; }
        public string CommandName { get; set; }
        public string Query { get; set; }
        public string Status { get; set; }
        public string CommandType { get; set; }
        public string OperationType { get; set; }
    }
}
