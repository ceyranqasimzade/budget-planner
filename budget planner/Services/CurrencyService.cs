using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using budget_planner.ViewModels;
using Microsoft.Extensions.Caching.Memory; // 🟢 Keş üçün əlavə edildi

namespace budget_planner.Services
{
    public class CurrencyService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache; // 🟢 Keş interfeysi əlavə olundu
        private const string RatesCacheKey = "CBAR_ExchangeRates"; // Keş üçün unikal açar söz

        // Constructor-a IMemoryCache dəstəyi əlavə edildi
        public CurrencyService(HttpClient httpClient, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _cache = cache;
        }

        public async Task<List<CurrencyRateVM>> GetExchangeRatesAsync()
        {
            // 🟢 1. YADDAŞ YOXLANIŞI: Məzənnələr artıq yaddaşda varsa, CBAR-a getmədən anında qaytar
            if (_cache.TryGetValue(RatesCacheKey, out List<CurrencyRateVM>? cachedRates) && cachedRates != null)
            {
                return cachedRates;
            }

            // 🔴 2. Yaddaşda yoxdursa, CBAR-dan məlumatları çəkmək üçün orijinal koda davam et
            var rates = new List<CurrencyRateVM>();

            string todayUrl = "https://www.cbar.az/currencies/today.xml";

            DateTime prevDate = DateTime.Now;
            if (prevDate.DayOfWeek == DayOfWeek.Monday)
                prevDate = prevDate.AddDays(-3);
            else if (prevDate.DayOfWeek == DayOfWeek.Sunday)
                prevDate = prevDate.AddDays(-2);
            else
                prevDate = prevDate.AddDays(-1);

            string prevUrl = $"https://www.cbar.az/currencies/{prevDate:dd.MM.yyyy}.xml";

            try
            {
                string todayXml = await FetchXmlAsync(todayUrl);
                string prevXml = await FetchXmlAsync(prevUrl);

                if (string.IsNullOrEmpty(todayXml))
                {
                    throw new Exception("Bugünkü məzənnə XML-i CBAR-dan çəkilə bilmədi.");
                }

                XDocument todayDoc = XDocument.Parse(todayXml);
                XDocument? prevDoc = !string.IsNullOrEmpty(prevXml) ? XDocument.Parse(prevXml) : null;

                string[] targetCurrencies = { "USD", "EUR", "RUB", "GBP", "TRY", "GEL", "AED", "CHF", "CNY", "CAD" };

                var valutesToday = todayDoc.Descendants("Valute")
                                           .Where(x => targetCurrencies.Contains(x.Attribute("Code")?.Value))
                                           .ToList();

                foreach (var val in valutesToday)
                {
                    string code = val.Attribute("Code")!.Value;
                    string rawValue = val.Element("Value")?.Value?.Replace(',', '.') ?? "1";

                    decimal rate = Convert.ToDecimal(rawValue, CultureInfo.InvariantCulture);

                    decimal prevRate = rate;
                    if (prevDoc != null)
                    {
                        var prevVal = prevDoc.Descendants("Valute").FirstOrDefault(x => x.Attribute("Code")?.Value == code);
                        if (prevVal != null)
                        {
                            string rawPrevValue = prevVal.Element("Value")?.Value?.Replace(',', '.') ?? rawValue;
                            prevRate = Convert.ToDecimal(rawPrevValue, CultureInfo.InvariantCulture);
                        }
                    }

                    rates.Add(new CurrencyRateVM
                    {
                        Code = code,
                        Rate = rate,
                        PreviousRate = prevRate,
                        Symbol = GetCurrencySymbol(code)
                    });
                }

                // 🟢 3. YADDAŞA YAZILMA: CBAR-dan uğurla çəkilən məlumatları 1 saatlıq cache-ə atırıq
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(1));

                _cache.Set(RatesCacheKey, rates, cacheOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Valyuta çəkilərkən xəta: {ex.Message}");

                rates = new List<CurrencyRateVM>
                {
                    new CurrencyRateVM { Code = "USD", Symbol = "$", Rate = 1.70M, PreviousRate = 1.70M },
                    new CurrencyRateVM { Code = "EUR", Symbol = "€", Rate = 1.85M, PreviousRate = 1.84M },
                    new CurrencyRateVM { Code = "TRY", Symbol = "₺", Rate = 0.051M, PreviousRate = 0.052M },
                    new CurrencyRateVM { Code = "RUB", Symbol = "₽", Rate = 0.018M, PreviousRate = 0.018M },
                    new CurrencyRateVM { Code = "GBP", Symbol = "£", Rate = 2.15M, PreviousRate = 2.14M },
                    new CurrencyRateVM { Code = "GEL", Symbol = "₾", Rate = 0.63M, PreviousRate = 0.63M },
                    new CurrencyRateVM { Code = "AED", Symbol = "د.إ", Rate = 0.46M, PreviousRate = 0.46M },
                    new CurrencyRateVM { Code = "CHF", Symbol = "CHF", Rate = 1.92M, PreviousRate = 1.91M },
                    new CurrencyRateVM { Code = "CNY", Symbol = "¥", Rate = 0.23M, PreviousRate = 0.23M },
                    new CurrencyRateVM { Code = "CAD", Symbol = "CA$", Rate = 1.25M, PreviousRate = 1.25M }
                };
            }

            return rates;
        }

        private async Task<string> FetchXmlAsync(string url)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch { }
            return string.Empty;
        }

        // 🟢 STATIC öz yerində qaldı
        public static string GetCurrencySymbol(string code)
        {
            return code switch
            {
                "USD" => "$",
                "EUR" => "€",
                "RUB" => "₽",
                "GBP" => "£",
                "TRY" => "₺",
                "GEL" => "₾",
                "AED" => "د.إ",
                "CHF" => "CHF",
                "CNY" => "¥",
                "CAD" => "CA$",
                "AZN" => "₼",
                _ => code
            };
        }
    }
}