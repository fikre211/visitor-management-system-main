using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using GatePass.MS.Domain;

namespace GatePass.MS.ClientApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }


        public DbSet<Designation> Designation { get; set; }
        public DbSet<Company> Company { get; set; }

        public DbSet<Department> Department { get; set; }
        public DbSet<Employee> Employee { get; set; }
        public DbSet<Guest> Guest { get; set; }
        public DbSet<Device> Device { get; set; }

        public DbSet<RequestInformation> RequestInformation { get; set; }
        public DbSet<Feedback> Feedback { get; set; }

        public DbSet<RequestStatus> RequestStatus { get; set; }
        public DbSet<RequestType> RequestType { get; set; }
        public DbSet<Attachment> Attachment { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<UserActivityLog> UserActivityLogs { get; set; }
        public DbSet<GuestActivityLogs> GuestActivityLogs { get; set; }



    }
}
