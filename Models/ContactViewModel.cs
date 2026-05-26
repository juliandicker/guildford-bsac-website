namespace GuildfordBsac.Web.Models
{
    using System.ComponentModel.DataAnnotations;

    public class ContactViewModel
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "Your name", Prompt = "Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(254)]
        [Display(Name = "Your email", Prompt = "Email")]
        [EmailAddress]
        // Intentionally not named "email" to avoid bot autofill on the real field
        public string Emaily { get; set; } = string.Empty;

        // Honeypot: must remain empty. Bots fill it in, humans don't see it.
        public string? Emailx { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Subject", Prompt = "Subject")]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [StringLength(5000)]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Message", Prompt = "Message")]
        public string Message { get; set; } = string.Empty;
    }
}