namespace AnalisisDocumentalApp.Models
{
    public class InvoiceInfo
    {
        public string ClientName { get; set; }
        public string ClientAddress { get; set; }
        public string SupplierName { get; set; }
        public string SupplierAddress { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime Date { get; set; }
        public List<InvoiceItem> InvoiceItems { get; set; }
        public decimal TotalInvoice { get; set; }
    }
    public class InvoiceItem
    {
        public string Name { get; set; }
        public int Quantity { get; set; }        
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
