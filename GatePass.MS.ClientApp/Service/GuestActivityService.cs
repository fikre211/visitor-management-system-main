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

        public async Task<GuestActivityReportModel> GetGuestActivitiesAsync(DateTime? startDate, DateTime? endDate, int? SelectedGuestId, string? activityType)
        {
            var query = _context.GuestActivityLogs.AsQueryable();

            var guests = await _context.Guest.Select(u => new SelectListItem
            {
                Value = u.Id.ToString(),
                Text = $"{u.FirstName} {u.LastName}" // Show full name in dropdown
            }).ToListAsync();

            // Filter by activity type
            if (activityType != null)
            {
                query = query.Where(a => a.ActivityType == activityType);
            }
            if (startDate.HasValue)
            {
                query = query.Where(a => a.Timestamp >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(a => a.Timestamp <= endDate.Value);
            }

            var activities = await query
                .Select(a => new GuestActivityDto
                {
                    Timestamp = a.Timestamp,
                    Email = _context.Guest.Where(u => u.Id == a.GuestId).Select(u => u.Email).FirstOrDefault(),
                    FirstName = _context.Guest.Where(u => u.Id == a.GuestId).Select(u => u.FirstName).FirstOrDefault(),
                    LastName = _context.Guest.Where(u => u.Id == a.GuestId).Select(u => u.LastName).FirstOrDefault(),
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
