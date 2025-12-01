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
            // 1. Get logged-in user's department
            var user = await _userManager.Users
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Id == userId);

            var departmentId = user?.Employee?.DepartmentId;

            // 2. Get all guests who submitted requests to this department
            var guestIds = await _context.RequestInformation
                .Include(r => r.Employee)
                .Where(r => r.Employee.DepartmentId == departmentId)
                .Select(r => r.GuestId)
                .Distinct()
                .ToListAsync();

            // 3. Build dropdown list WITHOUT DUPLICATES
            var guests = await _context.Guest
                .Where(g => guestIds.Contains(g.Id))
                .Select(g => new SelectListItem
                {
                    Value = g.Id.ToString(),
                    Text = g.FirstName + " " + g.LastName
                })
                .Distinct()
                .ToListAsync();

            // 4. Activities query (only for this department)
            var query = _context.GuestActivityLogs
                .Where(a => guestIds.Contains(a.GuestId))
                .AsQueryable();

            if (SelectedGuestId.HasValue)
                query = query.Where(a => a.GuestId == SelectedGuestId);

            if (!string.IsNullOrEmpty(activityType))
                query = query.Where(a => a.ActivityType == activityType);

            if (startDate.HasValue)
                query = query.Where(a => a.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.Timestamp <= endDate.Value);

            // 5. Get final activity list
            var activities = await query
                .Include(a => a.Guest)
                .Select(a => new GuestActivityDto
                {
                    Timestamp = a.Timestamp,
                    Email = a.Guest.Email,
                    FirstName = a.Guest.FirstName,
                    LastName = a.Guest.LastName,
                    ActivityType = a.ActivityType,
                    ActivityDescription = a.ActivityDescription
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
