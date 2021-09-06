using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using SocialMedia.Configurations;
using SocialMedia.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace SocialMedia.Services
{
    public class UserService
    {
        private IMongoCollection<User> _userCollection;
        private SocialMediaMongoConfiguration _mongoConfigurationSettings;
        private IConfiguration _jwtConfig;

        public UserService(IOptions<SocialMediaMongoConfiguration> mongoConfigurationSettings, IConfiguration jwtConfig)
        {
            _jwtConfig = jwtConfig;
            _mongoConfigurationSettings = mongoConfigurationSettings.Value;
            var mongoClient = new MongoClient(_mongoConfigurationSettings.ConnectionString);
            var mongoDatabase = mongoClient.GetDatabase(_mongoConfigurationSettings.Database);
            _userCollection = mongoDatabase.GetCollection<User>(_mongoConfigurationSettings.Collection);
        }

        public IMongoCollection<User> GetUserCollectionProcessor() => _userCollection;

        public bool ValidateToken(string token)
        {
            try
            {
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.GetValue<string>("JWT:SecurityKey")));
                var tokenHandler = new JwtSecurityTokenHandler();

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    IssuerSigningKey = securityKey,
                    ValidIssuer = "https://www.demo.com",
                    ValidAudience = "https://www.demo.com",
                }, out SecurityToken validatedToken);

            }
            catch
            {
                return false;
            }
            return true;
        }

        public string CreateToken(Claim[] claims, DateTime exp)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.GetValue<string>("JWT:SecurityKey")));
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = exp,
                Issuer = "https://www.demo.com",
                Audience = "https://www.demo.com",
                SigningCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature)
            };

            return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
        }
    }
}
