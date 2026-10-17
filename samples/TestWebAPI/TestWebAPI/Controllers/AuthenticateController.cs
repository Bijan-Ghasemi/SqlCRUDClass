using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using JwtAuthDemo.Infrastructure;
using TestWebAPI.Log;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LaboratoryAPI.Authentication
{
    [Route("api/[controller]")]
    [ApiController]
    [Log]
    public class AuthenticateController : ControllerBase
    {
        private readonly ILogger<AuthenticateController> _logger;
        private readonly IJwtAuthManager _jwtAuthManager;
        public AuthenticateController(ILogger<AuthenticateController> logger, IJwtAuthManager jwtAuthManager)
        {
            _logger = logger;
            _jwtAuthManager = jwtAuthManager;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public ActionResult Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            Claim[] claims;
            claims = new[]
{
                    new Claim(ClaimTypes.Name,request.UserName)
                    ,new Claim(ClaimTypes.Role,"User")
                };

            var jwtResult = _jwtAuthManager.GenerateTokens(request.UserName, claims, DateTime.Now);
            _logger.LogInformation($"User [{request.UserName}] logged in the system.");
            return Ok(new LoginResult
            {
                UserName = request.UserName,
                AccessToken = jwtResult.AccessToken,
                ErrorMessage=""
            });
        }
        //public ActionResult GetCurrentUser()
        //{
        //    return Ok(new LoginResult
        //    {
        //        UserName = User.Identity.Name,
        //        Role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty,
        //        OriginalUserName = User.FindFirst("OriginalUserName")?.Value
        //    });
        //}

        //[HttpPost("logout")]
        //[Authorize]
        //public ActionResult Logout()
        //{
        //    // optionally "revoke" JWT token on the server side --> add the current token to a block-list
        //    // https://github.com/auth0/node-jsonwebtoken/issues/375

        //    var userName = User.Identity.Name;
        //    _jwtAuthManager.RemoveRefreshTokenByUserName(userName);
        //    _logger.LogInformation($"User [{userName}] logged out the system.");
        //    return Ok();
        //}

    }
    public class LoginRequest
    {
        [Required]
        [JsonPropertyName("username")]
        public string UserName { get; set; }

        [Required]
        [JsonPropertyName("password")]
        public string Password { get; set; }

    }

    public class LoginResult
    {
        [JsonPropertyName("username")]
        public string UserName { get; set; }

        [JsonPropertyName("accessToken")]
        public string AccessToken { get; set; }
        public string ErrorMessage { get; internal set; }
    }

    public class RefreshTokenRequest
    {
        [JsonPropertyName("refreshToken")]
        public string RefreshToken { get; set; }
    }

    public class ImpersonationRequest
    {
        [JsonPropertyName("username")]
        public string UserName { get; set; }
    }
}

