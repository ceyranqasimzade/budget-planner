namespace budget_planner.Models
{
    public class BaseModel
    {
        public int Id { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? UpdatedDate { get; set; }

        public bool IsDeleted { get; set; }
    }
}