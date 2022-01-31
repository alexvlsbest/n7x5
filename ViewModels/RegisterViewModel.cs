using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace N7.ViewModels
{
    public class RegisterViewModel
    {
        
        [Required]
        [Display(Name = "Username")]
        public string UserName { get; set; }

        [Required]
        [Display(Name = "E-mail")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Required]
        [Compare("Password", ErrorMessage = "PasswordsAreNotSame")]
        [DataType(DataType.Password)]
        [Display(Name = "ConfirmPassword")]
        public string PasswordConfirm { get; set; }

        public bool Blocked { get; set; }
        [DataType(DataType.DateTime)]
        public DateTime RegistrationDate { get; set; }
        
        [DataType(DataType.DateTime)]
        public DateTime LastLoginDate { get; set; }

    }
}
