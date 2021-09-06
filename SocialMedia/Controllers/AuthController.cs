using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SocialMedia.Models;
using SocialMedia.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.Extensions.Configuration;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.IdentityModel;
using System.Security;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace SocialMedia.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : Controller
    {
        IMongoCollection<User> _userCollectionProcessor;
        UserService _userService;
        public AuthController(UserService userService)
        {
            _userCollectionProcessor = userService.GetUserCollectionProcessor();
            _userService = userService;
        }

        [Route("register")]
        [HttpPost]
        public ActionResult Register(User user)
        {
            if (_userCollectionProcessor.Find(x => x.Username == user.Username).FirstOrDefault() != null)
            {
                ModelState.AddModelError("errors", "This user already exists");
                return BadRequest(ModelState);
            }

            string salt = BCrypt.Net.BCrypt.GenerateSalt(12);
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(user.Password, salt);

            _userCollectionProcessor.InsertOne(new User { Username = user.Username, Password = hashedPassword });

            return Ok();
        }

        [Route("login")]
        [HttpPost]
        public ActionResult Login(User user)
        {
            var registeredUser = _userCollectionProcessor.Find<User>(x => x.Username == user.Username).FirstOrDefault();

            if (registeredUser == null)
            {
                ModelState.AddModelError("errors", "This user does not exist");
                return BadRequest(ModelState);
            }
            else if (!BCrypt.Net.BCrypt.Verify(user.Password, registeredUser.Password))
            {
                ModelState.AddModelError("errors", "This password is incorrect");
                return BadRequest(ModelState);
            }

            string token = _userService.CreateToken(new Claim[] { new Claim("Username", user.Username) }, DateTime.Now.AddDays(3));

            return Ok(token);
        }
    }
}
