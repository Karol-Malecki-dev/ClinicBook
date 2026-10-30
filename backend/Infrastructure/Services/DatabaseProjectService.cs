using Application.DTOs.Project;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Responses;

namespace Infrastructure.Services;

public sealed class DatabaseProjectService : IProjectService
{
    private readonly ApplicationDbContext _dbContext;

    public DatabaseProjectService(ApplicationDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<List<ProjectDto>>> GetUserProjectsAsync(Guid ownerId, bool includeArchived = false, string scope = "all")
    {
        var query = _dbContext.Projects
            .AsNoTracking()
            .Include(project => project.Members)
            .Where(project => project.Members.Any(member => member.UserId == ownerId));
        if (!includeArchived) query = query.Where(project => !project.IsArchived);
        if (scope.Equals("owned", StringComparison.OrdinalIgnoreCase)) query = query.Where(project => project.OwnerId == ownerId);

        var projects = await query.OrderByDescending(project => project.UpdatedAt).ToListAsync();
        return ApiResponse<List<ProjectDto>>.Success(projects.Select(project => Map(project, ownerId)).ToList());
    }

    public async Task<ApiResponse<ProjectDto>> GetProjectAsync(Guid ownerId, Guid projectId, bool includeArchived = false)
    {
        var project = await _dbContext.Projects
            .AsNoTracking()
            .Include(item => item.Members)
            .FirstOrDefaultAsync(item =>
                item.Id == projectId
                && item.Members.Any(member => member.UserId == ownerId)
                && (includeArchived || !item.IsArchived));
        return project is null ? ApiResponse<ProjectDto>.Error(404, "Project not found.") : ApiResponse<ProjectDto>.Success(Map(project, ownerId));
    }

    public async Task<ApiResponse<ProjectDto>> CreateProjectAsync(Guid ownerId, CreateProjectDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (ownerId == Guid.Empty) return ApiResponse<ProjectDto>.Error(400, "Owner ID cannot be empty.");
        if (string.IsNullOrWhiteSpace(dto.Name)) return ApiResponse<ProjectDto>.Error(400, "Project name cannot be empty.");

        var project = new Project { OwnerId = ownerId, Name = dto.Name.Trim(), Description = dto.Description?.Trim() };
        project.Members.Add(new ProjectMember { ProjectId = project.Id, UserId = ownerId, Role = ProjectMemberRole.Owner });
        _dbContext.Projects.Add(project);
        await _dbContext.SaveChangesAsync();
        return ApiResponse<ProjectDto>.Success(Map(project, ownerId), "Project created successfully.", 201);
    }

    public async Task<ApiResponse<ProjectDto>> UpdateProjectAsync(Guid ownerId, Guid projectId, UpdateProjectDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var project = await GetOwnedProjectAsync(ownerId, projectId);
        if (project is null) return ApiResponse<ProjectDto>.Error(404, "Project not found.");
        if (string.IsNullOrWhiteSpace(dto.Name)) return ApiResponse<ProjectDto>.Error(400, "Project name cannot be empty.");
        project.Name = dto.Name.Trim(); project.Description = dto.Description?.Trim(); project.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return ApiResponse<ProjectDto>.Success(Map(project, ownerId), "Project updated successfully.");
    }

    public async Task<ApiResponse<bool>> ArchiveProjectAsync(Guid ownerId, Guid projectId)
    {
        var project = await GetOwnedProjectAsync(ownerId, projectId);
        if (project is null) return ApiResponse<bool>.Error(404, "Project not found.");
        project.IsArchived = true; project.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return ApiResponse<bool>.Success(true, "Project archived successfully.");
    }

    public async Task<ApiResponse<List<ProjectMemberDto>>> GetProjectMembersAsync(Guid ownerId, Guid projectId)
    {
        if (!await CanAccessProjectAsync(ownerId, projectId)) return ApiResponse<List<ProjectMemberDto>>.Error(404, "Project not found.");
        var members = await _dbContext.ProjectMembers.AsNoTracking().Include(member => member.User).Where(member => member.ProjectId == projectId).ToListAsync();
        return ApiResponse<List<ProjectMemberDto>>.Success(members.Select(member => new ProjectMemberDto { UserId = member.UserId, DisplayName = member.User.DisplayName, Email = member.User.Email, Role = member.Role, AddedAt = member.AddedAt }).ToList());
    }

    public async Task<ApiResponse<List<ProjectMemberUserDto>>> GetAvailableProjectMembersAsync(Guid ownerId, Guid projectId)
    {
        if (!await IsOwnerAsync(ownerId, projectId)) return ApiResponse<List<ProjectMemberUserDto>>.Error(404, "Project not found.");
        var memberIds = _dbContext.ProjectMembers.Where(member => member.ProjectId == projectId).Select(member => member.UserId);
        var users = await _dbContext.Users.AsNoTracking().Where(user => user.IsActive && !memberIds.Contains(user.Id)).ToListAsync();
        return ApiResponse<List<ProjectMemberUserDto>>.Success(users.Select(user => new ProjectMemberUserDto { Id = user.Id, DisplayName = user.DisplayName, Email = user.Email }).ToList());
    }

    public async Task<ApiResponse<ProjectMemberDto>> AddProjectMemberAsync(Guid ownerId, Guid projectId, Guid userId)
    {
        if (!await IsOwnerAsync(ownerId, projectId)) return ApiResponse<ProjectMemberDto>.Error(404, "Project not found.");
        if (!await _dbContext.Users.AnyAsync(user => user.Id == userId && user.IsActive)) return ApiResponse<ProjectMemberDto>.Error(404, "User not found.");
        if (await _dbContext.ProjectMembers.AnyAsync(member => member.ProjectId == projectId && member.UserId == userId)) return ApiResponse<ProjectMemberDto>.Error(409, "User is already a project member.");
        var member = new ProjectMember { ProjectId = projectId, UserId = userId, Role = ProjectMemberRole.Member };
        _dbContext.ProjectMembers.Add(member); await _dbContext.SaveChangesAsync();
        var saved = await _dbContext.ProjectMembers.Include(item => item.User).FirstAsync(item => item.Id == member.Id);
        return ApiResponse<ProjectMemberDto>.Success(MapMember(saved), "Project member added successfully.", 201);
    }

    public async Task<ApiResponse<ProjectMemberDto>> UpdateProjectMemberRoleAsync(Guid ownerId, Guid projectId, Guid userId, ProjectMemberRole role)
    {
        if (!await IsOwnerAsync(ownerId, projectId)) return ApiResponse<ProjectMemberDto>.Error(404, "Project not found.");
        var member = await _dbContext.ProjectMembers.Include(item => item.User).FirstOrDefaultAsync(item => item.ProjectId == projectId && item.UserId == userId);
        if (member is null) return ApiResponse<ProjectMemberDto>.Error(404, "Project member not found.");
        if (!Enum.IsDefined(role) || role == ProjectMemberRole.Owner) return ApiResponse<ProjectMemberDto>.Error(400, "Invalid project member role.");
        member.Role = role; await _dbContext.SaveChangesAsync();
        return ApiResponse<ProjectMemberDto>.Success(MapMember(member), "Project member role updated successfully.");
    }

    public async Task<ApiResponse<bool>> RemoveProjectMemberAsync(Guid ownerId, Guid projectId, Guid userId)
    {
        if (!await IsOwnerAsync(ownerId, projectId)) return ApiResponse<bool>.Error(404, "Project not found.");
        var member = await _dbContext.ProjectMembers.FirstOrDefaultAsync(item => item.ProjectId == projectId && item.UserId == userId && item.Role != ProjectMemberRole.Owner);
        if (member is null) return ApiResponse<bool>.Error(404, "Project member not found.");

        var assignedTasks = await _dbContext.ProjectTasks
            .Where(task => task.ProjectId == projectId && task.AssignedUserId == userId)
            .ToListAsync();
        foreach (var task in assignedTasks)
        {
            task.AssignedUserId = null;
            task.UpdatedAt = DateTime.UtcNow;
        }

        _dbContext.ProjectMembers.Remove(member); await _dbContext.SaveChangesAsync();
        return ApiResponse<bool>.Success(true, "Project member removed successfully.");
    }

    private Task<Project?> GetOwnedProjectAsync(Guid ownerId, Guid projectId) => _dbContext.Projects.FirstOrDefaultAsync(project => project.Id == projectId && project.OwnerId == ownerId && !project.IsArchived);
    private Task<bool> CanAccessProjectAsync(Guid userId, Guid projectId) => _dbContext.ProjectMembers.AnyAsync(member => member.ProjectId == projectId && member.UserId == userId);
    private Task<bool> IsOwnerAsync(Guid userId, Guid projectId) => _dbContext.Projects.AnyAsync(project => project.Id == projectId && project.OwnerId == userId && !project.IsArchived);
    private static ProjectDto Map(Project project, Guid userId) => new() { Id = project.Id, Name = project.Name, Description = project.Description, OwnerId = project.OwnerId, CreatedAt = project.CreatedAt, UpdatedAt = project.UpdatedAt, IsArchived = project.IsArchived, CurrentUserRole = project.Members.FirstOrDefault(member => member.UserId == userId)?.Role ?? ProjectMemberRole.Viewer };
    private static ProjectMemberDto MapMember(ProjectMember member) => new() { UserId = member.UserId, DisplayName = member.User.DisplayName, Email = member.User.Email, Role = member.Role, AddedAt = member.AddedAt };
}
