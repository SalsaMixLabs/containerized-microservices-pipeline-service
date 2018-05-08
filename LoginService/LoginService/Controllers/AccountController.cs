using LoginService.Models;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Net.Mail;
using System.Text.RegularExpressions;
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

            var (isValid, message) = ValidateEmailName(model.Email);
            if (!isValid)
            {
                return BadRequest(message);
            }

            (isValid, message) = ValidatePassword(model.Password);
            if (!isValid)
            {
                return BadRequest(message);
            }

            (isValid, message) = ValidateUserName(model.UserName);
            if (!isValid)
            {
                return BadRequest(message);
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
        ///     Placeholder to put policy on email name validation.  
        ///     
        ///     This uses the built in MailAddress validation from .Net
        /// </summary>
        /// <param name="email"></param>
        /// <returns> true and "" if it is a valid email or false and a user message if it is not</returns>
        private (bool isValid, string message) ValidateEmailName(string email)
        {
            //
            //  the MailAddress object doesn't like "" or null's
            if (email == "" || email == null)
                return (false, "email can't be blank");
            try
            {
                MailAddress m = new MailAddress(email);

                return (true, "");
            }
            catch (FormatException e)
            {
                return (false, e.ToString());
            }
        }
        /// <summary>
        ///     Apply password policy.  In this demo, it calls the PasswordAdvisor class located below and 
        ///     rejects any password that isn't at least "medium" strength.  In a production system, these
        ///     strings would be loaded dynamically and localized.
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>

        private (bool isValid, string message) ValidatePassword(string password)
        {
            var passwordStrength = PasswordAdvisor.CheckStrength(password);
            switch (passwordStrength)
            {
                case PasswordScore.Blank:
                    return (false, "No blank passwords");
                case PasswordScore.VeryWeak:
                    return (false, "This is a very weak password.  Make it longer and use at least one Upper Case chacter and at least one special character");
                case PasswordScore.Weak:
                    return (false, "This is a weak password.  Make it longer and use at least one Upper Case chacter and at least one special character");
                case PasswordScore.Medium:
                    break;
                case PasswordScore.Strong:
                    break;
                case PasswordScore.VeryStrong:
                    break;
                default:
                    break;
            }

            return (true, "");
        }



        /// <summary>
        ///     Apply username policy.  We ensure that it is at least 3 characters long.
        ///     In production we would also check to ensure it doesn't exist.
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>

        private (bool isValid, string message) ValidateUserName(string userName)
        {

            string error = "usernames must be at least 3 characters long";
            if (userName == null || userName == "")
                return (false, error);
            if (userName.Length < 4)
                return (false, error);

            return (true, "");
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

    /*
     *  support for password strength detection
     *  
    */

    public enum PasswordScore
    {
        Blank = 0,
        VeryWeak = 1,
        Weak = 2,
        Medium = 3,
        Strong = 4,
        VeryStrong = 5
    }

    public class PasswordAdvisor
    {
        public static PasswordScore CheckStrength(string password)
        {
            if (password == "" || password == null)
                return PasswordScore.Blank;

            int score = 0;

            if (password.Length < 1)
                return PasswordScore.Blank;
            if (password.Length < 4)
                return PasswordScore.VeryWeak;

            if (password.Length >= 8)
                score++;
            if (password.Length >= 12)
                score++;
            if (Regex.Match(password, @"\d", RegexOptions.None).Success) // has at least one number
                score++;
            if (Regex.Match(password, @"(?=.*[A-Z])(?=.*[a-z])", RegexOptions.None).Success)
                score++;
            if (Regex.Match(password, @"/.[!,@,#,$,%,^,&,*,?,_,~,-,£,(,)]/", RegexOptions.None).Success)
                score++;

            return (PasswordScore)score;
        }


    }
}
