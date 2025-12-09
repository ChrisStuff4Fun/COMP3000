using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options):base(options){} // Default constructor


    // Map CS entities to tables in DB
    public DbSet<DeviceDeviceGroupLink> Device_DeviceGroup_Link {get; set;}
    public DbSet<Device>                Devices {get; set;}
    public DbSet<DeviceGroup>           DeviceGroups {get; set;}
    public DbSet<DeviceJoinCode>        DeviceJoinCodes {get; set;}
    public DbSet<DevicePolicyStatus>    DevicePolicyStatus {get; set;}
    public DbSet<Geofence>              Geofences {get; set;}
    public DbSet<Organisation>          Organisations {get; set;}
    public DbSet<OrgJoinCode>           OrgJoinCodes {get; set;}
    public DbSet<Policy>                Policies {get; set;}
    public DbSet<User>                  UserAccessLevels {get; set;}




}