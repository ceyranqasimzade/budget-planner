using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Xml.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using budget_planner.ViewModels;

namespace budget_planner.Services
{
    public class CurrencyService : ICurrencyService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private const string CacheKey = "CBAR_ExchangeRates";

        public CurrencyService(HttpClient httpClient, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _cache = cache;
        }

        public async Task<List<CurrencyRateVM>> GetExchangeRatesAsync()
        {
            if (_cache.TryGetValue(CacheKey, out List<CurrencyRateVM>? cachedRates) && cachedRates != null)
            {
                return cachedRates;
            }

            var rates = new List<CurrencyRateVM>();
            try
            {
                string todayStr = DateTime.Now.ToString("dd.MM.yyyy");
                string url = $"https://www.cbar.az/currencies/{todayStr}.xml";

                string xmlContent = await FetchXmlAsync(url);
                if (!string.IsNullOrEmpty(xmlContent))
                {
                    XDocument doc = XDocument.Parse(xmlContent);
                    var valTypes = doc.Descendants("ValType");

                    foreach (var valType in valTypes)
                    {
                        var valutes = valType.Elements("Valute");
                        foreach (var valute in valutes)
                        {
                            string code = valute.Attribute("Code")?.Value ?? "";
                            string name = valute.Element("Name")?.Value ?? "";
                            string valueStr = valute.Element("Value")?.Value ?? "0";
                            string nominalStr = valute.Element("Nominal")?.Value ?? "1";

                            if (decimal.TryParse(valueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal value) &&
                                decimal.TryParse(nominalStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal nominal))
                            {
                                decimal rate = nominal > 0 ? value / nominal : value;
                                rates.Add(new CurrencyRateVM
                                {
                                    Code = code,
                                    Name = name,
                                    Rate = rate
                                });
                            }
                        }
                    }
                }

                if (rates.Any())
                {
                    // 1 saatlıq cache-ə atırıq
                    _cache.Set(CacheKey, rates, TimeSpan.FromHours(1));
                }
            }
            catch
            {
                // Xəta baş verərsə boş siyahı əvəzinə mövcud siyahını qaytarır
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
            catch
            {
                // HTTP xətası halında təmiz idarəetmə
            }
            return string.Empty;
        }

        public async Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency)
        {
            if (string.IsNullOrWhiteSpace(fromCurrency) || string.IsNullOrWhiteSpace(toCurrency))
                return amount;

            fromCurrency = fromCurrency.ToUpper();
            toCurrency = toCurrency.ToUpper();

            if (fromCurrency == toCurrency)
                return amount;

            var rates = await GetExchangeRatesAsync();

            decimal GetRate(string code)
            {
                if (code == "AZN")
                    return 1m;

                var rate = rates.FirstOrDefault(x => x.Code == code);

                if (rate == null)
                    throw new Exception($"Currency '{code}' tapılmadı.");

                return rate.Rate;
            }

            decimal fromRate = GetRate(fromCurrency);
            decimal toRate = GetRate(toCurrency);

            decimal amountInAzn = fromCurrency == "AZN"
                ? amount
                : amount * fromRate;

            decimal result = toCurrency == "AZN"
                ? amountInAzn
                : amountInAzn / toRate;

            return Math.Round(result, 2);
        }

        public string GetCurrencySymbol(string currencyCode)
        {
            if (string.IsNullOrWhiteSpace(currencyCode))
                return "₼";

            return currencyCode.ToUpper() switch
            {
                "AZN" => "₼",
                "USD" => "$",
                "EUR" => "€",
                "TRY" => "₺",
                "RUB" => "₽",
                "GBP" => "£",
                "CHF" => "CHF",
                "AED" => "د.إ",
                "CAD" => "CA$",
                "CNY" => "¥",
                "GEL" => "⾾",
                _ => currencyCode
            };
        }
    }
}