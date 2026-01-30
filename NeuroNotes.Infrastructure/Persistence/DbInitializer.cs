using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace NeuroNotes.Infrastructure.Persistence
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(
            NeuroNotesDbContext context,
            RoleManager<IdentityRole> roleManager)
        {
            if (context.Database.IsRelational()) 
                await context.Database.MigrateAsync();

            await EnsureRoleAsync(roleManager, "Admin", new[] { "Permissions.Users.Manage", "Permissions.Roles.Manage" });
            await EnsureRoleAsync(roleManager, "User", new[] { "Permissions.Notes.Create" });
        }

        private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string roleName, string[] permissions)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var role = new IdentityRole(roleName);
                await roleManager.CreateAsync(role);

                foreach (var perm in permissions) 
                    await roleManager.AddClaimAsync(role, new Claim("Permission", perm));
            }
        }
    }
}
