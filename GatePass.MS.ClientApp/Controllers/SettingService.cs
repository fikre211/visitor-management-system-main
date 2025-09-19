using Microsoft.EntityFrameworkCore;
using GatePass.MS.Domain;
using System.Threading.Tasks;
using System.Configuration;
using GatePass.MS.ClientApp.Data;

namespace GatePass.MS.ClientApp.Controllers
{
    public interface ISettingService
    {
        Task<string> GetSettingValueAsync(string key);
        Task UpdateSettingAsync(string key, string value);
        // Additional methods for CRUD operations can be added here
    }

    public class SettingService : ISettingService
    {
        private readonly ApplicationDbContext _context;

        public SettingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GetSettingValueAsync(string key)
        {
            var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Key == key);
            return setting?.Value;
        }

        public async Task UpdateSettingAsync(string key, string value)
        {
            var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Key == key);
            if (setting != null)
            {
                setting.Value = value;
                await _context.SaveChangesAsync();
            }
            else
            {
                setting = new Setting
                {
                    Key = key,
                    Value = value
                };
                await _context.Settings.AddAsync(setting);
            }
        }

        // Implement other CRUD operations as needed
    }
}
