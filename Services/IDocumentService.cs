using AnalisisDocumentalApp.Models;

namespace AnalisisDocumentalApp.Services
{
    public interface IDocumentService
    {
        Task<int> UploadDocumentAsync(string fileName, string contentType, byte[] content);
        Task<DocumentType> ClassifyDocumentAsync(int documentId);
        Task<InvoiceInfo> ExtractInvoiceInfoAsync(int documentId);
        Task<InformationDocument> ExtractInformationAsync(int documentId);
    }
}
