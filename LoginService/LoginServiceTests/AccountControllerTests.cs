using LoginService.Controllers;
using LoginService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LoginServiceTests
{
    [TestClass]
    public class AccountControllerTests : ControllerTestsBase
    {
        [TestInitialize]
        public override void Init()
        {
            base.Init();
        }

        [TestMethod]
        public async Task AccountControllerTestGet()
        {
            string password = "ABCdef123!@#";

            var user = new ApplicationUser
            {
                UserName = Guid.NewGuid().ToString(),
                Email = $"{Guid.NewGuid()}@host.com",
            };

            var created = await UserManager.CreateAsync(user, password);

            Assert.IsTrue(created.Succeeded);

            user = await Context.Users.SingleAsync();

            var target = new AccountController(UserManager, ConfigMock.Object);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

            target.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = principal }
            };

            var response = (OkObjectResult)await target.Get();

            Assert.AreEqual(200, response.StatusCode);

            var actual = (ApiUserModel)response.Value;

            Assert.AreEqual(user.UserName, actual.UserName);
            Assert.AreEqual(user.Email, actual.Email);
            Assert.AreEqual(user.Id, actual.Id);
            Assert.IsFalse(string.IsNullOrEmpty(actual.Id));

            Assert.IsTrue(string.IsNullOrEmpty(actual.Password));
            Assert.IsTrue(string.IsNullOrEmpty(actual.NewPassword));
            Assert.IsTrue(string.IsNullOrEmpty(actual.Token));
        }

        [TestMethod]
        public async Task AccountControllerTestCreate()
        {
            var target = new AccountController(UserManager, ConfigMock.Object);

            var request = new ApiUserModel
            {
                UserName = Guid.NewGuid().ToString(),
                Email = $"{Guid.NewGuid()}@host.com",
                Password = "ABCdef123!@#"
            };

            var response = (OkObjectResult)await target.Post(request);

            Assert.AreEqual(200, response.StatusCode);

            var actual = (ApiUserModel)response.Value;

            Assert.AreEqual(request.UserName, actual.UserName);
            Assert.AreEqual(request.Email, actual.Email);
            Assert.IsFalse(string.IsNullOrEmpty(actual.Id));

            var created = await Context.Users.SingleAsync();
            Assert.AreEqual(request.UserName, created.UserName);
            Assert.AreEqual(request.Email, created.Email);
            Assert.AreEqual(created.Id, actual.Id);

            bool passwordOk = await UserManager.CheckPasswordAsync(created, request.Password);
            Assert.IsTrue(passwordOk);
        }

        [TestMethod]
        public async Task AccountControllerTestCreateNoPassword()
        {
            var target = new AccountController(UserManager, ConfigMock.Object);

            var request = new ApiUserModel
            {
                UserName = Guid.NewGuid().ToString(),
                Email = $"{Guid.NewGuid()}@host.com",
           };

            var response = (BadRequestObjectResult)await target.Post(request);

            Assert.AreEqual(400, response.StatusCode);
        }

        [TestMethod]
        public async Task AccountControllerTestCreateNoUsername()
        {
            var target = new AccountController(UserManager, ConfigMock.Object);

            var request = new ApiUserModel
            {
                Email = $"{Guid.NewGuid()}@host.com",
                Password = "ABCdef123!@#"
            };

            var response = (BadRequestObjectResult)await target.Post(request);

            Assert.AreEqual(400, response.StatusCode);
        }

        [TestMethod]
        public async Task AccountControllerTestCreateEmptyUsername()
        {
            var target = new AccountController(UserManager, ConfigMock.Object);

            var request = new ApiUserModel
            {
                UserName = "",
                Email = $"{Guid.NewGuid()}@host.com",
                Password = "ABCdef123!@#"
            };

            var response = (BadRequestObjectResult)await target.Post(request);

            Assert.AreEqual(400, response.StatusCode);
        }

        [TestMethod]
        public async Task AccountControllerTestCreateNoEmail()
        {
            var target = new AccountController(UserManager, ConfigMock.Object);

            var request = new ApiUserModel
            {
                UserName = Guid.NewGuid().ToString(),
                Password = "ABCdef123!@#"
            };

            var response = (BadRequestObjectResult)await target.Post(request);

            Assert.AreEqual(400, response.StatusCode);
        }

        [TestMethod]
        public async Task AccountControllerTestCreateInvalidEmail()
        {
            var target = new AccountController(UserManager, ConfigMock.Object);

            var request = new ApiUserModel
            {
                UserName = Guid.NewGuid().ToString(),
                Email = "invalidEmail",
                Password = "ABCdef123!@#"
            };

            var response = (BadRequestObjectResult)await target.Post(request);

            Assert.AreEqual(400, response.StatusCode);
        }

        [TestMethod]
        public async Task AccountControllerTestCreateEmptyBody()
        {
            var target = new AccountController(UserManager, ConfigMock.Object);

            var response = (BadRequestObjectResult)await target.Post(null);

            Assert.AreEqual(400, response.StatusCode);
        }

        [TestMethod]
        public async Task AccountControllerTestCreateDuplicate()
        {
            var target = new AccountController(UserManager, ConfigMock.Object);

            var request = new ApiUserModel
            {
                UserName = Guid.NewGuid().ToString(),
                Email = $"{Guid.NewGuid()}@host.com",
                Password = "ABCdef123!@#"
            };

            var response = (OkObjectResult)await target.Post(request);

            Assert.AreEqual(200, response.StatusCode);

            var error = (BadRequestObjectResult)await target.Post(request);

            Assert.AreEqual(400, error.StatusCode);
        }

        [TestMethod]
        public async Task AccountControllerTestDelete()
        {
            string password = "ABCdef123!@#";

            var user1 = new ApplicationUser { UserName = Guid.NewGuid().ToString(), Email = $"{Guid.NewGuid()}@host.com", };
            var user2 = new ApplicationUser { UserName = Guid.NewGuid().ToString(), Email = $"{Guid.NewGuid()}@host.com", };

            var created1 = await UserManager.CreateAsync(user1, password);
            var created2 = await UserManager.CreateAsync(user2, password);

            bool userExist = await Context.Users.AnyAsync(u => u.Id == user1.Id);
            Assert.IsTrue(userExist);

            userExist = await Context.Users.AnyAsync(u => u.Id == user2.Id);
            Assert.IsTrue(userExist);

            var target = new AccountController(UserManager, ConfigMock.Object);

            var result = (OkResult)await target.Delete(user1.Id);
            Assert.AreEqual(200, result.StatusCode);

            userExist = await Context.Users.AnyAsync(u => u.Id == user1.Id);
            Assert.IsFalse(userExist);

            userExist = await Context.Users.AnyAsync(u => u.Id == user2.Id);
            Assert.IsTrue(userExist);
        }

        [TestMethod]
        public async Task AccountControllerTestChangeEmail()
        {
            string password = "ABCdef123!@#";

            var user = new ApplicationUser
            {
                UserName = Guid.NewGuid().ToString(),
                Email = $"{Guid.NewGuid()}@host.com",
            };

            var created = await UserManager.CreateAsync(user, password);

            Assert.IsTrue(created.Succeeded);

            user = await Context.Users.SingleAsync();

            var target = new AccountController(UserManager, ConfigMock.Object);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

            target.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = principal }
            };

            var request = new ApiUserModel { Email = Guid.NewGuid().ToString() + "@a.com" };

            var response = (OkResult)await target.Put(request);

            Assert.AreEqual(200, response.StatusCode);

            Assert.AreEqual(request.Email, user.Email);

            bool passwordNotChanged = await UserManager.CheckPasswordAsync(user, password);

            Assert.IsTrue(passwordNotChanged);
        }

        [TestMethod]
        public async Task AccountControllerTestChangeInvalidEmail()
        {
            string password = "ABCdef123!@#";

            var user = new ApplicationUser
            {
                UserName = Guid.NewGuid().ToString(),
                Email = $"{Guid.NewGuid()}@host.com",
            };

            var created = await UserManager.CreateAsync(user, password);

            Assert.IsTrue(created.Succeeded);

            user = await Context.Users.SingleAsync();

            var target = new AccountController(UserManager, ConfigMock.Object);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

            target.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = principal }
            };

            var request = new ApiUserModel { Email = "invalid email" };

            var response = (BadRequestObjectResult)await target.Put(request);

            Assert.AreEqual(400, response.StatusCode);
        }

        [TestMethod]
        public async Task AccountControllerTestChangeEmptyBody()
        {
            string password = "ABCdef123!@#";

            var user = new ApplicationUser
            {
                UserName = Guid.NewGuid().ToString(),
                Email = $"{Guid.NewGuid()}@host.com",
            };

            var created = await UserManager.CreateAsync(user, password);

            Assert.IsTrue(created.Succeeded);

            user = await Context.Users.SingleAsync();

            var target = new AccountController(UserManager, ConfigMock.Object);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

            target.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = principal }
            };

            var response = (BadRequestObjectResult)await target.Put(null);

            Assert.AreEqual(400, response.StatusCode);
        }

        [TestMethod]
        public async Task AccountControllerTestChangePassword()
        {
            string password = "ABCdef123!@#";
            string email = $"{Guid.NewGuid()}@host.com";

            var user = new ApplicationUser
            {
                UserName = Guid.NewGuid().ToString(),
                Email = email,
            };

            var created = await UserManager.CreateAsync(user, password);

            Assert.IsTrue(created.Succeeded);

            user = await Context.Users.SingleAsync();

            var target = new AccountController(UserManager, ConfigMock.Object);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

            target.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = principal }
            };

            var request = new ApiUserModel { Password = password, NewPassword = "NewPass123)(*" };

            var response = (OkResult)await target.Put(request);

            Assert.AreEqual(200, response.StatusCode);

            Assert.AreEqual(email, user.Email); // email should not change

            bool passwordChanged = await UserManager.CheckPasswordAsync(user, request.NewPassword);

            Assert.IsTrue(passwordChanged);
        }

        [TestMethod]
        public async Task AccountControllerTestChangeInvalidPassword()
        {
            string password = "ABCdef123!@#";
            string email = $"{Guid.NewGuid()}@host.com";

            var user = new ApplicationUser
            {
                UserName = Guid.NewGuid().ToString(),
                Email = email,
            };

            var created = await UserManager.CreateAsync(user, password);

            Assert.IsTrue(created.Succeeded);

            user = await Context.Users.SingleAsync();

            var target = new AccountController(UserManager, ConfigMock.Object);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

            target.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = principal }
            };

            var request = new ApiUserModel { Password = password, NewPassword = " " };

            var response = (BadRequestObjectResult)await target.Put(request);

            Assert.AreEqual(400, response.StatusCode);
        }
    }
}
