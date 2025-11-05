using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Core.Entities
{
    public class ShortLink
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(2048)]
        [Url]
        public string OriginalUrl { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string ShortCode { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int Clicks { get; set; } = 0;
    }
}
