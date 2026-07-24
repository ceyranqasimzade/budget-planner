using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using budget_planner.ViewModels;

namespace budget_planner.Services
{
    public class CurrencyService
    {
        private readonly HttpClient _httpClient;

        public CurrencyService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<CurrencyRateVM>> GetExchangeRatesAsync()
        {
            var rates = new List<CurrencyRateVM>();

            // 1. Bu günün və Dünənin URL-lərini təyin edirik
            string todayUrl = "https://www.cbar.az/currencies/today.xml";

            // Həftəsonlarını (Şənbə/Bazar) nəzərə alaraq əvvəlki iş gününü tapırıq
            DateTime prevDate = DateTime.Now;
            if (prevDate.DayOfWeek == DayOfWeek.Monday)
                prevDate = prevDate.AddDays(-3); // Bazar ertəsidirsə -> Cüməyə get
            else if (prevDate.DayOfWeek == DayOfWeek.Sunday)
                prevDate = prevDate.AddDays(-2); // Bazardırsa -> Cüməyə get
            else
                prevDate = prevDate.AddDays(-1); // Normal iş günüdürsə -> Dünənə get

            string prevUrl = $"https://www.cbar.az/currencies/{prevDate:dd.MM.yyyy}.xml";

            try
            {
                // 2. Həm bugünkü, həm də əvvəlki günün məlumatlarını eyni anda çəkirik
                var todayTask = _httpClient.GetStringAsync(todayUrl);
                var prevTask = _httpClient.GetStringAsync(prevUrl);

                await Task.WhenAll(todayTask, prevTask);

                XDocument todayDoc = XDocument.Parse(await todayTask);
                XDocument prevDoc = XDocument.Parse(await prevTask);

                string[] targetCurrencies = { "USD", "EUR", "RUB", "GBP", "TRY" };

                var valutesToday = todayDoc.Descendants("Valute")
                                           .Where(x => targetCurrencies.Contains(x.Attribute("Code")?.Value))
                                           .ToList();

                // 3. Modeli hər iki məlumatla doldururuq
                foreach (var val in valutesToday)
                {
                    string code = val.Attribute("Code")!.Value;
                    decimal rate = Convert.ToDecimal(val.Element("Value")!.Value, CultureInfo.InvariantCulture);

                    // Əvvəlki günün XML-dən eyni valyutanı tapırıq
                    var prevVal = prevDoc.Descendants("Valute").FirstOrDefault(x => x.Attribute("Code")?.Value == code);
                    decimal prevRate = rate; // Default olaraq bu günə bərabər edirik (birdən dünənki tapılmazsa xəta verməsin)

                    if (prevVal != null)
                    {
                        prevRate = Convert.ToDecimal(prevVal.Element("Value")!.Value, CultureInfo.InvariantCulture);
                    }

                    rates.Add(new CurrencyRateVM
                    {
                        Code = code,
                        Rate = rate,
                        PreviousRate = prevRate, // YENİ: Dünənki kursu bura ötürürük
                        Symbol = GetCurrencySymbol(code)
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Valyuta çəkilərkən xəta: {ex.Message}");

                // API işləməzsə və ya internet kəsilərsə, default dəyərlər (Vizual olaraq rəngləri görmək üçün bəzilərinə fərqli PreviousRate yazdım)
                rates.Add(new CurrencyRateVM { Code = "USD", Symbol = "$", Rate = 1.70M, PreviousRate = 1.70M }); // Boz olacaq (Dəyişməyib)
                rates.Add(new CurrencyRateVM { Code = "EUR", Symbol = "€", Rate = 1.85M, PreviousRate = 1.84M }); // Yaşıl olacaq (Artıb)
                rates.Add(new CurrencyRateVM { Code = "TRY", Symbol = "₺", Rate = 0.051M, PreviousRate = 0.052M }); // Qırmızı olacaq (Azalıb)
            }

            return rates;
        }

        private string GetCurrencySymbol(string code)
        {
            return code switch
            {
                "USD" => "$",
                "EUR" => "€",
                "RUB" => "₽",
                "GBP" => "£",
                "TRY" => "₺",
                _ => code
            };
        }
    }
}