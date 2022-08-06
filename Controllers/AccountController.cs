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
        private readonly IUserService _user;
        private readonly IConfiguration _configuration;
        public AccountController(IUserService user,UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
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
            try
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
            catch (Exception e)
            {
                return StatusCode(500, e);
            }
            
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
            try
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
                    Role = model.Role,

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
                if (!string.IsNullOrEmpty(model.Role) && model.Role == ApplicationUserRoles.Admin)
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
            catch (Exception e)
            {
                return StatusCode(500, e);
            }
            
        }

        [HttpGet]
        [Route("GetUsers")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                return Ok( _user.GetUsers());
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "Error retrieving data from the database");
            }
        }

        // GET: AccountController/Id
        [Route("Getuser")]
        [HttpGet]
        public async Task<IActionResult> Getuser(int userid)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var val = await _user.Getuser(userid);
                    Log.Information($" retrieved information from GetUser {val}");
                    if (val == null)
                    {
                        return (StatusCode(400, new StandardResponse { ResponseCode = "99", ResponseMessage = "Id Not Found", Data = null }));
                    }
                    return Ok(new StandardResponse { ResponseCode = "00", ResponseMessage = "Sucessfull", Data = val });
                }
                return (StatusCode(400, new StandardResponse { ResponseCode = "99", ResponseMessage = "Id Not Found", Data = null }));
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }


        }



        // PUT: AccountController/Edit
        [Route("UpdateUser")]
        [HttpPut]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserRequest request)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    bool res = await _user.Updateuser(id, request);
                    if (res == false)
                    {
                        return (StatusCode(400, new StandardResponse { ResponseCode = "99", ResponseMessage = "Id Not Found", Data = null }));
                    }
                    return Ok(new StandardResponse { ResponseCode = "00", ResponseMessage = "Sucessfull", Data = null });
                }
                return (StatusCode(400, new StandardResponse { ResponseCode = "99", ResponseMessage = "invalid details", Data = null }));

            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }

        }

        // DELETE AccountController/Delete
        [Route("DeleteUser")]
        [HttpDelete]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                bool res = await _user.DeleteUser(id);
                if (res == false)
                {
                    return (StatusCode(400, new StandardResponse { ResponseCode = "99", ResponseMessage = "Id Not Found", Data = null }));
                }
                return Ok(new StandardResponse { ResponseCode = "00", ResponseMessage = "Sucessfull", Data = null });
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }

        }
        //[HttpGet("{search}")]
        //public async Task<ActionResult<IEnumerable<Employee>>> Search(string name, Gender? gender)
        //{
        //    try
        //    {
        //        var result = await employeeRepository.Search(name, gender);

        //        if (result.Any())
        //        {
        //            return Ok(result);
        //        }

        //        return NotFound();
        //    }
        //    catch (Exception)
        //    {
        //        return StatusCode(StatusCodes.Status500InternalServerError,
        //            "Error retrieving data from the database");
        //    }
        //}



    }
}
