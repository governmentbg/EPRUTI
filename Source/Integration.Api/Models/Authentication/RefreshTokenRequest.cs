namespace Integration.Api.Models.Authentication
{
    using System.ComponentModel.DataAnnotations;

    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; }
    }
}
