using GatePass.MS.Application;
using GatePass.MS.ClientApp.Controllers;
using GatePass.MS.ClientApp.Data;
using GatePass.MS.Domain;
using GatePass.MS.Domain.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GatePass.MS.ClientApp.Service
{
    public class GuestActivityService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public GuestActivityService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task LogActivityAsync(int guestId, string activityType, string activityDescription)
        {
            var log = new GuestActivityLogs
            {
                GuestId = guestId,
                ActivityType = activityType,
                ActivityDescription = activityDescription,
                Timestamp = DateTime.Now
            };

            _context.GuestActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }


        public async Task<List<GuestActivityReport>> GetGuestActivitiesByIdAsync(int guestId)
        {
            return await _context.GuestActivityLogs
                .Where(log => log.GuestId == guestId)
                .Select(log => new GuestActivityReport
                {
                    GuestId = log.GuestId,
                    Email = _context.Guest.FirstOrDefault(u => u.Id == log.GuestId).Email,
                    ActivityType = log.ActivityType,
                    ActivityDescription = log.ActivityDescription,
                    Timestamp = log.Timestamp
                })
                .ToListAsync();
        }

        public async Task<GuestActivityReportModel> GetGuestActivitiesAsync(
     DateTime? startDate,
     DateTime? endDate,
     int? SelectedGuestId,
     string? activityType,
     string userId)
        {
            // 1. Get the logged-in user's department
            var user = await _userManager.Users
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Id == userId);

            var departmentId = user?.Employee?.DepartmentId;

            // 2. Get all guest IDs who visited this department
            var guestIds = await _context.RequestInformation
                .Where(r => r.DepartmentId == departmentId)
                .Select(r => r.GuestId)
                .Distinct()
                .ToListAsync();

            // 3. Build dropdown list
            var guests = await _context.Guest
                .Where(g => guestIds.Contains(g.Id))
                .Select(g => new SelectListItem
                {
                    Value = g.Id.ToString(),
                    Text = g.FirstName + " " + g.LastName
                })
                .ToListAsync();

            // 4. Build activity list from RequestInformation
            var query = _context.RequestInformation
                .Include(r => r.Guest)
                .Where(r => guestIds.Contains(r.GuestId))
                .AsQueryable();

            // Filter by selected guest
            if (SelectedGuestId.HasValue)
                query = query.Where(r => r.GuestId == SelectedGuestId.Value);

            // Filter by date
            if (startDate.HasValue)
                query = query.Where(r => r.VisitDateTimeStart >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(r => r.VisitDateTimeEnd <= endDate.Value);

            // 5. Convert results to Activity DTO
            var activities = await query
                .Select(r => new GuestActivityDto
                {
                    FirstName = r.Guest.FirstName,
                    LastName = r.Guest.LastName,
                    Email = r.Guest.Email,
                    Timestamp = r.VisitDateTimeStart,
                    ActivityType = r.PurposeOfVisit,
                    ActivityDescription = "Visit Requested"
                })
                .ToListAsync();

            return new GuestActivityReportModel
            {
                StartDate = startDate,
                EndDate = endDate,
                SelectedGuestId = SelectedGuestId,
                Guests = guests,
                Activities = activities
            };
        }




    }

}
