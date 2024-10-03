
namespace AnalisisDocumentalApp.Models
{
    public class Document
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public byte[] Content { get; set; }
        public DateTime UploadDate { get; set; }
        public DocumentType Type { get; set; }
    }

    public enum DocumentType
    {
        Invoice,
        Information,
        Unknown
    }
}
