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

        public async Task<InvoiceInfo> ExtractFacturaInfoAsync(byte[] documentContent)
        {
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
                invoiceInfo.Date = document.Fields.TryGetValue("InvoiceDate", out var date) ? date.Value.AsDate().DateTime : DateTime.MinValue;

                document.Fields.TryGetValue("InvoiceTotal", out var totalInvoice);

                if(totalInvoice != null)
                {
                    var currency = totalInvoice.Value.AsCurrency();
                    invoiceInfo.TotalInvoice = (decimal)currency.Amount;
                }
                else
                {
                    invoiceInfo.TotalInvoice = (decimal)0;
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

        public async Task<InformationDocument> ExtractInformacionAsync(byte[] documentContent)
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

        /*
        private string GenerateSimpleSummary(string text)
        {
            var sentences = text.Split('.', StringSplitOptions.RemoveEmptyEntries);
            return string.Join(". ", sentences.Take(2)) + ".";
        }
        */

        private string GenerateSimpleSummary(string text)
        {
            int maxWords = 20;
            var words = text.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", words.Take(maxWords)) + (words.Length > maxWords ? "..." : "");
        }


        private string AnalyzeSentiment(string text)
        {
            int positiveWords = CountOccurrences(text, new[] { "bueno", "excelente", "fantástico" });
            int negativeWords = CountOccurrences(text, new[] { "malo", "terrible", "pésimo" });

            if (positiveWords > negativeWords) return "Positivo";
            if (negativeWords > positiveWords) return "Negativo";
            return "Neutral";
        }

        private int CountOccurrences(string text, string[] words)
        {
            return words.Sum(word => text.Split(new[] { ' ', '.', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Count(w => w.Equals(word, StringComparison.OrdinalIgnoreCase)));
        }

    }
}
