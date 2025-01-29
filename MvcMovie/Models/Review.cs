using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MvcMovie.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; } // Ocena 1-5

        public string Comment { get; set; }

        public int MovieId { get; set; }
        public Movie Movie { get; set; }
    }
}
