using System.Collections.Generic;
using System.Threading.Tasks;
using budget_planner.ViewModels; // CurrencyRateVM buradadırsa

namespace budget_planner.Services
{
    public interface ICurrencyService
    {
        Task<List<CurrencyRateVM>> GetExchangeRatesAsync();
        Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency);
        string GetCurrencySymbol(string currencyCode);
    }
}