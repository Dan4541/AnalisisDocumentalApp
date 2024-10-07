using AnalysisDocumentalApp.Data;
using AnalysisDocumentalApp.Models;
using Microsoft.EntityFrameworkCore;

namespace AnalysisDocumentalApp.Services
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

        /// <summary>
        /// Clasifica un documento basándose en su contenido y actualiza el tipo en la base de datos.
        /// </summary>
        /// <param name="documentId">El identificador único del documento a clasificar.</param>
        /// <returns>
        /// Un <see cref="Task{TResult}"/> que representa la operación asíncrona.
        /// El resultado de la tarea es un <see cref="DocumentType"/> que indica el tipo de documento clasificado.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Se lanza cuando no se encuentra un documento con el ID proporcionado.
        /// </exception>
        /// <remarks>
        /// Este método realiza las siguientes operaciones:
        /// 1. Busca el documento en la base de datos utilizando el ID proporcionado.
        /// 2. Si el documento no se encuentra, lanza una excepción.
        /// 3. Utiliza un servicio de Azure para clasificar el contenido del documento.
        /// 4. Actualiza el tipo del documento en la base de datos con el resultado de la clasificación.
        /// 5. Guarda los cambios en la base de datos.
        /// 6. Devuelve el tipo de documento clasificado.
        /// </remarks>
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

        /// <summary>
        /// Extrae información detallada de una factura a partir de un documento almacenado en la base de datos.
        /// </summary>
        /// <param name="documentId">El identificador único del documento de factura a procesar.</param>
        /// <returns>
        /// Un <see cref="Task{TResult}"/> que representa la operación asíncrona.
        /// El resultado de la tarea es un objeto <see cref="InvoiceInfo"/> que contiene la información extraída de la factura.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Se lanza cuando no se encuentra un documento con el ID proporcionado.
        /// </exception>
        /// <remarks>
        /// Este método realiza las siguientes operaciones:
        /// 1. Busca el documento en la base de datos utilizando el ID proporcionado.
        /// 2. Si el documento no se encuentra, lanza una excepción.
        /// 3. Utiliza un servicio de Azure para extraer la información de la factura del contenido del documento.
        /// 4. Devuelve la información extraída de la factura.
        /// </remarks>
        public async Task<InvoiceInfo> ExtractInvoiceInfoAsync(int documentId)
        {
            var document = await _context.Documents.FindAsync(documentId);
            if (document == null)
            {
                throw new ArgumentException("Document not found", nameof(documentId));
            }

            return await _azureService.ExtractInvoiceInfoAsync(document.Content);
        }

        /// <summary>
        /// Extrae la información de un documento utilizando un servicio de Azure.
        /// </summary>
        /// <param name="documentId">El identificador único del documento en la base de datos.</param>
        /// <returns>
        /// Un objeto <see cref="InformationDocument"/> que contiene la información extraída del documento.
        /// </returns>
        /// <exception cref="ArgumentException">Se lanza si el documento no se encuentra en la base de datos.</exception>
        public async Task<InformationDocument> ExtractInformationAsync(int documentId)
        {
            var document = await _context.Documents.FindAsync(documentId);
            if (document == null)
            {
                throw new ArgumentException("Document not found", nameof(documentId));
            }

            return await _azureService.ExtractInformationAsync(document.Content);
        }

        /// <summary>
        /// Sube un documento a la base de datos y guarda su contenido.
        /// </summary>
        /// <param name="fileName">El nombre del archivo que se va a subir.</param>
        /// <param name="contentType">El tipo de contenido del archivo (por ejemplo, "application/pdf").</param>
        /// <param name="content">Un arreglo de bytes que representa el contenido del archivo.</param>
        /// <returns>El identificador único del documento subido.</returns>
        /// <exception cref="ApplicationException">
        /// Se lanza cuando ocurre un error al guardar el documento en la base de datos o si ocurre un error inesperado.
        /// </exception>
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
