// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using GatePass.MS.Domain;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using GatePass.MS.ClientApp.Data;
using GatePass.MS.ClientApp.Service;
using ZXing.QrCode.Internal;


namespace GatePass.MS.ClientApp.Areas.Identity.Pages.Account
{[ResponseCache(NoStore = true, Duration = 0, Location = ResponseCacheLocation.None)]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly ICurrentCompany _current;

        private readonly ApplicationDbContext _context;
        private readonly UserActivityService _userActivity;
        public LoginModel(SignInManager<ApplicationUser> signInManager, ILogger<LoginModel> logger, ICurrentCompany current, ApplicationDbContext context, UserActivityService userActivity, UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _logger = logger;
            _current = current;
            _context = context;
            _userManager = userManager;
            _userActivity = userActivity;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from
            ///     




            /// </summary>
            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string returnUrl = null)
        {
            var companyName = (string)RouteData.Values["companyName"];
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            if (User.Identity.IsAuthenticated)
            {
                // Redirect to the right company-specific home
                return Redirect($"/{companyName}");
            }

            // If returnUrl is null, default it
            returnUrl ??= $"/{companyName}";

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            ReturnUrl = returnUrl;
            return Page();
        }
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            var companyName = (string)RouteData.Values["companyName"];

            // If returnUrl is null, build the default
            returnUrl ??= $"/{companyName}";

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(Input?.Email))
            {
                // Do NOT try to find user when Email is missing.
                return Page();
            }

            var user = await _signInManager.UserManager.FindByEmailAsync(Input.Email);




            if (ModelState.IsValid)
            {
                if (user != null && !await _signInManager.UserManager.IsEmailConfirmedAsync(user))
                {
                    ModelState.AddModelError(string.Empty, "Your email address has not been confirmed. Please check your email for the confirmation link.");
                    return Page();
                }

                if (user != null)
                {
                    if (!user.IsActive)
                    {
                        ModelState.AddModelError(string.Empty, "Your account is inactive. Please contact the administrator.");
                        return Page();
                    }

                    if (user.IsLocked)
                    {
                        ModelState.AddModelError(string.Empty, "Your account is locked. Please contact the administrator.");
                        return RedirectToPage("./Lockout");
                    }
                }


                if (Input.Email == "superadmin@mintvms.com")
                {

                    var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User logged in.");
                        return LocalRedirect(returnUrl);
                    }
                }
                if (user != null)
                {
                    var employee = await _context.Employee
                        .Include(e => e.Department) // Make sure Department is loaded
                        .FirstOrDefaultAsync(e => e.Email == Input.Email);

                    if (employee == null)
                    {
                        ModelState.AddModelError(string.Empty, "Employee record not found.");
                        return Page();
                    }

                    if (employee.Department == null)
                    {
                        ModelState.AddModelError(string.Empty, "Employee is not associated with any department.");
                        return Page();
                    }

                    if (_current?.Value == null)
                    {
                        ModelState.AddModelError(string.Empty, "Company context is not available.");
                        return Page();
                    }

                    if (employee.Department.CompanyId == _current.Value.Id)
                    {
                        var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);

                        if (result.Succeeded)
                        {
                            _logger.LogInformation("User logged in.");
                            return LocalRedirect(returnUrl);
                        }

                        if (result.RequiresTwoFactor)
                        {
                            return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                        }
                    }
                }



                if (user != null)
                {
                    user.AccessFailedCount += 1;
                    await _signInManager.UserManager.UpdateAsync(user);

                    var maxFailedAttempts = _signInManager.Options.Lockout.MaxFailedAccessAttempts; // You can set your own threshold here
                    if (user.AccessFailedCount >= maxFailedAttempts)
                    {
                        user.IsLocked = true;
                        await _signInManager.UserManager.UpdateAsync(user);
                        //Log activity
                        await _userActivity.LogActivityAsync(user.Id, "Account Lock", "User Account locked due to many failed login attempt.");

                        ModelState.AddModelError(string.Empty, "Your account has been locked due to too many failed login attempts.");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, $"Invalid login attempt. You have {maxFailedAttempts - user.AccessFailedCount} attempt(s) left before your account is locked.");
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt. User does not exist.");
                }
                return Page();

            }
            return Page();
        }

    }
}