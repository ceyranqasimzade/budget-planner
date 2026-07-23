using System.ComponentModel.DataAnnotations;

namespace budget_planner.Models
{
    public class Card : BaseModel
    {
        [Required(ErrorMessage = "Kart adı daxil edilməlidir")]
        public string CardName { get; set; } = null!;



        [Required(ErrorMessage = "Son 4 rəqəm daxil edilməlidir")]
        [StringLength(4, MinimumLength = 4,
            ErrorMessage = "Kartın son 4 rəqəmi daxil edilməlidir")]
        public string Last4Digits { get; set; } = null!;



        [Range(0, double.MaxValue,
            ErrorMessage = "Balans mənfi ola bilməz")]
        public decimal Balance { get; set; }



        public string Currency { get; set; } = "AZN";



        public string UserId { get; set; } = null!;


        public ApplicationUser User { get; set; } = null!;



        // Bir kartın çoxlu transaction-u ola bilər
        public ICollection<Transaction> Transactions { get; set; }
            = new List<Transaction>();
    }
}