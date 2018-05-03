using LoginService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LoginService.Data
{
    /// <summary>
    /// Roles automatically created by DB seed process.
    /// </summary>
    public enum Role { Administrator, Owner, Contributor, Reader }

    /// <summary>
    /// Applies automatic migrations and seeds the data in the data base.
    /// </summary>
    public class DbInitializer
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        IServiceProvider _serviceProvider;

        /// <summary>
        /// Creates an instance of DbInitializer.
        /// </summary>
        /// <param name="serviceProvider">Dependency injection provider.</param>
        public DbInitializer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _userManager = serviceProvider.GetService<UserManager<ApplicationUser>>();
            _roleManager = serviceProvider.GetService<RoleManager<IdentityRole>>();
        }

        /// <summary>
        /// Applies automatic migrations in the data base. Creates the data base if it doesn't exist.
        /// </summary>
        /// <returns>Tas for await.</returns>
        public async Task ApplyMigrationsAsync()
        {
            await _serviceProvider.GetService<ApplicationDbContext>().Database.MigrateAsync();
        }

        /// <summary>
        /// Ensures that there are some test users in the data base.
        /// </summary>
        /// <returns>Tas for await.</returns>
        public async Task SeedDataAsync()
        {
            await EnsureRoles();

            if (!_userManager.Users.Any())
            {
                var roles = new[]
                {
                    new [] { Role.Administrator },
                    new [] { Role.Reader },
                    new [] { Role.Reader, Role.Contributor },
                    new [] { Role.Reader, Role.Contributor, Role.Owner },
                };

                for (int i = 0; i < roles.Length; i++)
                {
                    var user = await CreateUser($"user{i}", "contoso.com", $"Password{i}", roles[i]);
                }
            }
        }

        private async Task<ApplicationUser> CreateUser(string userName, string domain, string password, params Role[] roles)
        {
            var user = new ApplicationUser { UserName = userName, Email = $"{userName}@{domain}" };
            await _userManager.CreateAsync(user, password);

            foreach (var role in roles)
            {
                await _userManager.AddToRoleAsync(user, role.ToString());
            }

            return user;
        }

        private async Task EnsureRoles()
        {
            if (!_roleManager.Roles.Any())
            {
                foreach (var role in Enum.GetValues(typeof(Role)).Cast<Role>())
                {
                    await _roleManager.CreateAsync(new IdentityRole { Name = role.ToString() });
                }
            }
        }
    }
}
