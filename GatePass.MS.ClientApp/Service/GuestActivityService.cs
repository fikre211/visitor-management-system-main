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
            int? selectedGuestId,
            string? activityType,
            string userId)
        {
            // 1️⃣ Logged-in user
            var user = await _userManager.Users
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                throw new Exception("User not found");

            var companyId = user.Employee?.CompanyId;

            if (companyId == null)
                throw new Exception("User is not assigned to a company");

            // 2️⃣ GuestIds that belong to this company (via RequestInformation)
            var guestIdsForCompany = await _context.RequestInformation
                .Where(r => r.CompanyId == companyId)
                .Select(r => r.GuestId)
                .Distinct()
                .ToListAsync();

            // 3️⃣ Guest dropdown (company-filtered)
            var guests = await _context.Guest
                .Where(g => guestIdsForCompany.Contains(g.Id))
                .Select(g => new SelectListItem
                {
                    Value = g.Id.ToString(),
                    Text = g.FirstName + " " + g.LastName
                })
                .ToListAsync();

            // 4️⃣ Activity query (from GuestActivityLogs)
            var query = _context.GuestActivityLogs
                .Include(a => a.Guest)
                .Where(a =>
                    a.GuestId != null &&
                    guestIdsForCompany.Contains(a.GuestId.Value))
                .AsQueryable();

            // 5️⃣ Filters
            if (selectedGuestId.HasValue)
                query = query.Where(a => a.GuestId == selectedGuestId.Value);

            if (startDate.HasValue)
                query = query.Where(a => a.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.Timestamp <= endDate.Value);

            if (!string.IsNullOrWhiteSpace(activityType))
                query = query.Where(a => a.ActivityType == activityType);

            // 6️⃣ Projection
            var activities = await query
                .OrderByDescending(a => a.Timestamp)
                .Select(a => new GuestActivityDto
                {
                    FirstName = a.Guest.FirstName,
                    LastName = a.Guest.LastName,
                    Email = a.Guest.Email,
                    ActivityType = a.ActivityType,
                    ActivityDescription = a.ActivityDescription,
                    Timestamp = a.Timestamp
                })
                .ToListAsync();

            return new GuestActivityReportModel
            {
                StartDate = startDate,
                EndDate = endDate,
                SelectedGuestId = selectedGuestId,
                Guests = guests,
                Activities = activities
            };
        }


    }

}
