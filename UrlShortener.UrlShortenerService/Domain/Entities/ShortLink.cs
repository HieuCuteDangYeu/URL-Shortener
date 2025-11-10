using System.ComponentModel.DataAnnotations;

namespace UrlShortener.UrlShortenerService.Domain.Entities
{
    public class ShortLink
    {
        [Key]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Short code is required")]
        [StringLength(10, MinimumLength = 3, ErrorMessage = "Short code must be between 3 and 10 characters")]
        [RegularExpression("^[a-zA-Z0-9_-]+$", ErrorMessage = "Short code can only contain letters, numbers, underscores, and hyphens")]
        public string ShortCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Original URL is required")]
        [Url(ErrorMessage = "Original URL must be a valid URL")]
        [StringLength(2048, ErrorMessage = "Original URL is too long")]
        public string OriginalUrl { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Range(0, int.MaxValue, ErrorMessage = "Click count cannot be negative")]
        public int ClickCount { get; set; } = 0;
    }
}
