using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace N7.ViewModels
{
    public enum Theme
    {
        [Display(Name ="Black")] Black,
        [Display(Name ="White")] White
    }

    public class ThemeViewModel
    {
        public Theme CurrentTheme { get; set; }
    }
}
