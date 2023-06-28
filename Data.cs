using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.ComponentModel.DataAnnotations;

namespace PintAPI.Models
{
    public class CareGroup
    {
        [Required]
        public int CareGroupId { get; set; }
        
        [Required]
        public string Email { get; set; }
        
        [Required]
        public string PasswordHash { get; set; }
        
        [Required]
        public string ApiKey { get; set; }
        
        public string Address { get; set; }
    }

    public class UniqueDevice
    {
        [Required]
        public int UniqueDeviceId { get; set; }
        
        [Required]
        public Guid DeviceGuid { get; set; }
        
        [Required]
        public string FriendlyName { get; set; }
    }

    public class Patient
    {
        [Required]
        public int PatientId { get; set; }
        
        [Required]
        public CareGroup CareGroup { get; set; }

        [Required]
        public string FirstName { get; set; }
        
        [Required]
        public string LastName { get; set; }
        
        [Required]
        public DateTime DateOfBirth { get; set; }

        public string? Notes { get; set; }
    }
    
    public class Device
    {
        [Required]
        public int DeviceId { get; set; }
        
        [Required]
        public string Identifier { get; set; }
        
        [Required]
        public string FriendlyName { get; set; }
        
        [Required]
        public Admin CreatedBy { get; set; }
    }

    public class PatientDevice
    {
        [Required]
        public int PatientDeviceId { get; set; }
        
        [Required]
        public Patient Patient { get; set; }
        
        [Required]
        public Device Device { get; set; }
    }
    
    public class Event
    {
        [Required]
        public int EventId { get; set; }
        
        [Required]
        public PatientDevice PatientDevice { get; set; }
        
        [Required]
        public DateTime Timestamp { get; set; }
        
        [Required]
        public string Type { get; set; }
    }

    public class Log
    {
        [Required]
        public int LogId { get; set; }
        
        [Required]
        public PatientDevice PatientDevice { get; set; }
        
        [Required]
        public DateTime Timestamp { get; set; }
        
        [Required]
        public uint Heartbeat { get; set; }
        
        [Required]
        public uint Battery { get; set; }
    }

    public class Admin
    {
        [Required]
        public int AdminId { get; set; }
        
        [Required]
        public string FirstName { get; set; }
        
        [Required]
        public string LastName { get; set; }
        
        [Required]
        public string Key { get; set; }
    }
}

namespace PintAPI
{
    public class PintApiDb : DbContext
    {
        public PintApiDb(DbContextOptions options) : base(options) { }
        
        public DbSet<Models.CareGroup> CareGroups { get; set; }
        public DbSet<Models.Patient> Patients { get; set; }
        public DbSet<Models.PatientDevice> PatientDevices { get; set; }
        public DbSet<Models.Device> Devices { get; set; }
        public DbSet<Models.Event> Events { get; set; }
        public DbSet<Models.Log> Logs { get; set; }
        
        public DbSet<Models.Admin> Admins { get; set; }
    }

    public class PintAppleDbContextFactory : IDesignTimeDbContextFactory<PintApiDb>
    {
        public PintApiDb CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PintApiDb>();

            string connectionString =
                "Server=pintappledb.com;Database=pintapi;User=pintapple;Password=Xo81K&ajrK7O8aPpgkpjCfu17#D*j1fld4oCnFYeiGbev9shz^9v9a0R^%d3Cg3rWOn6N&VKhZV68rrCXhsXxi5%ZTW@s^&OGb&s;ThisIsNotARealPassword:)";
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            
            return new PintApiDb(optionsBuilder.Options);
        }
    }
}