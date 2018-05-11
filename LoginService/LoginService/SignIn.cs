
using LoginService.Models;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace LoginService {

    public interface ISignIn {
        Task<string> GenerateJwtTokenAsync(ApplicationUser user);
    }

    public class SignIn : ISignIn
    {

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public SignIn(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration
        ){
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Creates a JWT token for a user.
        /// </summary>
        /// <param name="user">ApplicationUser object.</param>
        /// <returns>Authentication token.</returns>
        public async Task<string> GenerateJwtTokenAsync(ApplicationUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };

            var roles = await _userManager.GetRolesAsync(user);

            var roleClaims = roles.Select(r => new Claim(ClaimTypes.Role, r));

            claims.AddRange(roleClaims);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["JwtExpireMinutes"]));

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtIssuer"],
                audience: _configuration["JwtAudience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}