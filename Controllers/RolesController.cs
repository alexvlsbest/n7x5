using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using N7.Models;
using N7.ViewModels;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore.Authorization;

namespace N7.Controllers

{
    [Authorize(Roles = "Admin")]
    public class RolesController : Controller
    {
        RoleManager<IdentityRole> _roleManager;
        UserManager<AppUser> _userManager;
        SignInManager<AppUser> _signInManager;
        public RolesController(RoleManager<IdentityRole> roleManager, UserManager<AppUser> userManager,
                                SignInManager<AppUser> signInManager)
        {
            _signInManager = signInManager;
            _roleManager = roleManager;
            _userManager = userManager;
        }

        private bool LogoutIfBlocked()
        {
            bool y = false;
            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    if (_userManager.Users.ToList()
                                .Find(x => x.UserName == User.Identity.Name) == null)
                        y = true;

                    foreach (var user in _userManager.Users)
                    {
                        if (user.UserName == User.Identity.Name)
                        {
                            if (user.Blocked)
                                y = true;
                            break;
                        }
                    }
                    if (y)
                    {
                        _signInManager.SignOutAsync();
                    }
                }
            }
            catch { }
            return y;
        }
        
        public IActionResult Index()
        {
            if (LogoutIfBlocked())
                return RedirectToAction("Login", "Account");
            return View(_roleManager.Roles.ToList()); 
        }

        public IActionResult Create()
        {
            if (LogoutIfBlocked())
                return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(string name)
        {
            if (LogoutIfBlocked())
                return RedirectToAction("Login", "Account");
            if (!string.IsNullOrEmpty(name))
            {
                IdentityResult result = await _roleManager.CreateAsync(new IdentityRole(name));
                if (result.Succeeded)
                {
                    return RedirectToAction("Index");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }
            return View(name);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            if (LogoutIfBlocked())
                return RedirectToAction("Login", "Account");
            IdentityRole role = await _roleManager.FindByIdAsync(id);
            if (role != null)
            {
                IdentityResult result = await _roleManager.DeleteAsync(role);
            }
            return RedirectToAction("Index");
        }

        public IActionResult UserList()
        {
            if (LogoutIfBlocked())
                return RedirectToAction("Login", "Account");
            return View(_userManager.Users.ToList());
        }

        public async Task<IActionResult> Edit(string userId)
        {
            if (LogoutIfBlocked())
                return RedirectToAction("Login", "Account");

            AppUser user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                
                var userRoles = await _userManager.GetRolesAsync(user);
                var allRoles = _roleManager.Roles.ToList();
                ChangeRoleViewModel model = new ChangeRoleViewModel
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    UserRoles = userRoles,
                    AllRoles = allRoles
                };
                return View(model);
            }

            return NotFound();
        }
        [HttpPost]
        public async Task<IActionResult> Edit(string userId, List<string> roles)
        {
            if (LogoutIfBlocked())
                return RedirectToAction("Login", "Account");
            
            AppUser user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {                
                var userRoles = await _userManager.GetRolesAsync(user);            
                var allRoles = _roleManager.Roles.ToList();           
                var addedRoles = roles.Except(userRoles);
                var removedRoles = userRoles.Except(roles);
                await _userManager.AddToRolesAsync(user, addedRoles);
                await _userManager.RemoveFromRolesAsync(user, removedRoles);
                return RedirectToAction("UserList");
            }
            return NotFound();
        }
    }
}

