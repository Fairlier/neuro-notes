using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeuroNotes.Application.Common.Exceptions;
using NeuroNotes.Application.Features.Auth.Commands.LoginUser;
using NeuroNotes.Application.Features.Auth.Commands.RegisterUser;
using NeuroNotes.Application.Interfaces.Identity;
using NeuroNotes.Infrastructure.Identity.Models;
using NeuroNotes.Infrastructure.Persistence;
using System.Security.Authentication;

namespace NeuroNotes.Infrastructure.Identity.Services
{
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly NeuroNotesDbContext _dbContext;
        private readonly ILogger<IdentityService> _logger;

        public IdentityService(
            UserManager<AppUser> userManager,
            IJwtTokenGenerator jwtTokenGenerator,
            NeuroNotesDbContext dbContext,
            ILogger<IdentityService> logger)
        {
            _userManager = userManager;
            _jwtTokenGenerator = jwtTokenGenerator;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<RegisterUserResponse> CreateUserAsync(string email, string password)
        {
            _logger.LogInformation("Starts creating new user with Email {Email}.", email);

            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser is not null)
            {
                _logger.LogWarning("Registration failed. User with Email {Email} already exists.", email);
                throw new InvalidOperationException("User with this email already exists.");
            }

            var user = new AppUser
            {
                Email = email,
                UserName = email
            };

            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Registration failed for Email {Email}. Errors: {Errors}", email, errors);
                throw new InvalidOperationException($"Registration failed: {errors}");
            }

            _logger.LogInformation("User created successfully. UserId: {UserId}", user.Id);

            return new RegisterUserResponse { Id = user.Id };
        }

        public async Task<LoginUserResponse> LoginUserAsync(string email, string password, string ipAddress)
        {
            _logger.LogInformation("Authenticates user with Email {Email}.", email);

            var user = await _userManager.FindByEmailAsync(email);
            if (user is null || !await _userManager.CheckPasswordAsync(user, password))
            {
                _logger.LogWarning("Authentication failed for Email {Email}. Invalid credentials.", email);
                throw new InvalidCredentialException("Invalid email or password.");
            }

            return await GenerateAuthResponseAsync(user, ipAddress);
        }

        public async Task<LoginUserResponse> RefreshTokenAsync(string token, string ipAddress)
        {
            var existingRefreshToken = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == token);

            if (existingRefreshToken is null)
            {
                _logger.LogWarning("Refresh failed. Token not found.");
                throw new InvalidCredentialException("Invalid Refresh Token.");
            }

            if (existingRefreshToken.IsRevoked)
            {
                _logger.LogCritical("Security Alert: Token reuse detected for User {UserId}. Revoking all sessions.", existingRefreshToken.UserId);

                var userTokens = await _dbContext.RefreshTokens
                    .Where(rt => rt.UserId == existingRefreshToken.UserId && rt.Revoked == null)
                    .ToListAsync();

                foreach (var t in userTokens)
                {
                    t.Revoke(ipAddress, "Revoked due to reuse detection (Security Alert)");
                }

                await _dbContext.SaveChangesAsync();

                throw new InvalidCredentialException("Token reuse detected. All sessions revoked for security.");
            }

            if (existingRefreshToken.IsExpired)
            {
                _logger.LogWarning("Refresh failed. Token expired for User {UserId}.", existingRefreshToken.UserId);
                throw new InvalidCredentialException("Refresh Token expired.");
            }

            var user = await _userManager.FindByIdAsync(existingRefreshToken.UserId);
            if (user is null)
            {
                _logger.LogError("Refresh failed. User {UserId} not found.", existingRefreshToken.UserId);
                throw new NotFoundException(nameof(AppUser), existingRefreshToken.UserId);
            }

            _logger.LogInformation("Rotates refresh token for User {UserId}.", user.Id);

            var newRefreshToken = _jwtTokenGenerator.GenerateRefreshToken(user.Id, ipAddress);

            existingRefreshToken.Revoke(ipAddress, newRefreshToken.Token);

            _dbContext.RefreshTokens.Add(newRefreshToken);
            _dbContext.RefreshTokens.Update(existingRefreshToken);

            await _dbContext.SaveChangesAsync();

            var newAccessToken = _jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email!, user.UserName!);

            return new LoginUserResponse
            {
                Id = user.Id,
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken.Token
            };
        }

        private async Task<LoginUserResponse> GenerateAuthResponseAsync(AppUser user, string ipAddress)
        {
            _logger.LogInformation("Generates authentication tokens for User {UserId}.", user.Id);

            var accessToken = _jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email!, user.UserName!);
            var refreshToken = _jwtTokenGenerator.GenerateRefreshToken(user.Id, ipAddress);

            _dbContext.RefreshTokens.Add(refreshToken);
            await _dbContext.SaveChangesAsync();

            return new LoginUserResponse
            {
                Id = user.Id,
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token
            };
        }
    }
}
