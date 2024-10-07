namespace AnalysisDocumentalApp.Models
{
    public class LogEntry
    {
        public int Id { get; set; }
        public LogType Type { get; set; }
        public string Description { get; set; }
        public DateTime DateTime { get; set; }
    }

    public enum LogType
    {
        DocumentUpload,
        AIProcessing,
        UserInteraction
    }
}
