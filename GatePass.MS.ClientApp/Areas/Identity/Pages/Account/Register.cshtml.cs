// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using GatePass.MS.ClientApp.Data;
using GatePass.MS.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using GatePass.MS.ClientApp.Service;

namespace GatePass.MS.ClientApp.Areas.Identity.Pages.Account
{
    [Authorize(Roles = "Admin")]
    public class RegisterModel : PageModel

    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly Controllers.IEmailSender _emailSender;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly UserActivityService _userActivity;
        private readonly ICurrentCompany _current;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            Controllers.IEmailSender emailSender,
                   ICurrentCompany current,

            UserActivityService userActivity)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _current = current;

            _context = context;
            _emailSender = emailSender;
            _roleManager = roleManager;
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
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

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
            /// 
            [Required(ErrorMessage = "Please select an employee")]
            [Display(Name = "Employee")]
            public int EmployeeId { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            public List<string> SelectedRoles { get; set; } = new List<string>();

            public IEnumerable<SelectListItem> RolesList { get; set; }


            public string UserName { get; set; }


            [DataType(DataType.EmailAddress)]
            [Display(Name = "Email")]
            public string Email { get; set; }




            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }



            //Additional information If the user to be register is guest
            public string FirstName { get; set; }
            public string LastName { get; set; }
            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; }
            public string ProfilePicture { get; set; }


        }


        public async Task<IActionResult> OnGetAsync(int? employeeId, string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            var user = await _userManager.GetUserAsync(User);
            if (employeeId == null)
            {
                Input = new InputModel
                {
                    // Initialize your InputModel properties if needed
                };
            }
            else
            {
                var myemployee = await _context.Employee.FindAsync(employeeId);
                if (myemployee == null)
                {
                    return RedirectToPage("/Employees/Index"); // Ensure redirection is returned properly
                }

                // Check if the user already exists
                var existingUser = await _userManager.FindByNameAsync(myemployee.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError(string.Empty, "This Employee Already has Account!");
                    TempData["message"] = " This Employee Already has Account!";
                    TempData["MessageType"] = "info";
                    return RedirectToAction("EmployeeList", "Admin"); // Redirect to the EmployeeList action in Admin controller
                }
                // If you reach this line it means  Employee has no account
                Input = new InputModel
                {
                    RolesList = _roleManager.Roles.Select(x => x.Name).Select(i => new SelectListItem
                    {
                        Text = i,
                        Value = i
                    }),
                    EmployeeId = myemployee.Id,
                    UserName = myemployee.Email,
                };
            }

            return Page();
        }
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            var companyName = (string)RouteData.Values["companyName"];

            returnUrl ??= Url.Content($"/{companyName}");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var user = CreateUser();

                if (Input?.EmployeeId == 0)//if it is not register Employee create guest Account
                {
                    var guest = new Guest
                    {
                        FirstName = Input.FirstName,
                        LastName=Input.LastName,
                       
                    };
                    _context.Guest.Add(guest);
                    await _context.SaveChangesAsync();
                    user.GuestId= guest.Id;
                    user.PhoneNumber= Input.PhoneNumber;
                   

                    // Set the username (typically the email address)
                    await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                    // Set the email address
                    await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
                    // FORCE email confirmed for all new accounts
                    user.EmailConfirmed = true;

                }
                else //if it is registered Employee and need to create gpass account create account using registerd email in Employee table
                {
                    // Retrieve the employee from the database based on the provided EmployeeId
                    user.EmployeeId = Input.EmployeeId; // Set the EmployeeId
                    user.CompanyId = _current.Value.Id;
                    var employee = await _context.Employee.FindAsync(Input.EmployeeId);
                    if (Input.ProfilePicture != null)
                    {
                        user.UserPhotoPath = Input.ProfilePicture;
                    }
                    else
                    {
                        user.UserPhotoPath = "img/hero.jpg";
                    }
                    // Check if the employee exists and if so, set the email of the user to the email of the employee
                    if (employee != null)
                    {
                        user.Email = employee.Email;

                        user.EmailConfirmed = true;
                        // Set the username (typically the email address)
                        await _userStore.SetUserNameAsync(user, user.Email, CancellationToken.None);

                        // Set the email address
                        await _emailStore.SetEmailAsync(user, user.Email, CancellationToken.None);
                    }
                    else
                    {
                        // Handle the case where the employee does not exist or the email is not available
                        ModelState.AddModelError(string.Empty, "Employee not found or email not available.");
                        return Page();
                    }
                }



                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");
                    TempData["message"] = " User account created successfully!";
                    TempData["MessageType"] = "success";

                    if (Input.EmployeeId == 0)//if it is Guest
                    {
                        // Check if the Guest role exists, and create it if not
                        if (!await _roleManager.RoleExistsAsync("Guest"))
                        {
                            await _roleManager.CreateAsync(new IdentityRole("Guest"));
                            _logger.LogInformation("Guest role created");
                        }
                        await _userManager.AddToRoleAsync(user, "Guest");
                        _logger.LogInformation("Guest user created and added to Guest role");
                    }
                    else // if it is not guest
                    {
                        foreach (var role in Input.SelectedRoles)
                        {
                            await _userManager.AddToRoleAsync(user, role);
                        }
                    }

                    var currentUser = await _userManager.GetUserAsync(User);
                    //Log activity
                    await _userActivity.LogActivityAsync(currentUser.Id, "Register user", $"User register new account wth username:{user.UserName}");

                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);
                    try
                    {
                        await _emailSender.SendEmailAsync(user.Email, "Action Required: Confirm Your Email",
                        $@"
    <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #e0f7fa; padding: 20px; border-radius: 10px;'>
        <h2 style='color: #00796b;'>Welcome to MinT Ethiopia!</h2>
        <p>We're excited to have you on board. To get started, please confirm your email address by clicking the button below:</p>
        <a href='{HtmlEncoder.Default.Encode(callbackUrl)}' style='display: inline-block; background-color: #00796b; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Confirm Email</a>
        <p>If you have any questions or need assistance, feel free to reach out to our support team.</p>
        <p>Best regards,<br>MINT Ethiopia Team</p>
    </div>");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"General Exception: {ex.Message}");
                    }

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return LocalRedirect(returnUrl);
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}
