using GatePass.MS.Application;
using GatePass.MS.ClientApp.Controllers;
using GatePass.MS.ClientApp.Data;
using GatePass.MS.Domain;
using GatePass.MS.Domain.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GatePass.MS.ClientApp.Service
{
    public class UserActivityService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICurrentCompany _current;
        private readonly IHttpContextAccessor _http;

        public UserActivityService(ApplicationDbContext context, ICurrentCompany current, UserManager<ApplicationUser> userManager, IHttpContextAccessor http)
        {
            _context = context;
            _userManager = userManager;
            _current = current;
            _http = http;

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

        public async Task<UserActivityReportModel> GetUserActivitiesAsync(
                DateTime? startDate,
                DateTime? endDate,
                string? SelectedUserId)
        {
            // SAFE principal access
            var principal = _http.HttpContext?.User;

            if (principal == null)
                throw new Exception("HttpContext.User is null.");

            var currentUser = await _userManager.GetUserAsync(principal);
            bool isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            var query = _context.UserActivityLogs
                .Where(l => l.CompanyId == _current.Value.Id)
                .AsQueryable();

            // Restrict users list to same company
            var users = await _context.Users
                .Where(u => u.CompanyId == _current.Value.Id)
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = u.UserName
                })
                .ToListAsync();

            // Only Admin can filter by SelectedUserId
            if (isAdmin && !string.IsNullOrEmpty(SelectedUserId))
            {
                query = query.Where(a => a.UserId == SelectedUserId);
            }

            // Date Filters
            if (startDate.HasValue)
                query = query.Where(a => a.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.Timestamp <= endDate.Value);

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
