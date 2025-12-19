using GatePass.MS.ClientApp.Data;
using GatePass.MS.ClientApp.Models;
using GatePass.MS.ClientApp.Service;
using GatePass.MS.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Linq;
using Twilio.TwiML.Messaging;

namespace GatePass.MS.ClientApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ICurrentCompany _current;

        private readonly SignInManager<ApplicationUser> _signInManager;
        public HomeController(ApplicationDbContext context,
            ILogger<HomeController> logger ,
            ICurrentCompany current,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _current = current;
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
            
        }

        // GET: / 
        [HttpGet("")]
        public async Task<IActionResult> SelectCompany()
        {
            var today = DateTime.Today;

            // Total companies
            ViewBag.TotalCompanies = await _context.Company.CountAsync();

            // Today's visits (assuming VisitDateTimeStart is the visit date)
            ViewBag.TodaysVisits = await _context.RequestInformation
                .CountAsync(r => r.VisitDateTimeStart.Date == today && r.Status == "Approved");

            // All-time visits
            ViewBag.TotalVisits = await _context.RequestInformation
                .CountAsync(r => r.Status == "Approved");
            var companies = await _context.Company
                                 .ToListAsync();
            return View(companies);
        }
        public async Task<IActionResult> Index(string companyName)
        {
            if (_signInManager.IsSignedIn(User))
            {
                var today = DateTime.Today;
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return NotFound();
                }

                // Get company name for the current user
                var company = await _context.Company
                    .FirstOrDefaultAsync(c => c.Id == _current.Value.Id);

                if (company != null)
                {
                    ViewBag.CompanyName = company.Name;
                }

                // Get today's visitor count for the company
                ViewBag.TodaysVisitorCount = await _context.RequestInformation
                    .CountAsync(r => r.CompanyId == _current.Value.Id &&
                                        r.VisitDateTimeStart.Date == today &&
                                        r.Status == "Approved");

                // Get total visitor count for the company
                ViewBag.TotalVisitorCount = await _context.RequestInformation
                    .CountAsync(r => r.CompanyId == _current.Value.Id &&
                                        r.Status == "Approved");
                _context.Entry(currentUser).Reference(x => x.Employee).Load();
                var employeeId = currentUser.EmployeeId;
                var currentEmployee = _context.Employee
                    .Include(e => e.Department)
                    .SingleOrDefault(e => e.Id == employeeId);
                var departmentId = currentEmployee?.DepartmentId;
                var companyId = currentEmployee?.Department?.CompanyId;

                var isInSupervisorRole = await _userManager.IsInRoleAsync(currentUser, "Superviser");
                var isInGatekeeperRole = await _userManager.IsInRoleAsync(currentUser, "Gatekeeper");
                var isInAdminRole = await _userManager.IsInRoleAsync(currentUser, "Admin");
                var isInEmployeeRole = await _userManager.IsInRoleAsync(currentUser, "Employee");

                var employeeRole = await _context.Roles.SingleOrDefaultAsync(r => r.Name == "Employee");

                var query = _context.RequestInformation
                    .Where(r => r.CompanyId == _current.Value.Id)
                    .Include(r => r.Employee)
                        .ThenInclude(re => re.Department)
                    .AsQueryable();
                query = query.Where(r => ((currentUser.UserName == "superUser@gmail.com") || (isInAdminRole) || (r.Status == "Approved" && isInGatekeeperRole) || (r.EmployeeId == currentUser.EmployeeId) || (r.DepartmentId == departmentId && isInSupervisorRole)) && r.VisitDateTimeEnd >= today);
                int requests = await _context.RequestInformation
                    .CountAsync(r => r.CompanyId == _current.Value.Id &&
                                        r.VisitDateTimeStart < today &&
                                        r.Status == "Pending" &&
                                        !r.Deleted);
                int allRegisteredUser = _userManager.Users.Count();
                int employeesCount = await _context.UserRoles.CountAsync(ur => ur.RoleId == employeeRole.Id);
                int departments = await _context.Department.Where(d => d.CompanyId == _current.Value.Id).CountAsync();
                int allRequests = await query.CountAsync();
                int Pendingrequest = await query.CountAsync(r => r.Status == "Pending");
                int Approvedrequest = await query.CountAsync(r => r.Status == "Approved");
                int Rejectedrequest = await query.CountAsync(r => r.Status == "Rejected");
                int PendingrequestEmployee = await query.CountAsync(r => r.Status == "Pending" && r.EmployeeId == currentUser.EmployeeId);
                int ApprovedrequestEmployee = await query.CountAsync(r => r.Status == "Approved" && r.EmployeeId == currentUser.EmployeeId);
                int RejectedrequestEmployee = await query.CountAsync(r => r.Status == "Rejected" && r.EmployeeId == currentUser.EmployeeId);
                int Reviewedrequest = await query.CountAsync(r => r.Status == "Reviewed");
                ViewBag.PendingrequestEmployee = PendingrequestEmployee;
                ViewBag.ApprovedrequestEmployee = ApprovedrequestEmployee;
                ViewBag.RejectedrequestEmployee = RejectedrequestEmployee;
                ViewBag.outDatedRequests = requests;
                ViewBag.allRequests = allRequests;
                ViewBag.employees = employeesCount;
                ViewBag.departments = departments;
                ViewBag.Pendingrequest = Pendingrequest;
                ViewBag.Approvedrequest = Approvedrequest;
                ViewBag.Rejectedrequest = Rejectedrequest;
                ViewBag.allRegisteredUser = allRegisteredUser;
                ViewBag.Reviewedrequest = Reviewedrequest;
                int companyEmployeesCount = await _context.Employee.Where(e => e.CompanyId == _current.Value.Id).CountAsync();
                ViewBag.CompanyEmployees = companyEmployeesCount;

                // ----------------- START: ADDED CODE FOR CHARTS -----------------

                // 1. Data for User Roles Doughnut Chart
                var adminRole = await _context.Roles.SingleOrDefaultAsync(r => r.Name == "Admin");
                var supervisorRole = await _context.Roles.SingleOrDefaultAsync(r => r.Name == "Superviser");
                var gatekeeperRole = await _context.Roles.SingleOrDefaultAsync(r => r.Name == "Gatekeeper");

                int adminsCount = (adminRole != null) ? await _context.UserRoles.CountAsync(ur => ur.RoleId == adminRole.Id) : 0;
                int supervisorsCount = (supervisorRole != null) ? await _context.UserRoles.CountAsync(ur => ur.RoleId == supervisorRole.Id) : 0;
                int gatekeepersCount = (gatekeeperRole != null) ? await _context.UserRoles.CountAsync(ur => ur.RoleId == gatekeeperRole.Id) : 0;

                ViewBag.admins = adminsCount;
                ViewBag.supervisors = supervisorsCount;
                ViewBag.gatekeepers = gatekeepersCount;

                // 2. Data for Requests Line Chart
                var sevenDaysAgo = DateTime.Today.AddDays(-6);
                var requestsFromDb = await _context.RequestInformation
       .Where(r => r.CompanyId == _current.Value.Id && r.VisitDateTimeStart.Date >= sevenDaysAgo)
       .GroupBy(r => r.VisitDateTimeStart.Date)
       .Select(g => new
       {
           RequestDate = g.Key, // Keep the date as a DateTime object
           Count = g.Count()
       })
       .ToListAsync(); // This brings the data from the DB into memory

                // 2. Now that the data is in memory, format it using standard C#.
                //    This part is no longer translated to SQL.
                var requestsOverTime = requestsFromDb
                    .OrderBy(x => x.RequestDate)
                    .Select(x => new
                    {
                        Date = x.RequestDate.ToString("yyyy-MM-dd"), // Format the date HERE
                        x.Count
                    })
                    .ToList();

                ViewBag.RequestsOverTime = requestsOverTime;

                // ------------------ END: ADDED CODE FOR CHARTS ------------------

                var requestInformation = await _context.RequestInformation
                    .Include(r => r.Employee)
                        .ThenInclude(r => r.Department)
                    .Include(r => r.Guest)
                    .Include(r => r.Attachments)
                    .Where(r => r.CompanyId == _current.Value.Id)
                    .Where(r =>r.Status == "Approved")
                    .ToListAsync();

                return View(requestInformation);
            }
            else
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                ViewBag.TodaysVisitorCount = await _context.RequestInformation
                    .CountAsync(r => r.CompanyId == _current.Value.Id &&
                                        r.VisitDateTimeStart >= today &&
                                        r.VisitDateTimeStart < tomorrow &&
                                        r.Status == "Approved");


                // Get total visitor count for the company
                ViewBag.TotalVisitorCount = await _context.RequestInformation
                    .CountAsync(r => r.CompanyId == _current.Value.Id &&
                                        r.Status == "Approved");
                return View();
            }
        }




        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            return LocalRedirect(returnUrl);
        }



       
        public async Task<IActionResult> CheckIn(int id)
        {
            var request = await _context.RequestInformation.FindAsync(id);
            _logger.LogInformation("request id =================================================", request.Id);
            if (request == null)
            {
                return NotFound();
            }

            if (!request.IsCheckedIn)
            {
                request.IsCheckedIn = true;
                _context.Update(request);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index)); 
        }

     
        public async Task<IActionResult> CheckOut(int id)
        {
            var request = await _context.RequestInformation.FindAsync(id);
            if (request == null)
            {
                return NotFound();
            }

            if (request.IsCheckedIn)
            {
                request.IsCheckedIn = false;
                _context.Update(request);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index)); 
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        [HttpGet]
        public IActionResult GetChartData()
        {
            var data = _context.GuestActivityLogs
                .GroupBy(r => r.Timestamp.Date) // Group by Date without Time
                .Select(group => new
                {
                    Date = group.Key, // Keep DateTime format for now
                    Count = group.Count(e => e.ActivityType == "Check In") // Count occurrences of "Check In"
                })
                .OrderBy(x => x.Date) // Order by Date
                .AsEnumerable() // Switch to LINQ-to-Objects to apply formatting
                .Select(x => new
                {
                    Date = x.Date.ToString("yyyy-MM-dd"), // Format date after fetching data
                    Count = x.Count
                })
                .ToList();

            // Prepare data for the frontend
            var labels = data.Select(x => x.Date).ToArray();
            var values = data.Select(x => x.Count).ToArray();

            return Json(new { labels, data = values });
        }

    }
}
