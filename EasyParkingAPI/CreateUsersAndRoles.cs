using EasyParkingAPI.Model;
using EasyParkingAPI.Model;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace EasyParking.API
{
    public class CreateUsersAndRoles
    {
        private UserManager<ApplicationUser> _userManager;
        private RoleManager<IdentityRole> _roleManager;
        public CreateUsersAndRoles(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task CreateRolesAsync()
        {

            bool existeRoleAdmin = await _roleManager.RoleExistsAsync("Admin");
            if (!existeRoleAdmin)
            {
                var role1 = new IdentityRole() { Name = "Admin" };
                await _roleManager.CreateAsync(role1);
            }

            bool existeRoleAppUser = await _roleManager.RoleExistsAsync("AppUser");
            if (!existeRoleAppUser)
            {
                var role3 = new IdentityRole() { Name = "AppUser" };
                await _roleManager.CreateAsync(role3);
            }
        }

        public async Task CreateUsersAsync()
        {
            //*******************************************************************************************************
            //* CARGA DE Usuario: EasyParkingAdmin  Role: Admin
            //*******************************************************************************************************
            if (_userManager.FindByNameAsync("EasyParkingAdmin").Result == null)
            {
                ApplicationUser user01 = new ApplicationUser { UserName = "EasyParkingAdmin", Email = "lucianosponton14@hotmail.com" };
                user01.Email = "lucianosponton14@hotmail.com";
                user01.EmailConfirmed = true;

                _userManager.CreateAsync(user01, "easyparking123").Wait();
                _userManager.AddToRoleAsync(user01, "Admin").Wait();
            }
        }

    }
}
