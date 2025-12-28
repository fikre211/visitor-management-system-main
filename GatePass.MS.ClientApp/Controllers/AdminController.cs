using ExcelDataReader;
using GatePass.MS.ClientApp.Data;
using GatePass.MS.ClientApp.Service;
using GatePass.MS.Domain;
using GatePass.MS.Domain.ViewModels;
using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace GatePass.MS.ClientApp.Controllers
{

    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {

        private readonly ISettingService _isettingService;
        private readonly Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly UserActivityService _activityService;
        private readonly Microsoft.AspNetCore.Identity.RoleManager<IdentityRole> _roleManager;
        private readonly ICurrentCompany _current;

        private readonly UserActivityService _userActivityService;

        public AdminController(ISettingService isettingService, ICurrentCompany current,
            Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager,
            UserActivityService userActivityService,
                        Microsoft.AspNetCore.Identity.RoleManager<IdentityRole> roleManager,

            ApplicationDbContext context, UserActivityService activityService
            )
        {
            _isettingService = isettingService;
            _userManager = userManager;
            _userActivityService = userActivityService;
            _roleManager = roleManager;
            _current = current;

            _context = context;
            _activityService = activityService;
        }
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users
                .Where(u=>u.CompanyId==_current.Value.Id)
                .Include(u => u.Employee)
                .ThenInclude(d => d.Department)
                .Include(g => g.Guest)
                .ToListAsync();

            var userViewModels = new List<ViewUserViewModel>();

            foreach (var user in users)
            {
                bool isLockedOut = await _userManager.IsLockedOutAsync(user);
                var roles = await _userManager.GetRolesAsync(user);
                userViewModels.Add(new ViewUserViewModel
                {
                    Id = user.Id,
                    Department = user.Employee?.Department.Name ?? "IT",
                    UserName = user.UserName,
                    DepartmentSuperviser = "saboka",
                    Roles = roles.ToList() // Store all roles

                });
            }
            return View(userViewModels);

        }



        public async Task<IActionResult> UserDetails(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("User ID is required.");
            }

            var activities = await _activityService.GetUserActivitiesByIdAsync(id);
            ViewBag.activities = activities;
            var user = await _userManager.Users
                .Include(u => u.Employee)
                    .ThenInclude(e => e.Department)
                .Include(u => u.Guest)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            bool isLockedOut = await _userManager.IsLockedOutAsync(user);
            var userDetailsViewModel = new ViewUserViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Department = user.Employee?.Department?.Name ?? "Guest",
                Roles = roles.ToList(),
                IsLocked = user.IsLocked,
                IsActive = user.IsActive
            };

            return View(userDetailsViewModel);
        }

        private bool EmployeeExists(int id)
        {
            return _context.Employee
                .Any(e => e.Id == id);
        }
        [HttpPost]
        [HttpGet]
        public async Task<IActionResult> EditUserRole(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user); // Get user's current roles
            var allRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync(); // Get all roles

            var model = new EditUserRoleViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                CurrentRoles = roles.ToList(),
                AvailableRoles = allRoles
            };

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> UpdateUserRole(string userId, string newRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles); // Remove existing roles

            var result = await _userManager.AddToRoleAsync(user, newRole); // Assign new role
            if (result.Succeeded)
            {
                TempData["message"] = "user role updated successfully!";
                TempData["MessageType"] = "success";
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", "Failed to update role.");
            return View();
        }

        // GET
        public async Task<IActionResult> UpdateSetting()
        {
            var appSettings = new SettingViewModel
            {
                AppName = await _isettingService.GetSettingValueAsync("AppName"),
                MaxLoginAttempts = await _isettingService.GetSettingValueAsync("MaxFailedAccessAttempts"),

            };
            ViewData["AppName"] = appSettings;
            return View(appSettings);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSetting(SettingViewModel model)
        {
            try
            {
                await _isettingService.UpdateSettingAsync("AppName", model.AppName);
                await _isettingService.UpdateSettingAsync("MaxFailedAccessAttempts", model.MaxLoginAttempts);
                // Log the activity asynchronously
                var userId = _userManager.GetUserId(User);
                await _activityService.LogActivityAsync(userId, "Update", $"System information updated.");

                TempData["message"] = "Setting is sent successfully!";
                TempData["MessageType"] = "success";
            }
            catch (Exception ex)
            {

                Console.Error.WriteLine($"Error updating settings: {ex.Message}");

                TempData["message"] = "There was an error updating the settings. Please try again later.";
                TempData["MessageType"] = "error";
            }

            return View();
        }




        [HttpPost]
        public async Task<IActionResult> ActivateUser(ViewUserViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return NotFound();
            }

            // Set the user as active
            user.IsActive = true;
            // Update the user in the database
            await _userManager.UpdateAsync(user);
            // Optionally send a notification to the user about their account activation
            // await _emailSender.SendEmailAsync(user.Email, "Account Activated", "Your account has been activated.");
            // Log the activity asynchronously
            var userId = _userManager.GetUserId(User);
            await _activityService.LogActivityAsync(userId, "Activate", $"Account {user.UserName} Activated.");

            return RedirectToAction(nameof(UserDetails), new { model.Id });
        }
        [HttpPost]
        public async Task<IActionResult> DeactivateUser(ViewUserViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return NotFound();
            }

            // Set the user as inactive
            user.IsActive = false;


            // Update the user in the database
            await _userManager.UpdateAsync(user);

            // Optionally send a notification to the user about their account deactivation
            // await _emailSender.SendEmailAsync(user.Email, "Account Deactivated", "Your account has been deactivated.");
            var userId = _userManager.GetUserId(User);
            await _activityService.LogActivityAsync(userId, "Deactivate", $"Account {user.UserName} Deactivated.");

            return RedirectToAction(nameof(UserDetails), new { model.Id });
        }

        [HttpPost]
        public async Task<IActionResult> UnlockUser(ViewUserViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return NotFound();
            }

            // Unlock the user
            user.IsLocked = false;
            // Reset failed attempts
            user.AccessFailedCount = 0;
            // Update the user in the database
            await _userManager.UpdateAsync(user);
            // Optionally send a notification to the user about their account unlock
            // await _emailSender.SendEmailAsync(user.Email, "Account Unlocked", "Your account has been unlocked.");
            var userId = _userManager.GetUserId(User);
            await _activityService.LogActivityAsync(userId, "Unlock", $"Account {user.UserName} Unlocked.");

            return RedirectToAction(nameof(UserDetails), new { model.Id });
        }


        // GET: Employees
        public async Task<IActionResult> EmployeeList()
        {
            var employeeList = await _context.Employee
                .Where(e=>e.CompanyId==_current.Value.Id)
                .Include(e => e.Department).Include(e => e.Designation).ToListAsync();
            return View(employeeList);
        }
        // GET: Locked Account
        public async Task<IActionResult> ViewLockedAccount()
        {
            // Determine current company from injected ICurrentCompany
            var companyId = _current?.Value?.Id;

            // Base query for locked users including related data
            var query = _userManager.Users
                .Include(u => u.Employee)
                .ThenInclude(d => d.Department)
                .Include(g => g.Guest)
                .Where(u => u.IsLocked);

            // Restrict to current company if available
            if (companyId.HasValue)
            {
                query = query.Where(u => u.CompanyId == companyId.Value);
            }

            var users = await query.ToListAsync();

            var userViewModels = new List<ViewUserViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userViewModels.Add(new ViewUserViewModel
                {
                    Id = user.Id,
                    Department = user.Employee?.Department?.Name ?? "IT",
                    UserName = user.UserName,
                    DepartmentSuperviser = "saboka",
                    Roles = roles.ToList(),
                    IsLocked = user.IsLocked,
                    IsActive = user.IsActive
                });
            }

            return View(userViewModels);
        }

        // Action on Request Information start from this line
        [HttpGet]
        public async Task<IActionResult> RequestTobeDeleted()
        {
            var today = DateTime.Today;

            var requests = await _context.RequestInformation
                .Where(r => r.CompanyId == _current.Value.Id // Filter by current company
                            && r.VisitDateTimeStart < today
                            && r.Status == "Pending"
                            && !r.Deleted)
                .Include(r => r.Guest)
                .ToListAsync();
            return View(requests);
        }

        public async Task<IActionResult> DeleteOutDatedRequest(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var request = await _context.RequestInformation.FirstOrDefaultAsync(m => m.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            return PartialView("_DeleteOutDatedRequest", request);
        }
        public async Task<IActionResult> DeleteOutDatedRequestConfirmed(int id)
        {

            var request = await _context.RequestInformation.FindAsync(id);
            if (request != null)
            {
                request.Deleted = true; // Mark as deleted
                _context.Update(request);
                await _context.SaveChangesAsync();
                TempData["message"] = "Request Deleted successfully!";
                TempData["MessageType"] = "success";
                var userId = _userManager.GetUserId(User);
                await _activityService.LogActivityAsync(userId, "Delete", $"Request of Id: {request.Id} Deleted.");

            }


            return RedirectToAction(nameof(RequestTobeDeleted));
        }

        // Action on Request Information End at this line

        public IActionResult UploadExcel()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadExcel(IFormFile file)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (file != null && file.Length > 0)
            {
                var uploadsFolder = $"{Directory.GetCurrentDirectory()}\\wwwroot\\uploads\\";

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var filePath = Path.Combine(uploadsFolder, file.FileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                using (var stream = System.IO.File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        do
                        {
                            bool isHeaderSkipped = false;

                            while (reader.Read())
                            {
                                if (!isHeaderSkipped)
                                {
                                    isHeaderSkipped = true;
                                    continue;
                                }

                                Employee e = new Employee();
                                string email= reader.GetValue(3).ToString();
                                bool emailExists = await _context.Employee.AnyAsync(e => e.Email == email);
                                if (!emailExists)
                                {
                                    e.FirstName = reader.GetValue(1).ToString();
                                    e.LastName = reader.GetValue(2).ToString();
                                    e.Email= email;
                                    e.Phone = reader.GetValue(4).ToString();
                                    e.Gender = reader.GetValue(5).ToString();
                                    e.Address = reader.GetValue(6).ToString();
                                    e.Age = Convert.ToInt32(reader.GetValue(7).ToString());
                                    e.DepartmentId = Convert.ToInt32(reader.GetValue(8).ToString());
                                    e.DesignationId = Convert.ToInt32(reader.GetValue(9).ToString());

                                    _context.Add(e);
                                    await _context.SaveChangesAsync();
                                }

                                
                            }
                        } while (reader.NextResult());
                        TempData["message"] = "uploaded successfully!";
                        TempData["MessageType"] = "success";
                    }
                }
            }
            else
            {
                TempData["message"] = " file is empty!";
                TempData["MessageType"] = "error";
            }
            return RedirectToAction(nameof(EmployeeList));
        }



    }
}
