﻿using AnalysisDocumentalApp.Models;

namespace AnalysisDocumentalApp.Services
{
    public interface ILogService
    {
        Task LogActivityAsync(LogType type, string description);
        Task<IEnumerable<LogEntry>> GetLogEntriesAsync(string filter = null);
        Task<byte[]> ExportToExcelAsync(IEnumerable<LogEntry> entries);
    }
}
