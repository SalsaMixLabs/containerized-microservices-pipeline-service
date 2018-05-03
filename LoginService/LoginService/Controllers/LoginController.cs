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

namespace LoginService.Controllers
{
    /// <summary>
    /// Controller producing authorization tokens.
    /// </summary>
    [Produces("application/json")]
    [Route("api/login")]
    [EnableCors("AllowSpecificOrigin")]
    public class LoginController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly TelemetryClient _telemetryClient = new TelemetryClient();

        public LoginController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// POST: api/login
        /// Creates a JWT token and returns user info with the token.
        /// </summary>
        /// <param name="model">Account details. Must include UserName and Password.</param>
        /// <returns>Account details with the authentication token.</returns>
        [HttpPost]        
        public async Task<IActionResult> Post([FromBody]ApiUserModel model)
        {
            if (model == null)
            {
                return BadRequest("Failed: HTTP request body is required.");
            }

            var signIn = await _signInManager.PasswordSignInAsync(model.UserName, model.Password, false, false);

            if (signIn.Succeeded)
            {
                var appUser = _userManager.Users.SingleOrDefault(r => r.UserName == model.UserName);

                string token = await GenerateJwtTokenAsync(appUser);

                var result = new ApiUserModel { Token = token, Id = appUser.Id, UserName = appUser.UserName, Email = appUser.Email };

                _telemetryClient.TrackEvent("Successfull login.");

                return Ok(result);
            }

            _telemetryClient.TrackEvent("Failed login.");

            return Unauthorized();
        }

        [HttpGet("{value}")]
        public string Get(string value)
        {
            //Test method to troubleshoot connectivity. Will be removed once CI/CD with ACR works.
            return "Echo > " + value;
        }

        private async Task<string> GenerateJwtTokenAsync(ApplicationUser user)
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
