using AnalisisDocumentalApp.Data;
using AnalisisDocumentalApp.Models;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace AnalisisDocumentalApp.Services
{
    public class LogService : ILogService
    {
        private readonly ApplicationDbContext _context;

        public LogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<byte[]> ExportToExcelAsync(IEnumerable<LogEntry> entries)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Log Entries");

            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Type";
            worksheet.Cells[1, 3].Value = "Description";
            worksheet.Cells[1, 4].Value = "Date and Time";

            int row = 2;
            foreach (var entry in entries)
            {
                worksheet.Cells[row, 1].Value = entry.Id;
                worksheet.Cells[row, 2].Value = entry.Type.ToString();
                worksheet.Cells[row, 3].Value = entry.Description;
                worksheet.Cells[row, 4].Value = entry.DateTime.ToString("yyyy-MM-dd HH:mm:ss");
                row++;
            }

            worksheet.Cells.AutoFitColumns();

            return await package.GetAsByteArrayAsync();

        }

        public async Task<IEnumerable<LogEntry>> GetLogEntriesAsync(string filter = null)
        {
            var query = _context.LogEntries.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = query.Where(l => l.Description.Contains(filter));
            }

            return await query.OrderBy(l => l.Id).ToListAsync();
        }

        public async Task LogActivityAsync(LogType type, string description)
        {
            var logEntry = new LogEntry
            {
                Type = type,
                Description = description,
                DateTime = DateTime.UtcNow
            };

            _context.LogEntries.Add(logEntry);
            await _context.SaveChangesAsync();
        }
    }
}
