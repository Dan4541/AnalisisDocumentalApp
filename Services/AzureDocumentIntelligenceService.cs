using AnalisisDocumentalApp.Models;
using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;

namespace AnalisisDocumentalApp.Services
{
    public class AzureDocumentIntelligenceService
    {
        private readonly DocumentAnalysisClient _client;

        public AzureDocumentIntelligenceService(IConfiguration configuration)
        {
            string endpoint = configuration["AzureDocumentIntelligence:Endpoint"];
            string apiKey = configuration["AzureDocumentIntelligence:ApiKey"];

            AzureKeyCredential credential = new AzureKeyCredential(apiKey);
            _client = new DocumentAnalysisClient(new Uri(endpoint), credential);

        }

        /// <summary>
        /// Clasifica un documento basándose en su contenido.
        /// </summary>
        /// <param name="documentContent">Un array de bytes que representa el contenido del documento a clasificar.</param>
        /// <returns>
        /// Retorna un <see cref="Task{TResult}"/> que representa la operación asíncrona.
        /// El resultado de la tarea es un <see cref="DocumentType"/> que indica el tipo de documento clasificado:
        /// <see cref="DocumentType.Invoice"/> si el documento contiene la palabra "factura" (sin distinción entre mayúsculas y minúsculas),
        /// o <see cref="DocumentType.Information"/> en caso contrario.
        /// </returns>
        /// <exception cref="ArgumentException">Se lanza cuando el contenido del documento es nulo o vacío.</exception>
        /// <remarks>
        /// Este método utiliza el servicio Azure Form Recognizer para analizar el contenido del documento.
        /// La clasificación se basa en la presencia de la palabra "factura" en cualquier párrafo del documento.
        /// </remarks>
        public async Task<DocumentType> ClassifyDocumentAsync(byte[] documentContent)
        {
            if (documentContent == null || documentContent.Length == 0)
            {
                throw new ArgumentException("The document content cannot be empty.");
            }

            using MemoryStream stream = new MemoryStream(documentContent);
            AnalyzeDocumentOperation operation = await _client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-document", stream);
            AnalyzeResult result = operation.Value;
            
            if (!result.Paragraphs.Any(p => p.Content.Contains("factura", StringComparison.OrdinalIgnoreCase)))
            {
                return DocumentType.Information;
            }
            else
            {
                return DocumentType.Invoice;
            }
        }

        /// <summary>
        /// Extrae información detallada de una factura a partir de su contenido en bytes.
        /// </summary>
        /// <param name="documentContent">Un array de bytes que representa el contenido del documento de la factura.</param>
        /// <returns>
        /// Retorna un <see cref="Task{TResult}"/> que representa la operación asíncrona.
        /// El resultado de la tarea es un objeto <see cref="InvoiceInfo"/> que contiene la información extraída de la factura,
        /// incluyendo detalles del cliente, proveedor, número de factura, fecha, total y elementos individuales.
        /// </returns>
        /// <remarks>
        /// Este método utiliza el servicio Azure Form Recognizer con el modelo prebuilt-invoice para analizar el contenido de la factura.
        /// La información extraída incluye:
        /// - Nombre y dirección del cliente
        /// - Nombre y dirección del proveedor
        /// - Número de factura
        /// - Fecha de la factura
        /// - Total de la factura
        /// - Lista detallada de elementos de la factura (nombre, cantidad, precio unitario, importe total)
        /// 
        /// Si algún campo no se encuentra en el documento, se utilizan valores predeterminados o cadenas vacías según corresponda.
        /// </remarks>
        /// <exception cref="Exception">Puede lanzar excepciones si hay problemas al analizar el documento o extraer la información.</exception>
        public async Task<InvoiceInfo> ExtractInvoiceInfoAsync(byte[] documentContent)
        {
            if (documentContent == null || documentContent.Length == 0)
            {
                throw new ArgumentException("The document content cannot be empty.");
            }

            using MemoryStream stream = new MemoryStream(documentContent);
            AnalyzeDocumentOperation operation = await _client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-invoice", stream);
            AnalyzeResult result = operation.Value;
            InvoiceInfo invoiceInfo = new InvoiceInfo();
            invoiceInfo.InvoiceItems = new List<InvoiceItem>();

            for (int i = 0; i < result.Documents.Count; i++)
            {
                AnalyzedDocument document = result.Documents[i];                
                invoiceInfo.ClientName = document.Fields.TryGetValue("CustomerName", out var client) ? client.Value.AsString() : "";

                document.Fields.TryGetValue("CustomerAddress", out var addressClient);
                var ac = addressClient.Value.AsAddress();
                invoiceInfo.ClientAddress = $"{ac.City} {ac.State} {ac.House} {ac.Suburb}";
                
                invoiceInfo.SupplierName = document.Fields.TryGetValue("VendorName", out var supplier) ? supplier.Value.AsString() : "";

                document.Fields.TryGetValue("VendorAddress", out var addressVendor);
                var av = addressVendor.Value.AsAddress();
                invoiceInfo.SupplierAddress = $"{av.City} {av.State} {av.House} {av.Suburb}";

                invoiceInfo.InvoiceNumber = document.Fields.TryGetValue("InvoiceId", out var numInvoice) ? numInvoice.Value.AsString() : "";

                if (document.Fields.TryGetValue("InvoiceDate", out var dateField) && dateField.Value != null)
                {
                    try
                    {
                        var extractedDate = dateField.Value.AsDate().DateTime;

                        if (extractedDate == new DateTime(2000, 1, 1))
                        {
                            invoiceInfo.Date = DateTime.MinValue;
                        }
                        else
                        {
                            invoiceInfo.Date = extractedDate;
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        invoiceInfo.Date = DateTime.MinValue;
                    }
                }
                else
                {
                    invoiceInfo.Date = DateTime.MinValue; 
                }


                if (document.Fields.TryGetValue("InvoiceTotal", out var totalInvoice) && totalInvoice.Value != null)
                {
                    try
                    {
                        var currency = totalInvoice.Value.AsCurrency();
                        invoiceInfo.TotalInvoice = (decimal)currency.Amount;
                    }
                    catch (InvalidOperationException)
                    {
                        invoiceInfo.TotalInvoice = 0;
                    }
                }
                else
                {
                    invoiceInfo.TotalInvoice = 0; 
                }

                if (document.Fields.TryGetValue("Items", out var items) && items.Value.AsList() != null)
                {
                    foreach (var item in items.Value.AsList())
                    {
                        
                        var itemFields = item.Value.AsDictionary();
                        InvoiceItem invoiceItem = new InvoiceItem();

                        if (itemFields.TryGetValue("Description", out var descriptionField))
                        {
                            invoiceItem.Name = descriptionField.Value.AsString();
                        }

                        if (itemFields.TryGetValue("Quantity", out var quantityField))
                        {
                            invoiceItem.Quantity = (int)quantityField.Value.AsDouble();
                        }

                        if (itemFields.TryGetValue("UnitPrice", out var unitPriceField))
                        {
                            var unitPriceCurrency = unitPriceField.Value.AsCurrency();
                            invoiceItem.UnitPrice = (decimal)unitPriceCurrency.Amount;
                        }

                        if (itemFields.TryGetValue("Amount", out var totalField))
                        {
                            var totalAmount = totalField.Value.AsCurrency();
                            invoiceItem.TotalAmount = (decimal) totalAmount.Amount;
                        }

                        invoiceInfo.InvoiceItems.Add(invoiceItem);
                               
                    }
                }
            }

            return invoiceInfo;
        }

        /// <summary>
        /// Extrae y analiza información de un documento a partir de su contenido en bytes.
        /// </summary>
        /// <param name="documentContent">Un array de bytes que representa el contenido del documento a analizar.</param>
        /// <returns>
        /// Retorna un <see cref="Task{TResult}"/> que representa la operación asíncrona.
        /// El resultado de la tarea es un objeto <see cref="InformationDocument"/> que contiene:
        /// - Una descripción breve del documento (primeros 300 caracteres o menos)
        /// - Un resumen simple del contenido
        /// - Un análisis del sentimiento general del texto
        /// </returns>
        /// <remarks>
        /// Este método utiliza el servicio Azure Form Recognizer con el modelo prebuilt-document para analizar el contenido del documento.
        /// El proceso incluye:
        /// 1. Extracción del texto completo del documento.
        /// 2. Generación de una descripción breve (máximo 300 caracteres).
        /// 3. Creación de un resumen simple utilizando el método GenerateSimpleSummary (no incluido en este fragmento).
        /// 4. Análisis del sentimiento del texto utilizando el método AnalyzeSentiment (no incluido en este fragmento).
        /// 
        /// Los métodos GenerateSimpleSummary y AnalyzeSentiment deben estar implementados en la clase para que este método funcione correctamente.
        /// </remarks>
        /// <exception cref="Exception">Puede lanzar excepciones si hay problemas al analizar el documento o extraer la información.</exception>
        public async Task<InformationDocument> ExtractInformationAsync(byte[] documentContent)
        {
            using MemoryStream stream = new MemoryStream(documentContent);
            AnalyzeDocumentOperation operation = await _client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-document", stream);
            AnalyzeResult result = operation.Value;

            string fullText = string.Join(" ", result.Paragraphs.Select(p => p.Content));

            return new InformationDocument
            {
                Description = fullText.Length > 300 ? fullText.Substring(0, 300) + "..." : fullText,
                Summary = GenerateSimpleSummary(fullText),
                Feeling = AnalyzeSentiment(fullText)
            };
        }

        /// <summary>
        /// Genera un resumen simple de un texto, limitando la cantidad de palabras.
        /// </summary>
        /// <param name="text">El texto completo del que se generará el resumen.</param>
        /// <returns>
        /// Una cadena que contiene las primeras 20 palabras del texto original.
        /// Si el texto original tiene más de 20 palabras, se añaden puntos suspensivos al final.
        /// </returns>
        /// <remarks>
        /// Este método:
        /// 1. Divide el texto en palabras, considerando espacios, retornos de carro y saltos de línea como separadores.
        /// 2. Selecciona las primeras 20 palabras.
        /// 3. Une estas palabras en una sola cadena.
        /// 4. Si el texto original tenía más de 20 palabras, añade "..." al final del resumen.
        /// </remarks>
        public string GenerateSimpleSummary(string text)
        {
            int maxWords = 20;
            var words = text.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", words.Take(maxWords)) + (words.Length > maxWords ? "..." : "");
        }

        /// <summary>
        /// Analiza el sentimiento general de un texto basándose en la presencia de palabras positivas y negativas predefinidas.
        /// </summary>
        /// <param name="text">El texto a analizar.</param>
        /// <returns>
        /// Una cadena que representa el sentimiento general del texto:
        /// - "Positivo" si hay más palabras positivas que negativas.
        /// - "Negativo" si hay más palabras negativas que positivas.
        /// - "Neutral" si el número de palabras positivas y negativas es igual, o si no se encuentran palabras clave.
        /// </returns>
        private string AnalyzeSentiment(string text)
        {
            int positiveWords = CountOccurrences(text, new[] { "bueno", "excelente", "fantástico" });
            int negativeWords = CountOccurrences(text, new[] { "malo", "terrible", "pésimo" });

            if (positiveWords > negativeWords) return "Positivo";
            if (negativeWords > positiveWords) return "Negativo";
            return "Neutral";
        }

        /// <summary>
        /// Cuenta el número total de ocurrencias de un conjunto de palabras en un texto dado.
        /// </summary>
        /// <param name="text">El texto en el que se buscarán las palabras.</param>
        /// <param name="words">Un array de cadenas que contiene las palabras a buscar.</param>
        /// <returns>
        /// Un entero que representa el número total de veces que aparecen las palabras especificadas en el texto.
        /// </returns>
        /// <remarks>
        /// Este método:
        /// 1. Divide el texto en palabras individuales, utilizando espacios, puntos y comas como separadores.
        /// 2. Compara cada palabra del texto con cada palabra del array 'words'.
        /// 3. Realiza una comparación sin distinguir entre mayúsculas y minúsculas.
        /// 4. Suma todas las ocurrencias encontradas.
        /// </remarks>
        private int CountOccurrences(string text, string[] words)
        {
            return words.Sum(word => text.Split(new[] { ' ', '.', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Count(w => w.Equals(word, StringComparison.OrdinalIgnoreCase)));
        }

    }
}
