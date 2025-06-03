namespace WebApi.Model.Authentication
{
    using System.ComponentModel.DataAnnotations;

    public class SignInRequest
    {
        [Required]
        [StringLength(100)]
        public string UserName { get; set; }

        [Required]
        [StringLength(250)]
        public string Password { get; set; }
    }
}
