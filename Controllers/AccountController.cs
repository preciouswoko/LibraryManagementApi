using LibraryManagement.Data.Models;
using LibraryManagement.Data.Response;
using LibraryManagement.Data.ViewModel;
using LibraryManagement.Service.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace LibraryManagementApi.Controllers
{
    public class AccountController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserRepository _user;
        private readonly IConfiguration _configuration;
        public AccountController(IUserRepository user,UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
        {
            _user = user;
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                var userRoles = await _userManager.GetRolesAsync(user);

                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };

                foreach (var userRole in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                }

                var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

                var token = new JwtSecurityToken(
                    issuer: _configuration["JWT:ValidIssuer"],
                    audience: _configuration["JWT:ValidAudience"],
                    expires: DateTime.Now.AddHours(3),
                    claims: authClaims,
                    signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                    );

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    expiration = token.ValidTo
                });
            }
            return Unauthorized();
        }

        //[HttpPost]
        //[Route("register")]
        //public async Task<IActionResult> Register([FromBody] User model)
        //{
        //    var userExists = await _userManager.FindByNameAsync(model.EmailAddress);
        //    if (userExists != null)
        //        return StatusCode(StatusCodes.Status500InternalServerError, new StandardResponse { ResponseCode = "Error", ResponseMessage = "User already exists!" , Data  = "null"});

        //    IdentityUser user = new IdentityUser()
        //    {
        //        Email = model.EmailAddress,
        //        SecurityStamp = Guid.NewGuid().ToString(),
        //        UserName = model.FirstName + model.LastName
        //    };
        //    var result = await _userManager.CreateAsync(user, model.Password);
        //    if (!result.Succeeded)
        //        return StatusCode(StatusCodes.Status500InternalServerError, new StandardResponse { ResponseCode = "Error", ResponseMessage = "User creation failed! Please check user details and try again.", Data = "null" });

        //    return Ok(new StandardResponse { ResponseCode = "Success", ResponseMessage = "User created successfully!" , Data  = "Null"});
        //}

        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> Register([FromBody] UserRequest model)
        {
            var userExists = await _userManager.FindByNameAsync(model.UserName);
            if (userExists != null)
                return StatusCode(StatusCodes.Status500InternalServerError, new StandardResponse { ResponseCode = "Error", ResponseMessage = $"{userExists} User already exists!", Data = "null" });

            
            User user = new User()
            {
                Email = model.EmailAddress,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = model.UserName,
                EmailAddress = model.EmailAddress,
                Address = model.Address,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Gender = model.Gender,
                PhoneNumber = model.PhoneNumber,
                Password = model.Password,
                Nationality = model.Nationality,

            };

            // Create User
            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return StatusCode(StatusCodes.Status500InternalServerError, new StandardResponse { ResponseCode = "Error", ResponseMessage = $"{result} User creation failed! Please check user details and try again.", Data = "null" });
            Log.Information($"Create User: {result}");
            //Checking roles in database and Creating if not exists
            if (!await _roleManager.RoleExistsAsync(ApplicationUserRoles.Admin))
                await _roleManager.CreateAsync(new IdentityRole(ApplicationUserRoles.Admin));
            if (!await _roleManager.RoleExistsAsync(ApplicationUserRoles.User))
                await _roleManager.CreateAsync(new IdentityRole(ApplicationUserRoles.User));
            //Add Role to user
            if(!string.IsNullOrEmpty(model.Role) && model.Role == ApplicationUserRoles.Admin)
            {
                await _userManager.AddToRoleAsync(user, ApplicationUserRoles.Admin);
               
            }
            else
            {
                await _userManager.AddToRoleAsync(user, ApplicationUserRoles.User);
               
            }

            //if (await _roleManager.RoleExistsAsync(ApplicationUserRoles.Admin))
            //{
            //    await _userManager.AddToRoleAsync(user, ApplicationUserRoles.Admin);
            //}
            Log.Information($"User created successfully!");
            return Ok(new StandardResponse { ResponseCode = "Success", ResponseMessage = "User created successfully!", Data = "Null" });
        }
    
}
}
