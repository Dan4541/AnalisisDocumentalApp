using AnalisisDocumentalApp.Models;

namespace AnalisisDocumentalApp.Services
{
    public interface IDocumentService
    {
        Task<int> UploadDocumentAsync(string fileName, string contentType, byte[] content);
        Task<DocumentType> ClassifyDocumentAsync(int documentId);
        Task<InvoiceInfo> ExtractFacturaInfoAsync(int documentId);
        Task<InformationDocument> ExtractInformacionAsync(int documentId);
    }
}
