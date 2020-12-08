using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AspNetCoreJwtDemo.Data;
using AspNetCoreJwtDemo.Repositories;
using AspNetCoreJwtDemo.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AspNetCoreJwtDemo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _environment;
        private readonly AuthDbContext _context;
        private readonly IUserRepository _repository;

        public AuthController(IConfiguration config, IWebHostEnvironment environment, AuthDbContext context, IUserRepository repository)
        {
            _config = config;
            _environment = environment;
            _context = context;
            _repository = repository;
        }

        private string GetToken(UserViewModel vm)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, vm.Id.ToString()),
                new Claim(ClaimTypes.Name, vm.Name),
                
            };

            foreach (var role in vm.Roles)
            {
                claims.Add(new Claim("Roles", role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.GetSection("AuthConfig:Key").Value));

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = credentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login(UserLoginViewModel vm)
        {

            var status = await _repository.Login(vm);

            if (status)
            {
                var userViewModel = await _repository.Get(vm.Username);
                return Ok(new { token = GetToken(userViewModel) });
            }

            return Unauthorized();
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserRegisterViewModel viewModel)
        {
            await _repository.Register(viewModel);
            return Ok();
        }
    }
}
