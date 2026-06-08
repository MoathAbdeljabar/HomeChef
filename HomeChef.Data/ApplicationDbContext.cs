
using HomeChef.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace HomeChef.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser> 
{

    public ApplicationDbContext(DbContextOptions option) : base(option)
        {


        }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);


        
        builder.Entity<ApplicationUser>(entity =>
        {
            // Create unique index on PhoneNumber
            entity.HasIndex(u => u.PhoneNumber)
                  .IsUnique()
                  .HasFilter("[PhoneNumber] IS NOT NULL");


            // Make Email optional (nullable)
            entity.Property(e => e.Email)
                .IsRequired(false)  // Makes it optional
                .HasMaxLength(256);
        });
  
    
        builder.Entity<IdentityRole>().HasData(
            new IdentityRole { Name = "Admin", NormalizedName = "ADMIN" },
            new IdentityRole { Name = "Kitchen", NormalizedName = "KITCHEN" },
            new IdentityRole { Name = "User", NormalizedName = "USER" }
        );

        //builder.ApplyConfiguration(new ProductConfiguration());
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

    }

 
}


public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {

        var basePath = Path.GetFullPath(Path.Combine(
              Directory.GetCurrentDirectory(),
              "..", "HomeChef.API"));


        // Build configuration manually
        IConfiguration configuration = new ConfigurationBuilder()
            // Set the path to the ASP.NET Core project (adjust as needed)
            .SetBasePath(basePath).AddJsonFile("appsettings.json").Build();

        // Read the connection string by name
        var connectionString = configuration.GetConnectionString("Default");

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }


}

