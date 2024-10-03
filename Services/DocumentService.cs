using AnalisisDocumentalApp.Data;
using AnalisisDocumentalApp.Models;
using Microsoft.EntityFrameworkCore;

namespace AnalisisDocumentalApp.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly ApplicationDbContext _context;
        private readonly AzureDocumentIntelligenceService _azureService;

        public DocumentService(ApplicationDbContext context, AzureDocumentIntelligenceService azureService)
        {
            _context = context;
            _azureService = azureService;
        }

        public async Task<DocumentType> ClassifyDocumentAsync(int documentId)
        {
            var document = await _context.Documents.FindAsync(documentId);
            if (document == null)
            {
                throw new ArgumentException("Document not found", nameof(documentId));
            }

            var documentType = await _azureService.ClassifyDocumentAsync(document.Content);

            document.Type = documentType;
            await _context.SaveChangesAsync();

            return documentType;
        }

        public async Task<InvoiceInfo> ExtractFacturaInfoAsync(int documentId)
        {
            var document = await _context.Documents.FindAsync(documentId);
            if (document == null)
            {
                throw new ArgumentException("Document not found", nameof(documentId));
            }

            return await _azureService.ExtractFacturaInfoAsync(document.Content);
        }

        public async Task<InformationDocument> ExtractInformacionAsync(int documentId)
        {
            var document = await _context.Documents.FindAsync(documentId);
            if (document == null)
            {
                throw new ArgumentException("Document not found", nameof(documentId));
            }

            return await _azureService.ExtractInformacionAsync(document.Content);
        }

        public async Task<int> UploadDocumentAsync(string fileName, string contentType, byte[] content)
        {
            try
            {
                var document = new Document
                {
                    FileName = fileName,
                    ContentType = contentType,
                    Content = content,
                    UploadDate = DateTime.UtcNow
                };

                _context.Documents.Add(document);
                await _context.SaveChangesAsync();

                return document.Id;
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Database Error: {ex.InnerException?.Message}");
                throw new ApplicationException("Error saving document to database.", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected Error: {ex.Message}");
                throw new ApplicationException("Unexpected error while uploading the document.", ex);
            }

        }
    }
}
