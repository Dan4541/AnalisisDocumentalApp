using AnalysisDocumentalApp.Data;
using AnalysisDocumentalApp.Models;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace AnalysisDocumentalApp.Services
{
    public class LogService : ILogService
    {
        private readonly ApplicationDbContext _context;

        public LogService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Exporta una lista de entradas de log a un archivo de Excel en formato byte array.
        /// </summary>
        /// <param name="entries">Una colección de entradas de log <see cref="LogEntry"/> que se exportarán al archivo de Excel.</param>
        /// <returns>
        /// Un arreglo de bytes que representa el archivo de Excel generado.
        /// </returns>
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

        /// <summary>
        /// Obtiene una lista de entradas de log desde la base de datos, con la opción de aplicar un filtro basado en la descripción.
        /// </summary>
        /// <param name="filter">Una cadena de texto opcional para filtrar las entradas de log por descripción. Si se omite o es nulo, se obtendrán todas las entradas.</param>
        /// <returns>
        /// Una colección de objetos <see cref="LogEntry"/> que cumplen con el filtro proporcionado, si se especifica, ordenados por el identificador de la entrada.
        /// </returns>
        public async Task<IEnumerable<LogEntry>> GetLogEntriesAsync(string filter = null)
        {
            var query = _context.LogEntries.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = query.Where(l => l.Description.Contains(filter));
            }

            return await query.OrderBy(l => l.Id).ToListAsync();
        }

        /// <summary>
        /// Registra una nueva entrada de actividad en los logs de la base de datos.
        /// </summary>
        /// <param name="type">El tipo de la actividad a registrar, basado en el enumerador <see cref="LogType"/>.</param>
        /// <param name="description">La descripción de la actividad registrada.</param>
        /// <returns>Una tarea asincrónica que representa el proceso de guardar el log en la base de datos.</returns>
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
