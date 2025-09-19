using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using GatePass.MS.ClientApp.Data;
using GatePass.MS.ClientApp.Migrations;
using GatePass.MS.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GatePass.MS.ClientApp.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context

            )


        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;

        }
        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

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
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; }
            public string email { get; set; }
            public string firstname { get; set; }
            public string lastname { get; set; }
            public string gender { get; set; }
            public int age { get; set; }
            public string department { get; set; }
            public string designation { get; set; }
            public string address { get; set; }
            public string ProfilePicture { get; set; }


        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            var email = await _userManager.GetEmailAsync(user);
            Username = userName;
            var employee = await _context.Employee.FirstOrDefaultAsync(e => e.Email == email);
            if (employee != null)
            {
                var FirstName = employee.FirstName;
                var LastName = employee.LastName;
                var Phone = employee.Phone;
                var Gender = employee.Gender;
                var Address = employee.Address;
                var age = employee.Age;
                var departmentid = employee.DepartmentId;
                var designationid = employee.DesignationId;
                var department = await _context.Department.FirstOrDefaultAsync(d => d.Id == departmentid);
                var designation = await _context.Department.FirstOrDefaultAsync(d => d.Id == designationid);

                Input = new InputModel
                {
                    PhoneNumber = Phone,
                    email = email,
                    firstname = FirstName,
                    lastname = LastName,
                    age = age,
                    ProfilePicture=user.UserPhotoPath,
                    gender = Gender,
                    address = Address,
                    department = department != null ? department.Name : "Unknown",
                    designation = designation != null ? designation.Name : "Unknown",
                    //profilephoto = string.IsNullOrEmpty(user.UserPhotoPath) ? "img/hero.jpg": user.UserPhotoPath
                };
                // employeeName = emPosition;ployee.Name;
                // employeePosition = employee.
            }


        }
        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID .");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(IFormFile file)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID .");
            }

            //if (!ModelState.IsValid)
            //{
            //    await LoadAsync(user);
            //    return Page();
            //}

            

            var email = await _userManager.GetEmailAsync(user);
            var employee = await _context.Employee.FirstOrDefaultAsync(e => e.Email == email);
            if (employee != null)
            {
                employee.FirstName = Input.firstname;
                employee.LastName = Input.lastname;
                employee.Gender = Input.gender;
                employee.Age = Input.age;
                employee.Phone = Input.PhoneNumber;
                employee.Address = Input.address;

                _context.Employee.Update(employee);
                await _context.SaveChangesAsync();
                if (file != null )
                {
                    // Implement logic to save the uploaded file
                    // This example uses a folder named "uploads"
                    var uploadsFolder = $"{Directory.GetCurrentDirectory()}\\wwwroot\\img\\";
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    string FileName = file.FileName;
                    string filePath = Path.Combine(uploadsFolder, FileName);
                    try
                    {
                        await using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    // Update user profile picture path
                    user.UserPhotoPath = "/img/" + FileName;
                    await _context.SaveChangesAsync();
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        // Handle the exception (log it, show a message to the user, etc.)
                        return StatusCode(StatusCodes.Status500InternalServerError, "Access to the file path is denied.");
                    }
                    catch (Exception ex)
                    {
                        // Handle other potential exceptions
                        return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while uploading the file.");
                    }

                }
                // Update the employee entity in the database

            }
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                StatusMessage = "Failed to update user profile.";
                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["message"] = "Your profile has been updated successfully!";
            TempData["MessageType"] = "success";
            return RedirectToPage();
        }
    }

    //test


}