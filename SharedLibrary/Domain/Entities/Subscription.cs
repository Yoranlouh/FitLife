using System.ComponentModel.DataAnnotations;

namespace SharedLibrary.Domain.Entities
{
    public class Subscription
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        [Required]
        public decimal Price { get; set; }

        public int DurationInMonths { get; set; }

        // Relaties
        public ICollection<Member> Members { get; set; } = new List<Member>();
    }
}
