using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MediGuide.Application.DTOs;
using MediGuide.Domain.Entities;
using MediGuide.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

namespace MediGuide.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly MediGuideDbContext _context;
    private readonly IConfiguration _config;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        MediGuideDbContext context,
        IConfiguration config)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _context = context;
        _config = config;
    }

    [EnableRateLimiting("auth")]
    [HttpPost("register-patient")]
    public async Task<ActionResult<AuthResponseDto>> RegisterPatient(RegisterPatientDto dto)
    {
        // 1. Check if Identity user already exists
        if (await _userManager.FindByEmailAsync(dto.Email) is not null)
            return BadRequest("Email is already registered.");

        var existingPatient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Email == dto.Email);

        Patient patient;

        if (existingPatient is not null)
        {
            // If an orphaned patient record exists from a previous partial registration attempt, reuse it
            var userLinked = await _userManager.Users.AnyAsync(u => u.PatientId == existingPatient.Id);
            if (userLinked)
                return BadRequest("Email is already registered.");

            patient = existingPatient;
            patient.FullName = dto.FullName;
            patient.PhoneNumber = dto.PhoneNumber;
            patient.PreferredLanguage = dto.PreferredLanguage ?? "en";
            patient.DateOfBirth = dto.DateOfBirth.HasValue
                ? DateTime.SpecifyKind(dto.DateOfBirth.Value, DateTimeKind.Utc)
                : null;
            patient.Gender = dto.Gender;
            patient.EmergencyContactName = dto.EmergencyContactName;
            patient.EmergencyContactPhone = dto.EmergencyContactPhone;
            patient.Allergies = dto.Allergies;
            patient.ChronicConditions = dto.ChronicConditions;
            patient.CurrentMedications = dto.CurrentMedications;
            patient.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            patient = new Patient
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                PreferredLanguage = dto.PreferredLanguage ?? "en",
                DateOfBirth = dto.DateOfBirth.HasValue
                    ? DateTime.SpecifyKind(dto.DateOfBirth.Value, DateTimeKind.Utc)
                    : null,
                Gender = dto.Gender,
                EmergencyContactName = dto.EmergencyContactName,
                EmergencyContactPhone = dto.EmergencyContactPhone,
                Allergies = dto.Allergies,
                ChronicConditions = dto.ChronicConditions,
                CurrentMedications = dto.CurrentMedications
            };
            _context.Patients.Add(patient);
        }

        await _context.SaveChangesAsync();

        // 2. Create Identity user linked to the Patient
        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName,
            PhoneNumber = dto.PhoneNumber,
            PatientId = patient.Id
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            // If user creation failed (e.g. password complexity), don't leave orphaned patient if it was newly created
            if (existingPatient is null)
            {
                _context.Patients.Remove(patient);
                await _context.SaveChangesAsync();
            }

            return BadRequest(result.Errors.Select(e => e.Description));
        }

        // 3. Ensure role exists and assign it
        if (!await _roleManager.RoleExistsAsync("Patient"))
            await _roleManager.CreateAsync(new IdentityRole("Patient"));

        await _userManager.AddToRoleAsync(user, "Patient");

        return Ok(await CreateAuthResponseAsync(user));
    }

    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid email or password.");

        var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);
        if (result.IsLockedOut)
            return Problem(statusCode: StatusCodes.Status423Locked, title: "Account locked due to repeated failed sign-in attempts.");

        if (!result.Succeeded)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid email or password.");

        return Ok(await CreateAuthResponseAsync(user));
    }

    [EnableRateLimiting("auth")]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDto>> Refresh(RefreshTokenRequestDto dto)
    {
        if (!TryHashToken(dto.RefreshToken, out var tokenHash))
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid refresh token.");

        var token = await _context.RefreshTokens
            .SingleOrDefaultAsync(refreshToken => refreshToken.TokenHash == tokenHash);

        if (token is null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid refresh token.");

        if (token.ReplacedByTokenId.HasValue)
        {
            await RevokeActiveRefreshTokensAsync(token.ApplicationUserId, "Refresh token reuse detected.");
            return Problem(statusCode: StatusCodes.Status401Unauthorized,
                title: "Refresh token reuse detected. All active sessions have been revoked.");
        }

        if (token.IsRevoked || token.IsExpired)
        {
            if (!token.IsRevoked)
            {
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedReason = "Expired";
                await _context.SaveChangesAsync();
            }

            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Refresh token is expired or revoked.");
        }

        var user = await _userManager.FindByIdAsync(token.ApplicationUserId);
        if (user is null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid refresh token.");

        var replacementToken = CreateRefreshToken(user.Id);
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedReason = "Rotated";
        token.ReplacedByTokenId = replacementToken.Token.Id;
        _context.RefreshTokens.Add(replacementToken.Token);
        await _context.SaveChangesAsync();

        return Ok(await CreateAuthResponseAsync(user, replacementToken.PlainText));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshTokenRequestDto dto)
    {
        if (!TryHashToken(dto.RefreshToken, out var tokenHash))
            return NoContent();

        var token = await _context.RefreshTokens
            .SingleOrDefaultAsync(refreshToken => refreshToken.TokenHash == tokenHash);

        if (token is not null && !token.IsRevoked)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedReason = "User logout";
            await _context.SaveChangesAsync();
        }

        return NoContent();
    }

    private async Task<string> GenerateJwtToken(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new("fullName", user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (user.PatientId.HasValue)
            claims.Add(new Claim("patientId", user.PatientId.Value.ToString()));

        if (user.AgentId.HasValue)
            claims.Add(new Claim("agentId", user.AgentId.Value.ToString()));

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expireMinutes = int.Parse(_config["Jwt:ExpireMinutes"] ?? "60");

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expireMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("register-agent")]
    public async Task<ActionResult<AgentDto>> RegisterAgent(RegisterAgentDto dto)
    {
        if (await _userManager.FindByEmailAsync(dto.Email) is not null ||
            await _context.Agents.AnyAsync(a => a.Email == dto.Email))
            return BadRequest("Email is already registered.");

        // 1. Domain Agent
        var agent = new Agent
        {
            FullName = dto.FullName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            IsAvailable = true,
            IsActive = true
        };
        _context.Agents.Add(agent);
        await _context.SaveChangesAsync();

        // 2. Identity user linked to Agent
        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName,
            PhoneNumber = dto.PhoneNumber,
            AgentId = agent.Id
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            _context.Agents.Remove(agent);
            await _context.SaveChangesAsync();
            return BadRequest(result.Errors.Select(e => e.Description));
        }

        if (!await _roleManager.RoleExistsAsync("Agent"))
            await _roleManager.CreateAsync(new IdentityRole("Agent"));

        await _userManager.AddToRoleAsync(user, "Agent");

        var agentDto = new AgentDto(
            agent.Id,
            agent.FullName,
            agent.Email,
            agent.PhoneNumber,
            agent.Title,
            agent.Department,
            agent.Specialty,
            agent.IsAvailable,
            agent.IsActive);

        return CreatedAtAction(nameof(AgentsController.GetById), "Agents", new { id = agent.Id }, agentDto);
    }

    private async Task<AuthResponseDto> CreateAuthResponseAsync(ApplicationUser user, string? refreshToken = null)
    {
        var token = await GenerateJwtToken(user);
        var roles = await _userManager.GetRolesAsync(user);
        refreshToken ??= await IssueRefreshTokenAsync(user.Id);

        return new AuthResponseDto(
            token,
            refreshToken,
            user.Email!,
            user.FullName,
            roles,
            user.PatientId,
            user.AgentId);
    }

    private async Task<string> IssueRefreshTokenAsync(string userId)
    {
        var refreshToken = CreateRefreshToken(userId);
        _context.RefreshTokens.Add(refreshToken.Token);
        await _context.SaveChangesAsync();
        return refreshToken.PlainText;
    }

    private (RefreshToken Token, string PlainText) CreateRefreshToken(string userId)
    {
        var secret = RandomNumberGenerator.GetBytes(64);
        var plainText = Convert.ToBase64String(secret);
        return (new RefreshToken
        {
            ApplicationUserId = userId,
            TokenHash = Convert.ToHexString(SHA256.HashData(secret)),
            ExpiresAt = DateTime.UtcNow.AddDays(int.Parse(_config["Jwt:RefreshTokenDays"] ?? "7"))
        }, plainText);
    }

    private async Task RevokeActiveRefreshTokensAsync(string userId, string reason)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(token => token.ApplicationUserId == userId && token.RevokedAt == null)
            .ToListAsync();

        foreach (var activeToken in activeTokens)
        {
            activeToken.RevokedAt = DateTime.UtcNow;
            activeToken.RevokedReason = reason;
        }

        await _context.SaveChangesAsync();
    }

    private static bool TryHashToken(string token, out string tokenHash)
    {
        try
        {
            tokenHash = Convert.ToHexString(SHA256.HashData(Convert.FromBase64String(token)));
            return true;
        }
        catch (FormatException)
        {
            tokenHash = string.Empty;
            return false;
        }
    }
}
