using GatePass.MS.ClientApp.Data;
using GatePass.MS.Domain;
using GatePass.MS.Domain.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.EntityFrameworkCore;
using System;

namespace GatePass.MS.ClientApp.Controllers
{
    public class DeligateRoleController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        public DeligateRoleController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context
            )
        {

            _userManager = userManager;
            _context = context;
            _roleManager = roleManager;
        }
        public async Task<IActionResult> Index()
        {
            // Retrieve the current user
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return NotFound();
            }

            // Load the employee information
            _context.Entry(currentUser).Reference(x => x.Employee).Load();
            int? currentUserDepartmentId = currentUser?.Employee?.DepartmentId;


            // Get users in the same department, excluding the current user and supervisors
            var usersInSameDepartment = await _userManager.Users
                .Where(u => u.Employee.DepartmentId == currentUserDepartmentId
                    && u.Id != currentUser.Id  // Exclude the current user
                   )
                .Include(u => u.Employee)
                .ToListAsync();

            // Retrieve all role names and store them in ViewBag
            var roleNames = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            ViewBag.role = "Superviser";

            // Initialize a list to hold user view models
            var userViewModels = new List<ViewUserViewModel>();

            // Iterate through each user
            foreach (var user in usersInSameDepartment)
            {
                // Retrieve roles for the current user
                var userRoles = await _userManager.GetRolesAsync(user);

                // Add user information to the view model list
                userViewModels.Add(new ViewUserViewModel
                {
                    Id = user.Id,
                    FullName = user.Employee?.FirstName + " " + user.Employee?.LastName,
                    UserName = user.UserName,
                    Roles = userRoles.ToList()
                });
            }

            // Return the view with user view models
            return View(userViewModels);
        }


        [HttpPost]
        public async Task<IActionResult> AssignRole(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || string.IsNullOrEmpty(roleName))
            {
                return NotFound();
            }

            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                TempData["message"] = "Role does not exist.";
                TempData["MessageType"] = "info";
                return RedirectToAction("Index");
            }

            var result = await _userManager.AddToRoleAsync(user, roleName);
            if (result.Succeeded)
            {
                TempData["message"] = "you deligate your role successfully.";
                TempData["MessageType"] = "success";
                return RedirectToAction("Index");

            }
            TempData["message"] = "User has superviser role already";
            TempData["MessageType"] = "info";
            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> RevokeRole(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || string.IsNullOrEmpty(roleName))
            {
                return NotFound();
            }

            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                return BadRequest("Role does not exist.");
            }

            // Get the list of roles assigned to the user
            var userRoles = await _userManager.GetRolesAsync(user);

            // Check if the user has more than one role
            if (userRoles.Count <= 1)
            {
                TempData["message"] = "User must have at least one role.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Index");
            }

            // Proceed to revoke the role
            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            if (result.Succeeded)
            {
                TempData["message"] = "Role revoked successfully.";
                TempData["MessageType"] = "success";
                return RedirectToAction("Index");
            }

            TempData["message"] = "Failed to revoke role.";
            TempData["MessageType"] = "error";
            return RedirectToAction("Index");
        }


    }
}
