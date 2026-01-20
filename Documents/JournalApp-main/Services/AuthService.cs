using System.Security.Cryptography;
using System.Text;
using JournalApp.Models;
using JournalApp.Data;
using Microsoft.EntityFrameworkCore;

namespace JournalApp.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<bool> LogoutAsync();
        Task<UserProfileDto> GetUserProfileAsync(int userId);
        Task<bool> UpdateUserProfileAsync(int userId, UserProfileDto profileDto);
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
        Task InitializeDatabaseAsync();
        Task ResetDatabaseAsync();
    }

    public class AuthService : IAuthService
    {
        private readonly MyAppContext _context;

        public AuthService(MyAppContext context)
        {
            _context = context;
        }
        
        public async Task InitializeDatabaseAsync()
        {
            // Ensure database is created
            await _context.Database.EnsureCreatedAsync();
            
            // Optionally, run migrations if needed
            // await _context.Database.MigrateAsync();
        }
        
        public async Task ResetDatabaseAsync()
        {
            // Delete and recreate the database
            await _context.Database.EnsureDeletedAsync();
            await _context.Database.EnsureCreatedAsync();
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            try
            {
                // Check if user already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == registerDto.Username || u.Email == registerDto.Email);

                if (existingUser != null)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = existingUser.Username == registerDto.Username ? 
                            "Username already exists." : "Email already exists."
                    };
                }

                // Hash the password
                var passwordHash = HashPassword(registerDto.Password);

                var user = new User
                {
                    Username = registerDto.Username,
                    Email = registerDto.Email,
                    PasswordHash = passwordHash,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var userProfile = new UserProfileDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                };

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Registration successful",
                    User = userProfile,
                    Token = GenerateToken(user.Id, user.Username)
                };
            }
            catch (Exception ex)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = $"Registration failed: {ex.Message}"
                };
            }
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

                if (user == null || !VerifyPassword(loginDto.Password, user.PasswordHash))
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    };
                }

                // Update last login time
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var userProfile = new UserProfileDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                };

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Login successful",
                    User = userProfile,
                    Token = GenerateToken(user.Id, user.Username)
                };
            }
            catch (Exception ex)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = $"Login failed: {ex.Message}"
                };
            }
        }

        public async Task<bool> LogoutAsync()
        {
            // In a real application, you might invalidate tokens here
            // For now, we'll just return true
            return true;
        }

        public async Task<UserProfileDto> GetUserProfileAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return null;

            return new UserProfileDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }

        public async Task<bool> UpdateUserProfileAsync(int userId, UserProfileDto profileDto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            user.Username = profileDto.Username;
            user.Email = profileDto.Email;

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            if (!VerifyPassword(currentPassword, user.PasswordHash))
                return false;

            user.PasswordHash = HashPassword(newPassword);

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private bool VerifyPassword(string password, string hash)
        {
            var passwordHash = HashPassword(password);
            return passwordHash == hash;
        }

        private string GenerateToken(int userId, string username)
        {
            // In a real application, you would generate a proper JWT token
            // For now, we'll return a simple identifier
            return $"token_{userId}_{username}_{DateTime.UtcNow.Ticks}";
        }
    }
}