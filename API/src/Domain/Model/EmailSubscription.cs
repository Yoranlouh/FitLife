    using System.ComponentModel.DataAnnotations.Schema;

    namespace API.Domain.Model;

    public class EmailSubscription
    {
        [Column("id")] public int Id { get; init; }
        [Column("email")] public required string Email { get; init; }
    }