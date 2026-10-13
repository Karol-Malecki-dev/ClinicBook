using Application.DTOs.Project;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Responses;

namespace Infrastructure.Services;

public sealed class DatabaseProjectTaskService : IProjectTaskService
{
    private readonly ApplicationDbContext _dbContext;

    public DatabaseProjectTaskService(ApplicationDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<PagedResult<ProjectTaskDto>>> GetProjectTasksAsync(Guid userId, Guid projectId, ProjectTaskQueryDto query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!await HasAccessAsync(userId, projectId)) return ApiResponse<PagedResult<ProjectTaskDto>>.Error(404, "Project not found.");
        var page = Math.Max(query.PageNumber, 1); var size = Math.Clamp(query.PageSize, 1, 100);
        var tasks = _dbContext.ProjectTasks.AsNoTracking().Where(task => task.ProjectId == projectId);
        if (!string.IsNullOrWhiteSpace(query.Search)) tasks = tasks.Where(task => task.Title.Contains(query.Search));
        var total = await tasks.CountAsync();
        var items = await tasks.OrderByDescending(task => task.CreatedAt).Skip((page - 1) * size).Take(size).ToListAsync();
        return ApiResponse<PagedResult<ProjectTaskDto>>.Success(new PagedResult<ProjectTaskDto> { Items = items.Select(Map).ToList(), PageNumber = page, PageSize = size, TotalCount = total });
    }

    public async Task<ApiResponse<ProjectTaskDto>> GetProjectTaskAsync(Guid ownerId, Guid projectId, Guid taskId)
    {
        if (!await HasAccessAsync(ownerId, projectId)) return ApiResponse<ProjectTaskDto>.Error(404, "Project not found.");
        var task = await _dbContext.ProjectTasks.AsNoTracking().FirstOrDefaultAsync(item => item.Id == taskId && item.ProjectId == projectId);
        return task is null ? ApiResponse<ProjectTaskDto>.Error(404, "Project task not found.") : ApiResponse<ProjectTaskDto>.Success(Map(task));
    }

    public async Task<ApiResponse<ProjectTaskDto>> CreateProjectTaskAsync(Guid ownerId, Guid projectId, CreateProjectTaskDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (!await HasAccessAsync(ownerId, projectId)) return ApiResponse<ProjectTaskDto>.Error(404, "Project not found.");
        if (string.IsNullOrWhiteSpace(dto.Title)) return ApiResponse<ProjectTaskDto>.Error(400, "Task title cannot be empty.");
        if (dto.AssignedUserId is not null && !await IsProjectMemberAsync(projectId, dto.AssignedUserId.Value)) return ApiResponse<ProjectTaskDto>.Error(400, "Assigned user is not a project member.");
        var task = new ProjectTask { ProjectId = projectId, Title = dto.Title.Trim(), Description = dto.Description?.Trim(), Priority = dto.Priority, DueDate = dto.DueDate, AssignedUserId = dto.AssignedUserId, CreatedByUserId = ownerId };
        _dbContext.ProjectTasks.Add(task); await _dbContext.SaveChangesAsync();
        return ApiResponse<ProjectTaskDto>.Success(Map(task), "Project task created successfully.", 201);
    }

    public async Task<ApiResponse<ProjectTaskDto>> UpdateProjectTaskAsync(Guid ownerId, Guid projectId, Guid taskId, UpdateProjectTaskDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var task = await GetTaskForProjectAsync(ownerId, projectId, taskId);
        if (task is null) return ApiResponse<ProjectTaskDto>.Error(404, "Project task not found.");
        if (string.IsNullOrWhiteSpace(dto.Title)) return ApiResponse<ProjectTaskDto>.Error(400, "Task title cannot be empty.");
        if (dto.AssignedUserId is not null && !await IsProjectMemberAsync(projectId, dto.AssignedUserId.Value)) return ApiResponse<ProjectTaskDto>.Error(400, "Assigned user is not a project member.");
        task.Title = dto.Title.Trim(); task.Description = dto.Description?.Trim(); task.Priority = dto.Priority; task.DueDate = dto.DueDate; task.AssignedUserId = dto.AssignedUserId; task.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(); return ApiResponse<ProjectTaskDto>.Success(Map(task), "Project task updated successfully.");
    }

    public async Task<ApiResponse<ProjectTaskDto>> UpdateProjectTaskStatusAsync(Guid ownerId, Guid projectId, Guid taskId, UpdateProjectTaskStatusDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var task = await GetTaskForProjectAsync(ownerId, projectId, taskId);
        if (task is null) return ApiResponse<ProjectTaskDto>.Error(404, "Project task not found.");
        if (!Enum.IsDefined(dto.Status)) return ApiResponse<ProjectTaskDto>.Error(400, "Invalid task status.");
        task.Status = dto.Status; task.UpdatedAt = DateTime.UtcNow; await _dbContext.SaveChangesAsync();
        return ApiResponse<ProjectTaskDto>.Success(Map(task), "Project task status updated successfully.");
    }

    public async Task<ApiResponse<bool>> DeleteProjectTaskAsync(Guid ownerId, Guid projectId, Guid taskId)
    {
        var task = await GetTaskForProjectAsync(ownerId, projectId, taskId);
        if (task is null) return ApiResponse<bool>.Error(404, "Project task not found.");
        _dbContext.ProjectTasks.Remove(task); await _dbContext.SaveChangesAsync(); return ApiResponse<bool>.Success(true, "Project task deleted successfully.");
    }

    private Task<bool> HasAccessAsync(Guid userId, Guid projectId) => _dbContext.ProjectMembers.AnyAsync(member => member.ProjectId == projectId && member.UserId == userId && !member.Project.IsArchived);
    private Task<bool> IsProjectMemberAsync(Guid projectId, Guid userId) => _dbContext.ProjectMembers.AnyAsync(member => member.ProjectId == projectId && member.UserId == userId);
    private Task<ProjectTask?> GetTaskForProjectAsync(Guid userId, Guid projectId, Guid taskId) => _dbContext.ProjectTasks.FirstOrDefaultAsync(task => task.Id == taskId && task.ProjectId == projectId && task.Project.Members.Any(member => member.UserId == userId));
    private static ProjectTaskDto Map(ProjectTask task) => new() { Id = task.Id, ProjectId = task.ProjectId, Title = task.Title, Description = task.Description, Status = task.Status, Priority = task.Priority, DueDate = task.DueDate, AssignedUserId = task.AssignedUserId, CreatedByUserId = task.CreatedByUserId, CreatedAt = task.CreatedAt, UpdatedAt = task.UpdatedAt };
}
