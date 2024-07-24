using System.ComponentModel.DataAnnotations;

namespace Blog.ViewModels.Accounts
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Indique o E-mail.")]
        [EmailAddress(ErrorMessage = "E-mail inválido!")] //valida se o email é válido
        public string Email { get; set; }

        [Required(ErrorMessage = "Indique a password.")]
        public string Password { get; set; }
    }
}
