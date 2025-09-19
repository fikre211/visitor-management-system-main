using GatePass.MS.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;


namespace GatePass.MS.Application
{
    public interface IUserService
    {
        int GetCurrentUserId();
        Task<string> GetUsernameById(int userId);
    }

    public class UserService : IUserService
    {
        // Assume we have a user manager or similar service
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;


        public UserService(UserManager<ApplicationUser> userManager, IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public int GetCurrentUserId()
        {
            var userId = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userId);
        }

        public async Task<string> GetUsernameById(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            return user?.UserName;
        }
    }

}
