using System.Security.Claims;
using MediGuide.Application.DTOs;
using MediGuide.Domain.Entities;
using MediGuide.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediGuide.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly MediGuideDbContext _context;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        MediGuideDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    private const int MaxPageSize = 100;

    // GET: api/users/me
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                     ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found.");

        var roles = await _userManager.GetRolesAsync(user);

        Agent? agent = null;
        if (user.AgentId.HasValue)
        {
            agent = await _context.Agents.FindAsync(user.AgentId.Value);
        }
        else if (!string.IsNullOrEmpty(user.Email))
        {
            agent = await _context.Agents.FirstOrDefaultAsync(a => a.Email == user.Email);
            if (agent != null)
            {
                user.AgentId = agent.Id;
                await _userManager.UpdateAsync(user);
            }
        }

        return Ok(new UserProfileDto(
            user.Id,
            user.FullName,
            user.Email ?? string.Empty,
            user.PhoneNumber,
            roles,
            user.PatientId,
            user.AgentId,
            user.CreatedAt,
            agent?.Title,
            agent?.Department,
            agent?.Specialty,
            agent?.IsAvailable));
    }

    // PATCH: api/users/me
    [Authorize]
    [HttpPatch("me")]
    public async Task<ActionResult<UserProfileDto>> UpdateCurrentUser(UpdateUserProfileDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                     ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found.");

        user.FullName = dto.FullName;
        user.PhoneNumber = dto.PhoneNumber;

        // Synchronize linked domain entities if present
        if (user.PatientId.HasValue)
        {
            var patient = await _context.Patients.FindAsync(user.PatientId.Value);
            if (patient != null)
            {
                patient.FullName = dto.FullName;
                patient.PhoneNumber = dto.PhoneNumber ?? patient.PhoneNumber;
                patient.UpdatedAt = DateTime.UtcNow;
            }
        }

        Agent? agent = null;
        if (user.AgentId.HasValue)
        {
            agent = await _context.Agents.FindAsync(user.AgentId.Value);
        }
        else if (!string.IsNullOrEmpty(user.Email))
        {
            agent = await _context.Agents.FirstOrDefaultAsync(a => a.Email == user.Email);
            if (agent != null)
            {
                user.AgentId = agent.Id;
            }
        }

        if (agent != null)
        {
            agent.FullName = dto.FullName;
            agent.PhoneNumber = dto.PhoneNumber ?? agent.PhoneNumber;
            if (dto.Title != null) agent.Title = dto.Title;
            if (dto.Department != null) agent.Department = dto.Department;
            if (dto.Specialty != null) agent.Specialty = dto.Specialty;
            if (dto.IsAvailable.HasValue) agent.IsAvailable = dto.IsAvailable.Value;
            agent.UpdatedAt = DateTime.UtcNow;
        }

        // Change password if requested
        if (!string.IsNullOrWhiteSpace(dto.CurrentPassword) && !string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            var passResult = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!passResult.Succeeded)
                return BadRequest(passResult.Errors.Select(e => e.Description));
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return BadRequest(updateResult.Errors.Select(e => e.Description));

        await _context.SaveChangesAsync();

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new UserProfileDto(
            user.Id,
            user.FullName,
            user.Email ?? string.Empty,
            user.PhoneNumber,
            roles,
            user.PatientId,
            user.AgentId,
            user.CreatedAt,
            agent?.Title,
            agent?.Department,
            agent?.Specialty,
            agent?.IsAvailable));
    }

    // GET: api/users (Admin only)
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminUserDto>>> GetAllUsers([FromQuery] UserQueryParams query)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : Math.Min(query.PageSize, MaxPageSize);

        var usersQuery = _userManager.Users.Include(u => u.Agent).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            usersQuery = usersQuery.Where(u =>
                EF.Functions.ILike(u.FullName, $"%{search}%") ||
                EF.Functions.ILike(u.Email!, $"%{search}%") ||
                (u.PhoneNumber != null && EF.Functions.ILike(u.PhoneNumber, $"%{search}%")));
        }

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            var role = await _roleManager.FindByNameAsync(query.Role);
            if (role != null)
            {
                var userIdsInRole = _context.UserRoles
                    .Where(ur => ur.RoleId == role.Id)
                    .Select(ur => ur.UserId);

                usersQuery = usersQuery.Where(u => userIdsInRole.Contains(u.Id));
            }
            else
            {
                return Ok(new PagedResult<AdminUserDto>(new List<AdminUserDto>(), 0, page, pageSize));
            }
        }

        var totalCount = await usersQuery.CountAsync();

        var rawUsers = await usersQuery
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = new List<AdminUserDto>();
        foreach (var user in rawUsers)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;
            dtos.Add(new AdminUserDto(
                user.Id,
                user.FullName,
                user.Email ?? string.Empty,
                user.PhoneNumber,
                roles,
                !isLocked,
                user.CreatedAt,
                user.PatientId,
                user.AgentId,
                user.Agent?.Title,
                user.Agent?.Department,
                user.Agent?.Specialty));
        }

        return Ok(new PagedResult<AdminUserDto>(dtos, totalCount, page, pageSize));
    }

    // GET: api/users/{id} (Admin only)
    [Authorize(Roles = "Admin")]
    [HttpGet("{id}")]
    public async Task<ActionResult<AdminUserDto>> GetUserById(string id)
    {
        var user = await _userManager.Users
            .Include(u => u.Agent)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return NotFound("User not found.");

        var roles = await _userManager.GetRolesAsync(user);
        var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

        return Ok(new AdminUserDto(
            user.Id,
            user.FullName,
            user.Email ?? string.Empty,
            user.PhoneNumber,
            roles,
            !isLocked,
            user.CreatedAt,
            user.PatientId,
            user.AgentId,
            user.Agent?.Title,
            user.Agent?.Department,
            user.Agent?.Specialty));
    }

    // POST: api/users (Admin only)
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<AdminUserDto>> CreateUser(AdminCreateUserDto dto)
    {
        if (await _userManager.FindByEmailAsync(dto.Email) != null)
            return BadRequest("Email is already registered.");

        var role = string.IsNullOrWhiteSpace(dto.Role) ? "Patient" : dto.Role;
        if (!await _roleManager.RoleExistsAsync(role))
            await _roleManager.CreateAsync(new IdentityRole(role));

        Guid? patientId = null;
        Guid? agentId = null;
        Agent? createdAgent = null;

        if (role.Equals("Patient", StringComparison.OrdinalIgnoreCase))
        {
            var patient = new Patient
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber ?? string.Empty,
                PreferredLanguage = "en",
            };
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();
            patientId = patient.Id;
        }
        else if (role.Equals("Agent", StringComparison.OrdinalIgnoreCase))
        {
            createdAgent = new Agent
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber ?? string.Empty,
                Title = dto.Title,
                Department = dto.Department,
                Specialty = dto.Specialty,
                IsAvailable = true,
                IsActive = true
            };
            _context.Agents.Add(createdAgent);
            await _context.SaveChangesAsync();
            agentId = createdAgent.Id;
        }

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName,
            PhoneNumber = dto.PhoneNumber,
            PatientId = patientId,
            AgentId = agentId
        };

        var createResult = await _userManager.CreateAsync(user, dto.Password);
        if (!createResult.Succeeded)
            return BadRequest(createResult.Errors.Select(e => e.Description));

        await _userManager.AddToRoleAsync(user, role);

        var roles = await _userManager.GetRolesAsync(user);

        var adminDto = new AdminUserDto(
            user.Id,
            user.FullName,
            user.Email,
            user.PhoneNumber,
            roles,
            true,
            user.CreatedAt,
            user.PatientId,
            user.AgentId,
            createdAgent?.Title,
            createdAgent?.Department,
            createdAgent?.Specialty);

        return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, adminDto);
    }

    // PATCH: api/users/{id} (Admin only)
    [Authorize(Roles = "Admin")]
    [HttpPatch("{id}")]
    public async Task<ActionResult<AdminUserDto>> UpdateUser(string id, AdminUpdateUserDto dto)
    {
        var user = await _userManager.Users
            .Include(u => u.Agent)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return NotFound("User not found.");

        // Role immutability: once created, user roles cannot be changed by anyone
        if (!string.IsNullOrWhiteSpace(dto.Role))
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (!currentRoles.Contains(dto.Role, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest("User roles are permanent and cannot be modified once created.");
            }
        }

        if (!string.IsNullOrWhiteSpace(dto.FullName))
            user.FullName = dto.FullName;

        if (dto.PhoneNumber != null)
            user.PhoneNumber = dto.PhoneNumber;

        // Toggle active / lockout
        if (dto.IsActive.HasValue)
        {
            if (dto.IsActive.Value)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
            }
            else
            {
                // Safety guard: at least one active administrator must remain
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
                {
                    var isCurrentlyActive = !user.LockoutEnd.HasValue || user.LockoutEnd.Value <= DateTimeOffset.UtcNow;
                    if (isCurrentlyActive)
                    {
                        var adminRole = await _roleManager.FindByNameAsync("Admin");
                        if (adminRole != null)
                        {
                            var activeAdminCount = await (
                                from u in _userManager.Users
                                join ur in _context.UserRoles on u.Id equals ur.UserId
                                where ur.RoleId == adminRole.Id && (u.LockoutEnd == null || u.LockoutEnd <= DateTimeOffset.UtcNow)
                                select u.Id
                            ).CountAsync();

                            if (activeAdminCount <= 1)
                            {
                                return BadRequest("Cannot deactivate the last active Administrator account. At least one active Administrator must be maintained.");
                            }
                        }
                    }
                }

                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            }
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return BadRequest(updateResult.Errors.Select(e => e.Description));

        // Sync linked entity names and agent details
        Patient? patient = null;
        if (user.PatientId.HasValue)
        {
            patient = await _context.Patients.FindAsync(user.PatientId.Value);
        }
        else if (!string.IsNullOrEmpty(user.Email))
        {
            patient = await _context.Patients.FirstOrDefaultAsync(p => p.Email == user.Email);
            if (patient != null) user.PatientId = patient.Id;
        }

        if (patient != null)
        {
            patient.FullName = user.FullName;
            patient.PhoneNumber = user.PhoneNumber ?? patient.PhoneNumber;
            if (dto.IsActive.HasValue) patient.IsActive = dto.IsActive.Value;
            patient.UpdatedAt = DateTime.UtcNow;
        }

        Agent? agent = null;
        if (user.AgentId.HasValue)
        {
            agent = await _context.Agents.FindAsync(user.AgentId.Value);
        }
        else if (!string.IsNullOrEmpty(user.Email))
        {
            agent = await _context.Agents.FirstOrDefaultAsync(a => a.Email == user.Email);
            if (agent != null) user.AgentId = agent.Id;
        }

        if (agent != null)
        {
            agent.FullName = user.FullName;
            agent.PhoneNumber = user.PhoneNumber ?? agent.PhoneNumber;
            if (dto.Title != null) agent.Title = dto.Title;
            if (dto.Department != null) agent.Department = dto.Department;
            if (dto.Specialty != null) agent.Specialty = dto.Specialty;
            if (dto.IsActive.HasValue) agent.IsActive = dto.IsActive.Value;
            agent.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        var finalRoles = await _userManager.GetRolesAsync(user);
        var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

        return Ok(new AdminUserDto(
            user.Id,
            user.FullName,
            user.Email ?? string.Empty,
            user.PhoneNumber,
            finalRoles,
            !isLocked,
            user.CreatedAt,
            user.PatientId,
            user.AgentId,
            user.Agent?.Title,
            user.Agent?.Department,
            user.Agent?.Specialty));
    }

    // DELETE: api/users/{id} (Admin only)
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeactivateUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound("User not found.");

        // Safety guard: at least one active administrator must remain
        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
        {
            var isCurrentlyActive = !user.LockoutEnd.HasValue || user.LockoutEnd.Value <= DateTimeOffset.UtcNow;
            if (isCurrentlyActive)
            {
                var adminRole = await _roleManager.FindByNameAsync("Admin");
                if (adminRole != null)
                {
                    var activeAdminCount = await (
                        from u in _userManager.Users
                        join ur in _context.UserRoles on u.Id equals ur.UserId
                        where ur.RoleId == adminRole.Id && (u.LockoutEnd == null || u.LockoutEnd <= DateTimeOffset.UtcNow)
                        select u.Id
                    ).CountAsync();

                    if (activeAdminCount <= 1)
                    {
                        return BadRequest("Cannot deactivate the last active Administrator account. At least one active Administrator must be maintained.");
                    }
                }
            }
        }

        // Safe soft-deactivation via lockout
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));

        if (user.PatientId.HasValue)
        {
            var patient = await _context.Patients.FindAsync(user.PatientId.Value);
            if (patient != null)
            {
                patient.IsActive = false;
                patient.UpdatedAt = DateTime.UtcNow;
            }
        }

        if (user.AgentId.HasValue)
        {
            var agent = await _context.Agents.FindAsync(user.AgentId.Value);
            if (agent != null)
            {
                agent.IsActive = false;
                agent.IsAvailable = false;
                agent.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }
}
