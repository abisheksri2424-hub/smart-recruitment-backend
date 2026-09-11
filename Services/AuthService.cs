using Microsoft.AspNetCore.Identity;
using SmartRecruitment_Project.DTOs.Auth;
using SmartRecruitment_Project.Exceptions;
using SmartRecruitment_Project.Interfaces.Repositories;
using SmartRecruitment_Project.Interfaces.Services;
using SmartRecruitment_Project.Models;
using SmartRecruitment_Project.Models.Enums;

namespace SmartRecruitment_Project.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IJobSeekerRepository _jobSeekerRepository;
        private readonly IEmployerRepository _employerRepository;
        private readonly PasswordHasher<User> _passwordHasher;

        public AuthService(
            IAuthRepository authRepository,
            IJwtTokenService jwtTokenService,
            IJobSeekerRepository jobSeekerRepository,
            IEmployerRepository employerRepository)
        {
            _authRepository = authRepository;
            _jwtTokenService = jwtTokenService;
            _jobSeekerRepository = jobSeekerRepository;
            _employerRepository = employerRepository;

            _passwordHasher = new PasswordHasher<User>();
        }

        public async Task<AuthResponseDto> RegisterJobSeekerAsync(
            JobSeekerRegisterDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();

            var emailExists =
                await _authRepository.EmailExistsAsync(email);

            if (emailExists)
            {
                throw new ConflictException(
                    "An account already exists with this email.");
            }

            var user = new User
            {
                Email = email,
                Role = UserRole.JobSeeker,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            user.PasswordHash =
                _passwordHasher.HashPassword(
                    user,
                    dto.Password);

            var createdUser =
                await _authRepository.CreateUserAsync(user);

            var profile = new JobSeekerProfile
            {
                UserId = createdUser.Id,
                FullName = createdUser.Email.Split('@')[0]
            };

            await _jobSeekerRepository.AddProfileAsync(profile);
            await _jobSeekerRepository.SaveChangesAsync();

            var token =
                _jwtTokenService.GenerateToken(createdUser);

            return new AuthResponseDto
            {
                UserId = createdUser.Id,
                Email = createdUser.Email,
                Role = createdUser.Role.ToString(),
                Token = token,
                ExpiresAt = _jwtTokenService.GetTokenExpiry()
            };
        }

        public async Task<AuthResponseDto> RegisterEmployerAsync(
            EmployerRegisterDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();

            var emailExists =
                await _authRepository.EmailExistsAsync(email);

            if (emailExists)
            {
                throw new ConflictException(
                    "An account already exists with this email.");
            }

            var user = new User
            {
                Email = email,
                Role = UserRole.Employer,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            user.PasswordHash =
                _passwordHasher.HashPassword(
                    user,
                    dto.Password);

            var createdUser =
                await _authRepository.CreateUserAsync(user);

            var employerProfile = new EmployerProfile
            {
                UserId = createdUser.Id,
                CompanyName = createdUser.Email.Split('@')[0],
                UpdatedAt = DateTime.UtcNow
            };

            await _employerRepository.CreateAsync(employerProfile);

            var token =
                _jwtTokenService.GenerateToken(createdUser);

            return new AuthResponseDto
            {
                UserId = createdUser.Id,
                Email = createdUser.Email,
                Role = createdUser.Role.ToString(),
                Token = token,
                ExpiresAt = _jwtTokenService.GetTokenExpiry()
            };
        }

        public async Task<AuthResponseDto> LoginAsync(
            LoginDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();

            Console.WriteLine("====================================");
            Console.WriteLine("LOGIN REQUEST");
            Console.WriteLine($"EMAIL ENTERED: {email}");

            var user =
                await _authRepository.GetByEmailAsync(email);

            Console.WriteLine($"USER FOUND: {user != null}");

            if (user == null)
            {
                Console.WriteLine("LOGIN FAILED: User not found");
                Console.WriteLine("====================================");

                throw new UnauthorizedException(
                    "Invalid email or password.");
            }

            Console.WriteLine($"DB EMAIL: {user.Email}");
            Console.WriteLine($"ROLE: {user.Role}");
            Console.WriteLine($"ACTIVE: {user.IsActive}");

            if (!user.IsActive)
            {
                Console.WriteLine("LOGIN FAILED: Account inactive");
                Console.WriteLine("====================================");

                throw new UnauthorizedException(
                    "This account is inactive.");
            }

            var result =
                _passwordHasher.VerifyHashedPassword(
                    user,
                    user.PasswordHash,
                    dto.Password);

            Console.WriteLine($"PASSWORD RESULT: {result}");

            if (result == PasswordVerificationResult.Failed)
            {
                Console.WriteLine("LOGIN FAILED: Password incorrect");
                Console.WriteLine("====================================");

                throw new UnauthorizedException(
                    "Invalid email or password.");
            }

            var token =
                _jwtTokenService.GenerateToken(user);

            Console.WriteLine("LOGIN SUCCESS");
            Console.WriteLine($"USER: {user.Email}");
            Console.WriteLine($"ROLE: {user.Role}");
            Console.WriteLine("====================================");

            return new AuthResponseDto
            {
                UserId = user.Id,
                Email = user.Email,
                Role = user.Role.ToString(),
                Token = token,
                ExpiresAt = _jwtTokenService.GetTokenExpiry()
            };
        }
    }
}