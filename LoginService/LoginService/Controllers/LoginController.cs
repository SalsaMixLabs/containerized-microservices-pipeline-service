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
        private readonly ISignIn _signIn;
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
            _signIn = new SignIn(userManager, configuration);
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

                string token = await _signIn.GenerateJwtTokenAsync(appUser);

                var result = new ApiUserModel { Token = token, Id = appUser.Id, UserName = appUser.UserName, Email = appUser.Email };

                _telemetryClient.TrackEvent("Successful login.");

                return Ok(result);
            }

            _telemetryClient.TrackEvent("Failed login.");

            return Unauthorized();
        }

        [HttpGet("{value}")]
        public string Get(string value)
        {
            return "Echo > " + value;
        }
    }
}
