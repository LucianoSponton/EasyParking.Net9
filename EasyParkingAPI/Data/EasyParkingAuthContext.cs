using EasyParkingAPI.Model;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EasyParkingAPI.Data
{
    public class EasyParkingAuthContext : IdentityDbContext<ApplicationUser>
    {

        public EasyParkingAuthContext(DbContextOptions<EasyParkingAuthContext> options)
           : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //***************************************************************************************************
            // ApplicationUser
            //***************************************************************************************************
            modelBuilder.Entity<ApplicationUser>()
                .HasIndex(c => c.NumeroDeDocumento).IsUnique();

            modelBuilder.Entity<ApplicationUser>()
               .HasIndex(c => c.Email).IsUnique();


        }
    }
}

