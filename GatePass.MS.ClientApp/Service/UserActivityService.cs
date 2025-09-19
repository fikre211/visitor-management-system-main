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
    public class UserActivityService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICurrentCompany _current;

        public UserActivityService(ApplicationDbContext context, ICurrentCompany current, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
            _current = current;

        }

        public async Task LogActivityAsync(string userId, string activityType, string activityDescription)
        {
            var log = new UserActivityLog
            {
                UserId = userId,
                ActivityType = activityType,
                ActivityDescription = activityDescription,
                Timestamp = DateTime.Now,
                CompanyId=_current.Value.Id
            };

            _context.UserActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }


        public async Task<List<UserActivityReport>> GetUserActivitiesByIdAsync(string userId)
        {
            return await _context.UserActivityLogs
                .Where(log => log.UserId == userId)
                .Select(log => new UserActivityReport
                {
                    UserId = log.UserId,
                    UserName = _context.Users.FirstOrDefault(u => u.Id == log.UserId).UserName,
                    ActivityType = log.ActivityType,
                    ActivityDescription = log.ActivityDescription,
                    Timestamp = log.Timestamp
                })
                .ToListAsync();
        }

         public async Task<UserActivityReportModel> GetUserActivitiesAsync( DateTime? startDate, DateTime? endDate, string? SelectedUserId)
        {
            var query = _context.UserActivityLogs
                                 .Where(l => l.CompanyId == _current.Value.Id)

                .AsQueryable() 
                ;

            var users = await _context.Users
                .Select(u => new SelectListItem
            {
                Value = u.Id.ToString(),
                Text = u.UserName
            }).ToListAsync();
           // SelectedUserId = "73e4f008-45dc-4ebb-ae88-f11e5cb76290";
            // Filter by employeeId (UserId) if provided
            if (!string.IsNullOrEmpty(SelectedUserId))
            {
                query = query.Where(a => a.UserId == SelectedUserId);
            }

            // Filter by start date if provided
            if (startDate.HasValue)
            {
                query = query.Where(a => a.Timestamp >= startDate.Value);
            }

            // Filter by end date if provided
            if (endDate.HasValue)
            {
                query = query.Where(a => a.Timestamp <= endDate.Value);
            }
           
            var activities = await query
                .Select(a => new UserActivityDto
                {
                    Timestamp = a.Timestamp,
                    UserName = _context.Users.FirstOrDefault(u => u.Id == a.UserId).UserName,
                    ActivityType = a.ActivityType,
                    ActivityDescription = a.ActivityDescription
                })
                .ToListAsync();

            return new UserActivityReportModel
            {
                StartDate = startDate,
                EndDate = endDate,
                SelectedUserId = SelectedUserId,
                Users = users,
                Activities = activities
            };
        }

    }

}
