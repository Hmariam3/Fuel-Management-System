using CsvHelper.Configuration.Attributes;

public class TransactionCsvRecord
{
    [Name("Customer num.")]
    public string CustomerNum { get; set; }

    [Name("Customer")]
    public string Customer { get; set; }

    [Name("Date")]
    public string Date { get; set; }

    [Name("Hour")]
    public string Hour { get; set; }

    [Name("Driver code")]
    public string DriverCode { get; set; }

    [Name("Registration num.")]
    public string RegistrationNum { get; set; }

    [Name("Card type")]
    public string CardType { get; set; }

    [Name("Card num.")]
    public string CardNum { get; set; }

    [Name("Card name")]
    public string CardName { get; set; }

    [Name("Receipt num.")]
    public string ReceiptNum { get; set; }

    [Name("Past mileage")]
    public string PastMileage { get; set; }

    [Name("Current mileage")]
    public string CurrentMileage { get; set; }

    [Name("Operation type")]
    public string OperationType { get; set; }

    [Name("Product code")]
    public string ProductCode { get; set; }

    [Name("Product")]
    public string Product { get; set; }

    [Name("Unit price")]
    public string UnitPrice { get; set; }

    [Name("Quantity")]
    public string Quantity { get; set; }

    [Name("Amount")]
    public string Amount { get; set; }

    [Name("Currency num.")]
    public string CurrencyNum { get; set; }

    [Name("Currency")]
    public string Currency { get; set; }

    [Name("Balance")]
    public string Balance { get; set; }

    [Name("Station num.")]
    public string StationNum { get; set; }

    [Name("Place")]
    public string Place { get; set; }

    [Name("Invoice date")]
    public string InvoiceDate { get; set; }

    [Name("Invoice num.")]
    public string InvoiceNum { get; set; }
}