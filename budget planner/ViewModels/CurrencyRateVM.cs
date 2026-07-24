namespace budget_planner.ViewModels
{
    public class CurrencyRateVM
    {
        public string Code { get; set; } = string.Empty;     // Məsələn: "USD", "EUR"
        public string Symbol { get; set; } = string.Empty;   // Məsələn: "$", "€"
        public decimal Rate { get; set; }                    // Bu günün kursu (Məsələn: 1.7000)

        // --- YENİ ƏLAVƏ EDİLƏN HİSSƏLƏR ---

        public decimal PreviousRate { get; set; }            // Dünənin kursu (Məsələn: 1.6950)

        // Məzənnə fərqini avtomatik hesablayır (Riyazi olaraq: Bu gün - Dünən)
        public decimal Change => Rate - PreviousRate;        // Nəticə: +0.0050 və ya -0.0050

        // Məzənnənin qırmızı (azalıb) yoxsa yaşıl (artıb) olacağını təyin etmək üçün:
        public bool IsUp => Change > 0;                      // Sıfırdan böyükdürsə True (Yaşıl olacaq)
        public bool IsDown => Change < 0;                    // Sıfırdan kiçikdirsə True (Qırmızı olacaq)
    }
}