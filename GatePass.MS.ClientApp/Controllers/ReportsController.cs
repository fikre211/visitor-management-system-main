using GatePass.MS.Application;
using GatePass.MS.ClientApp.Data;
using GatePass.MS.ClientApp.Service;
using GatePass.MS.Domain;
using GatePass.MS.Domain.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GatePass.MS.ClientApp.Controllers
{
    public class ReportsController : Controller
    {
        private readonly ICurrentCompany _current;
        private readonly ReportService _reportService;
        private readonly UserActivityService _activityService;
        private readonly GuestActivityService _guestActivityService;

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public ReportsController(ReportService reportService,ApplicationDbContext context, UserActivityService activityService, GuestActivityService guestActivityService, ICurrentCompany current,UserManager<ApplicationUser> userManager)
        {
            _reportService = reportService;
            _activityService = activityService;
            _guestActivityService = guestActivityService;
            _userManager = userManager;
            _context = context;
            _current = current;
        }
        [HttpGet]
        public async Task<IActionResult> UserActivityReport( DateTime? startDate, DateTime? endDate, string? SelectedUserId)
        {
            var model = await _activityService.GetUserActivitiesAsync(startDate, endDate, SelectedUserId);
            return View(model);
        }
        [HttpGet]
        [HttpGet]
        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> GuestActivityReport(
    DateTime? startDate,
    DateTime? endDate,
    int? SelectedGuestId,
    string? activityType)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var model = await _guestActivityService.GetGuestActivitiesAsync(
                startDate,
                endDate,
                SelectedGuestId,
                activityType,
                userId
            );

            return View(model);
        }



        public IActionResult VisitReport(DateTime? startDate, DateTime? endDate, string? status, int? employeeId)
        {
            // Fetch reports based on filters
            var reportDtos = _reportService.GetVisitReports(User, startDate, endDate, status, employeeId);

            // Load the current user
            var currentUser = _userManager.GetUserAsync(User).Result;
            if (currentUser == null)
            {
                return NotFound();
            }

            _context.Entry(currentUser).Reference(x => x.Employee).Load();
            int? currentUserDepartmentId = currentUser?.Employee?.DepartmentId;

            // Check if supervisor
            var isInSupervisorRole = _userManager.IsInRoleAsync(currentUser, "Superviser").Result;
            var isInAdminRole = _userManager.IsInRoleAsync(currentUser, "Admin").Result;
            // Fetch all employees of the current company
            var allEmployees = _reportService.GetAllEmployees()
                                             .Where(e => e.CompanyId == _current.Value.Id)
                                             .ToList();


            // Apply role-based filtering
            var employees = isInSupervisorRole
                ? allEmployees.Where(e => e.DepartmentId == currentUserDepartmentId).ToList()
                : isInAdminRole
                    ? allEmployees.ToList()
                    : allEmployees.Where(e => e.Id == currentUser.EmployeeId).ToList();

            // Create ViewModel
            var model = new VisitReportModel
            {
                StartDate = startDate,
                EndDate = endDate,
                Status = status,
                EmployeeId = employeeId,
                Reports = reportDtos,
                Employees = employees
            };

            return View(model);
        }


    }

}
