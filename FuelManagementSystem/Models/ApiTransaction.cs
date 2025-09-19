using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace FuelManagementSystem.Models
{
    public class ApiTransaction
    {
        [JsonProperty("id")]
        public long Id { get; set; }  // Unique transaction ID

        [JsonProperty("date_trans")]
        public string DateTrans { get; set; }  // Raw transaction date string from API (/Date(…)/)

        [JsonProperty("heure_trans")]
        public int HeureTrans { get; set; }  // Transaction time in HHMMSS format (e.g., 213635 = 21:36:35)

        [JsonProperty("lib_trans")]
        public string LibTrans { get; set; }  // Transaction description (e.g., "Vente sur carte pré chargée")

        [JsonProperty("id_carte")]
        public string IdCarte { get; set; }  // Card ID used for this transaction

        [JsonProperty("client")]
        public string Client { get; set; }  // Client name or organization

        [JsonProperty("lieu")]
        public string Lieu { get; set; }  // Location or fuel station name

        [JsonProperty("no_ticket")]
        public double NoTicket { get; set; }  // Receipt or ticket number

        [JsonProperty("kms")]
        public int Kms { get; set; }  // Kilometers recorded (if applicable, usually 0)

        [JsonProperty("qtt")]
        public double Quantity { get; set; }  // Quantity of fuel in liters

        [JsonProperty("montant")]
        public double Amount { get; set; }  // Transaction amount in local currency (ETB)

        [JsonProperty("solde")]
        public double Solde { get; set; }  // Remaining balance on the card after transaction

        [JsonProperty("prod")]
        public string Product { get; set; }  // Product type (e.g., "Gasoil")

        [JsonProperty("devise_sigle")]
        public string Currency { get; set; }  // Currency symbol (e.g., "ETB")

        [JsonProperty("numero_facture")]
        public string NumeroFacture { get; set; }  // Invoice number (or "prepaid" if prepaid card)

        // ✅ Computed property for clean DateTime
        [JsonIgnore]
        public DateTime TransactionDateTime
        {
            get
            {
                if (!string.IsNullOrEmpty(DateTrans) && DateTrans.StartsWith("/Date("))
                {
                    // Extract milliseconds from /Date(1757973600000+0200)/
                    var ticksStr = DateTrans.Replace("/Date(", "").Replace(")/", "");
                    if (long.TryParse(ticksStr.Split('+')[0], out var ms))
                    {
                        return DateTimeOffset.FromUnixTimeMilliseconds(ms).DateTime;
                    }
                }
                return DateTime.MinValue;
            }
        }
    }

    public class ApiResponse
    {
        [JsonProperty("count")]
        public int Count { get; set; }  // Total number of transactions returned by the API

        [JsonProperty("data")]
        public List<ApiTransaction> Data { get; set; }  // List of transactions
    }
}
