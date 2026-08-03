using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MediGuide.Application.DTOs;
using MediGuide.Domain.Entities;
using MediGuide.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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

    [HttpPost("register-patient")]
    public async Task<ActionResult<AuthResponseDto>> RegisterPatient(RegisterPatientDto dto)
    {
        if (await _userManager.FindByEmailAsync(dto.Email) is not null)
            return BadRequest("Email is already registered.");

        // 1. Create domain Patient
        var patient = new Patient
        {
            FullName = dto.FullName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            PreferredLanguage = dto.PreferredLanguage ?? "en"
        };
        _context.Patients.Add(patient);
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
            return BadRequest(result.Errors.Select(e => e.Description));

        // 3. Ensure role exists and assign it
        if (!await _roleManager.RoleExistsAsync("Patient"))
            await _roleManager.CreateAsync(new IdentityRole("Patient"));

        await _userManager.AddToRoleAsync(user, "Patient");

        var token = await GenerateJwtToken(user);
        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new AuthResponseDto(token, user.Email!, user.FullName, roles, patient.Id, null));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null)
            return Unauthorized("Invalid email or password.");

        var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
        if (!result.Succeeded)
            return Unauthorized("Invalid email or password.");

        var token = await GenerateJwtToken(user);
        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new AuthResponseDto(
            token,
            user.Email!,
            user.FullName,
            roles,
            user.PatientId,
            user.AgentId));
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
}