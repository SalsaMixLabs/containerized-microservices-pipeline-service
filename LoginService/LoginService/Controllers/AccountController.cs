using LoginService.Models;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LoginService.Controllers
{
    [Produces("application/json")]
    [Route("api/account")]
    [EnableCors("AllowSpecificOrigin")]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TelemetryClient _telemetryClient = new TelemetryClient();

        public AccountController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        /// <summary>
        /// GET: api/account 
        /// Returns information about current account.
        /// </summary>
        /// <returns>Account details.</returns>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Get()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user.");
            }

            return Ok(new ApiUserModel { Id = user.Id, UserName = user.UserName, Email = user.Email });
        }

        /// <summary>
        /// POST: api/account
        /// Creates new account.
        /// </summary>
        /// <param name="model">Account details. Must include UserName, Password and Email.</param>
        /// <returns>Created account.</returns>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]ApiUserModel model)
        {
            if (model == null)
            {
                return BadRequest("Failed: HTTP request body is required.");
            }

            if (model.Password == null)
            {
                return BadRequest("Failed: Password is required.");
            }

            var user = new ApplicationUser { UserName = model.UserName, Email = model.Email };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                return BadRequest(result.ToString());
            }

            ApiUserModel response = new ApiUserModel { Email = user.Email, Id = user.Id, UserName = user.UserName };

            _telemetryClient.TrackEvent("User created.");

            return Ok(response);
        }

        /// <summary>
        /// PUT: api/account
        /// </summary>
        /// <param name="model">Parts of the account to be modified.</param>
        /// <returns>Operation status.</returns>
        [HttpPut]
        [Authorize]
        public async Task<IActionResult> Put([FromBody]ApiUserModel model)
        {
            if (model == null)
            {
                return BadRequest("Failed: HTTP request body is required.");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!string.IsNullOrEmpty(model.NewPassword))
            {                
                var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.Password, model.NewPassword);
                if (!changePasswordResult.Succeeded)
                {
                    return BadRequest(changePasswordResult.ToString());
                }

                return Ok();
            }
            else if (!string.IsNullOrEmpty(model.Email))
            {
                //await _acountManager.ChangeEmail(user, model.Email);
                if (user.Email != model.Email)
                {
                    var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
                    if (!setEmailResult.Succeeded)
                    {
                        return BadRequest(setEmailResult.ToString());
                    }
                }
                return Ok();
            }

            return BadRequest();
        }

        /// <summary>
        /// DELETE: api/account
        /// Deletes given account. Available only to Admin users.
        /// </summary>
        /// <param name="id">Id of the account to be deleted.</param>
        /// <returns>Operation status.</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Delete(string id)
        {
            var user = _userManager.Users.SingleOrDefault(r => r.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                return BadRequest(result.ToString());
            }

            return Ok();
        }
    }
}
