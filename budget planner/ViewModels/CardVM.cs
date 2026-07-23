namespace budget_planner.ViewModels
{
    public class CardVM
    {
        public int Id { get; set; }
        public string CardName { get; set; } = null!;
        public string Last4Digits { get; set; } = null!;
        public string Currency { get; set; } = "AZN";
        public decimal Balance { get; set; }
    }
}