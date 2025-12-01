using System;
using GatePass.MS.ClientApp.Data;
using GatePass.MS.Domain;
using GatePass.MS.Domain.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using GatePass.MS.ClientApp.Service;


namespace GatePass.MS.Application
{
    public class ReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICurrentCompany _current;



        public ReportService(ApplicationDbContext context, ICurrentCompany current, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _current = current;

            _userManager = userManager;
        }

        public List<VisitReportDto> GetVisitReports(ClaimsPrincipal User, DateTime? startDate, DateTime? endDate, string status, int? employeeId)
        {

            // Load the current user
            var currentUser = _userManager.GetUserAsync(User).Result;

            if (currentUser == null)
            {
                return new List<VisitReportDto>(); // Handle null user scenario
            }

            _context.Entry(currentUser).Reference(x => x.Employee).Load();
            int? currentUserDepartmentId = currentUser?.Employee?.DepartmentId;

            // Check if the current user is in the "Supervisor" role
            var isInSupervisorRole = _userManager.IsInRoleAsync(currentUser, "Superviser").Result;
            var isInAdminRole = _userManager.IsInRoleAsync(currentUser, "Admin").Result;

            // Start building the query
            var query = _context.RequestInformation
                .Where(r => r.CompanyId == _current.Value.Id)
                .Include(r => r.Employee)
                .Include(r => r.Guest)
                .Include(r => r.Devices)            // <-- added
                .Include(r => r.AdditionalGuests)   // <-- added
                .Where(r => (r.EmployeeId == currentUser.EmployeeId) ||
                            (isInSupervisorRole && r.DepartmentId == currentUserDepartmentId) ||
                             isInAdminRole)
                .AsQueryable();




            // Apply filters if provided
            if (startDate.HasValue)
            {
                query = query.Where(r => r.VisitDateTimeStart >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(r => r.VisitDateTimeEnd <= endDate.Value);
            }
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(r => r.Status == status);
            }
            if (employeeId.HasValue)
            {
                query = query.Where(r => r.EmployeeId == employeeId.Value);
            }
            if (employeeId.HasValue)
            {
                query = query.Where(r => r.EmployeeId == employeeId.Value);
            }

            var reports = query
                .Select(r => new VisitReportDto
                {
                    RequestId = r.Id,
                    VisitDateTimeStart = r.VisitDateTimeStart,
                    VisitDateTimeEnd = r.VisitDateTimeEnd,
                    PurposeOfVisit = r.PurposeOfVisit,
                    GuestName = r.IsIndividual
                        ? r.Guest.FirstName + " " + r.Guest.LastName
                        : r.Guest.CompanyName,
                    Phone = r.Guest.Phone,
                    Email = r.Guest.Email,
                    EmployeeName = r.Employee != null ? r.Employee.FirstName : "No Employee",
                    Status = r.Status,
                    Approver = r.Approver != null
                        ? r.Approver.Employee.FirstName + " " + r.Approver.Employee.LastName
                        : "No approver",
                    Timestamp = r.DateCreated,
                    ApprovedDateTimeStart = r.ApprovedDateTimeStart,
                    ApprovedDateTimeEnd = r.ApprovedDateTimeEnd,

                    // NEW FIELDS:
                    AdditionalGuests = string.Join(", ",
                        r.AdditionalGuests.Select(g => g.FirstName + " " + g.LastName)),

                    Devices = string.Join(", ",
                        r.Devices.Select(d => d.DeviceName))
                })
                .ToList();


            return reports;
        }
       
        public IEnumerable<Employee> GetAllEmployees()
        {
            return _context.Employee.ToList(); // Fetch all employees from the database
        }
    }

}
