using GatePass.MS.ClientApp.Data;
using GatePass.MS.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GatePass.MS.ClientApp.Controllers
{
    public class DbInitializer
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DbInitializer> _logger;
        private readonly IConfiguration _configuration;
        private readonly ISettingService _settingService;

        public DbInitializer(UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager, 
            ApplicationDbContext context, 
            ILogger<DbInitializer> logger,
            IConfiguration configuration,
            ISettingService settingService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _logger = logger;

            _configuration= configuration;
            _settingService = settingService;
        }

        public async Task SeedAsync()
        {
            var adminUsername = _configuration["AdminUser:Username"];
            var adminPassword = _configuration["AdminUser:Password"];
            var adminEmail = _configuration["AdminUser:Email"];
            try
            {
                // Ensure database is created and up-to-date
                _context.Database.Migrate();
                // Seed all roles in our system
                // Check if the admin role exists, and create it if not
                if (!await _roleManager.RoleExistsAsync("Admin"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Admin"));
                    _logger.LogInformation("Admin role created");
                }
                 // Check if the Superviser role exists, and create it if not
                if (!await _roleManager.RoleExistsAsync("Superviser"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Superviser"));
                    _logger.LogInformation("Superviser role created");
                }
                 // Check if the Employee role exists, and create it if not
                if (!await _roleManager.RoleExistsAsync("Employee"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Employee"));
                    _logger.LogInformation("Employee role created");
                }
                 // Check if the Gatekeeper role exists, and create it if not
                if (!await _roleManager.RoleExistsAsync("Gatekeeper"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Gatekeeper"));
                    _logger.LogInformation("Gatekeeper role created");
                }
                 // Check if the Guest role exists, and create it if not
               
                //End seeding Roles

                // Seed some Setting Setting
                await _settingService.UpdateSettingAsync("AppName", "VMS");
                await _settingService.UpdateSettingAsync("MaxFailedAccessAttempts", "5");
                // Seed Admin User 
                // Check if the super user exists
                var adminUser = await _userManager.FindByNameAsync(adminUsername);
                if (adminUser == null)
                {
                   
                    var user = new ApplicationUser
                    {
                        UserName = adminUsername,
                        Email = adminEmail,
                        EmailConfirmed = true // Optional: Automatically confirm email
                    };

                    var result = await _userManager.CreateAsync(user, adminPassword);

                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, "Admin");
                        _logger.LogInformation("Admin user created and added to Admin role");
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            _logger.LogError($"Error creating admin user: {error.Description}");
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("Admin user already exists");
                }

            
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while seeding the database: {ex.Message}");
                throw;
            }
        }
    }
}
