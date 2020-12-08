using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreJwtDemo.Data;
using AspNetCoreJwtDemo.Models;
using AspNetCoreJwtDemo.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreJwtDemo.Repositories
{
    public interface IUserRepository
    {
        Task Register(UserRegisterViewModel viewModel);
        Task<UserViewModel> GetById(int id);
        Task<IEnumerable<string>> GetRoles();
        Task<bool> Login(UserLoginViewModel viewModel);
        Task<Role> GetRole(string role);
        Task<UserViewModel> Get(string identifier);
    }

    public class UserRepository : IUserRepository
    {
        private readonly AuthDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public UserRepository(AuthDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task Register(UserRegisterViewModel viewModel)
        {
            var user = new User { Email = viewModel.Email, Name = viewModel.Name};
            byte[] passwordHash, passwordSalt;

            CreatePasswordHash(viewModel.Password, out passwordHash, out passwordSalt);
            
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;

            await _context.AddAsync(user);
            await _context.SaveChangesAsync();

            foreach (var role in viewModel.Roles)
            {
                var modelRole = await GetRole(role);

                if (modelRole != null)
                {
                    var userRoleModel = new UserRole
                    {
                        User = user,
                        Role = modelRole,
                        UserId = user.Id,
                        RoleId = modelRole.Id
                    };

                    //  add user-roles
                    await _context.AddAsync(userRoleModel);
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<UserViewModel> Get(string identifier)
        {
            var model = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u =>
                    u.Name == identifier || u.Email == identifier);

            if (model == null)
            {
                return null;
            }

            var roles = model.UserRoles.Select(r => r.Role.Name).ToList();

            var viewModel = new UserViewModel{Name = model.Name, Email = model.Email, Id = model.Id};

            viewModel.Roles = roles.ToArray();

            return viewModel;
        }

        public async Task<UserViewModel> GetById(int id)
        {
            var model = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (model == null)
            {
                return null;
            }

            var viewModel = new UserViewModel {Email = model.Email, Name = model.Name, Id = model.Id};

            //  add roles
            var roles = new List<string>();

            foreach (var userRole in model.UserRoles)
            {
                roles.Add(userRole.Role.Name);
            }

            viewModel.Roles = roles.ToArray();

            return viewModel;
        }

        public async Task<IEnumerable<string>> GetRoles()
        {
            return await _context.Roles.Select(r => r.Name).Where(r => r != "Admin").ToListAsync();
        }

        public async Task<bool> Login(UserLoginViewModel viewModel)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Name == viewModel.Username || u.Email == viewModel.Username);

            return VerifyPasswordHash(viewModel.Password, user.PasswordHash, user.PasswordSalt);
        }

        public async Task<Role> GetRole(string role)
        {
            return await _context.Roles.FirstOrDefaultAsync(r => r.Name == role);
        }


        private void CreatePasswordHash(string rawPassword, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawPassword));
            }
        }

        public bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA512(passwordSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));

                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != passwordHash[i])
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
