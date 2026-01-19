namespace HMS.Web.Models
{
    public class LossStat
    {
        public string Reason { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalValue { get; set; }
    }
}
