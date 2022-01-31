using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using N7.Models;
using N7.ViewModels;


namespace N7.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                AppUser user = new AppUser { UserName = model.UserName, Email = model.Email,
                                       Blocked = false, RegistrationDate = DateTime.Now,
                                       LastLoginDate = DateTime.Now };
                
                var result = await _userManager.CreateAsync(user, model.Password);

                await _userManager.AddToRoleAsync(user, "User");
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, false);
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            IdentityRole adminRole = await _roleManager.FindByNameAsync("Admin");
            if (adminRole==null)
            {
                await _roleManager.CreateAsync(new IdentityRole("Admin"));
            }
            IdentityRole userRole = await _roleManager.FindByNameAsync("User");
            if (userRole == null)
            {
                await _roleManager.CreateAsync(new IdentityRole("User"));
            }

            AppUser admin = await _userManager.FindByNameAsync("admin");
            if (admin==null)
            {
                admin = new AppUser
                {
                    UserName = "admin",
                    Email = "admin",
                    Blocked = false,
                    RegistrationDate = DateTime.Now,
                    LastLoginDate = DateTime.Now
                };
                await _userManager.CreateAsync(admin, "admin");
                await _userManager.AddToRoleAsync(admin, "Admin");
            }

            if (ModelState.IsValid)
            {
                var result =
                    await _signInManager.PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, false);

                AppUser user = await _userManager.FindByNameAsync(model.UserName);
                               
                if (result.Succeeded)
                {
                    user.LastLoginDate = DateTime.Now;
                    await _userManager.UpdateAsync(user); 
                    if (user.Blocked)
                     {
                        ModelState.AddModelError("", "You are locked out");
                        await _signInManager.SignOutAsync();
                     }
                     else
                     {
                          if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                          {
                                 return Redirect(model.ReturnUrl);
                          }
                          else
                          {
                               return RedirectToAction("Index", "Home");
                          }
                     }
                }
                else
                {
                        ModelState.AddModelError("", "Invalid login/password!");
                }
                
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {            
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
