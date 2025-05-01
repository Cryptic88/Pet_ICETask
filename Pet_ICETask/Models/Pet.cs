using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pet_ICETask.Models
{
    public class Pet
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }

        public int Age { get; set; }

        public string Breed { get; set; }

        [Column("adoption_status")]
        public string? AdoptionStatus { get; set; }

        public string? ImageUrl { get; set; }
    }
}
