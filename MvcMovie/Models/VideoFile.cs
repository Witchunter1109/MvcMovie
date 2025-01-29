using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MvcMovie.Models;

public class VideoFile
{
    public int Id { get; set; }

    [Required]
    public string FilePath { get; set; } = string.Empty;

    [Required]
    public int MovieId { get; set; }

    [ForeignKey("MovieId")]
    public Movie? Movie { get; set; }
    public string FileName { get; internal set; }
}
