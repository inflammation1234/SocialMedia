using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SocialMedia.Models;
using SocialMedia.Services;
using System;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.IO;

namespace SocialMedia.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SettingsController : Controller
    {
        IMongoCollection<User> _userCollectionProcessor;
        UserService _userService;
        public SettingsController(UserService userService)
        {
            _userCollectionProcessor = userService.GetUserCollectionProcessor();
            _userService = userService;
        }

        [Route("setProfilePhoto")]
        public ActionResult SetProfilePhoto(IFormFile image)
        {
            var headers = Request.Headers;

            if(!headers.ContainsKey("Authorization"))
            {
                ModelState.AddModelError("errors", "Authorization header cannot be empty");
                return BadRequest(ModelState);
            }

            var token = headers["Authorization"].ToString().Split(" ")[1];
            Debug.WriteLine(token);

            if(!_userService.ValidateToken(token))
            {
                ModelState.AddModelError("errors", "Token expired or incorrect");
                return BadRequest(ModelState);
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.ReadJwtToken(token);
            var claimEnumerator = securityToken.Claims.GetEnumerator();
            string username = null;

            while(claimEnumerator.MoveNext())
                if(claimEnumerator.Current.Type == "Username")
                {
                    username = claimEnumerator.Current.Value;
                    break;
                }

            Debug.WriteLine(username);

            string fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);

            using (FileStream fs = new FileStream(Environment.CurrentDirectory + "\\profiles\\" + fileName, FileMode.Create))
                image.CopyTo(fs);

            var update = Builders<User>.Update.Set(x => x.ProfilePhoto, "profiles\\" + fileName);

            _userCollectionProcessor.UpdateOne(x => x.Username == username, update);

            return Ok();
        }
    }
}
