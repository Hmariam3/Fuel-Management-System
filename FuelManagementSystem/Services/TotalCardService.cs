using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq; // Needed for Any(), FirstOrDefault()
using FuelManagementSystem.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Data.Entity; // EF6 (or Microsoft.EntityFrameworkCore for EF Core)

namespace FuelManagementSystem.Services
{
    public class TotalCardService
    {
        private readonly HttpClient _client;
        private readonly ILogger<TotalCardService> _logger;
        private readonly CookieContainer _cookieContainer = new CookieContainer();
        private readonly FuelManagementSystemEntities _db;

        public TotalCardService(FuelManagementSystemEntities db, ILogger<TotalCardService> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var handler = new HttpClientHandler { CookieContainer = _cookieContainer };
            _client = new HttpClient(handler);
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }


        public async Task<bool> LoginAsync(string login, string password)
        {
            try
            {
                var loginUrl = "https://www.mytotalfuelcard.com/Sessions/Create";
                var loginData = new { login, password };
                var content = new StringContent(JsonConvert.SerializeObject(loginData), System.Text.Encoding.UTF8, "application/json");

                var response = await _client.PostAsync(loginUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Login response status: {response.StatusCode}, content: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var cookies = _cookieContainer.GetCookies(new Uri(loginUrl));
                    var cookieStrings = new List<string>();
                    foreach (Cookie cookie in cookies)
                    {
                        cookieStrings.Add($"{cookie.Name}={cookie.Value}");
                    }
                    _logger.LogInformation($"Cookies received: {string.Join(", ", cookieStrings)}");
                    return true;
                }

                _logger.LogError($"Login failed with status {response.StatusCode}: {responseContent}");
                return false;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Login failed.");
                return false;
            }
        }

        public async Task<ApiResponse> GetApiTransactionsAsync(string cardId, string startDate = "", string endDate = "")
        {
            var url = $"https://www.mytotalfuelcard.com/Operations?idClient=-1&idCarte={cardId}&dateDebut={startDate}&dateFin={endDate}&typeOp=transaction&limit=100&start=0";
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<ApiResponse>(json);
        }

        public async Task<int> ImportToDatabaseAsync(string cardId, string startDate = "", string endDate = "")
        {
            var apiResponse = await GetApiTransactionsAsync(cardId, startDate, endDate);
            if (apiResponse?.Data == null || !apiResponse.Data.Any())
            {
                return 0;
            }

            int importedCount = 0;

            foreach (var apiTx in apiResponse.Data)
            {
                var ecard = _db.Ecards.FirstOrDefault(e => e.EcardID == apiTx.IdCarte);
                if (ecard == null)
                {
                    _logger.LogWarning($"E-card {apiTx.IdCarte} not found.");
                    continue;
                }

                var plateNo = ecard.PlateNo;
                var vehicle = _db.Vehicles.FirstOrDefault(v => v.PlateNo == plateNo);
                if (vehicle == null)
                {
                    _logger.LogWarning($"Vehicle with PlateNo {plateNo} not found.");
                    continue;
                }

                int? driverId = vehicle.DriverID;
                if (driverId == 0)
                {
                    var driver = _db.Drivers.FirstOrDefault(d => d.EmployeeNumber == 4355);
                    if (driver == null)
                    {
                        driver = new Driver
                        {
                            DriverName = "Default Driver",
                            EmployeeNumber = 4355,
                            CreatedAt = DateTime.Now
                        };
                        _db.Drivers.Add(driver);
                        await _db.SaveChangesAsync();
                    }
                    driverId = driver.DriverID;
                }

                int begKm = 0;

                // Find last transaction for this vehicle
                var lastTx = _db.FuelTransactions
                                .Where(t => t.PlateNo == plateNo)
                                .OrderByDescending(t => t.TransactionDateTime)
                                .FirstOrDefault();
                if (lastTx != null)
                {
                    begKm = lastTx.EndKm;
                }

                var fuelTx = new FuelTransaction
                {
                    RequestID = 0, // link to refill request if exists
                    PlateNo = plateNo,
                    DriverID = driverId ?? 0,
                    LitersUsed = (decimal)apiTx.Quantity,
                    Amount = (decimal)apiTx.Amount,
                    BegKm = begKm,
                    EndKm = apiTx.Kms,
                    D_KM = apiTx.Kms - begKm,
                    AvgKmLiter = (apiTx.Kms - begKm) > 0 && apiTx.Quantity > 0 ? (decimal)(apiTx.Kms - begKm) / (decimal)apiTx.Quantity : 0m,
                    TransactionDateTime = apiTx.TransactionDateTime,
                    EcardBalanceAfter = (decimal)apiTx.Solde
                };

                _db.FuelTransactions.Add(fuelTx);

                // Update Ecard Balance
                ecard.Balance = (decimal)apiTx.Solde;
                _db.Entry(ecard).State = EntityState.Modified;

                importedCount++;
            }

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException ex)
            {
                var inner = ex.InnerException?.InnerException?.Message ?? ex.Message;
                Debug.WriteLine($"SaveChanges failed: {inner}");
                throw;
            }

            _logger.LogInformation($"Imported {importedCount} transactions for card {cardId}.");
            return importedCount;
        }
    }
}
