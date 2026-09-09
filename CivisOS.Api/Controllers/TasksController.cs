using System.Security.Claims;
using CivisOS.Application.Common.Models;
using CivisOS.Application.Tasks.DTOs;
using CivisOS.Application.Tasks.Interfaces;
using CivisOS.Domain.Constants;
using CivisOS.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivisOS.Api.Controllers;

[ApiController]
[Route("api/v1/tasks")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly IWorkTaskService _taskService;
    private readonly IValidator<CreateWorkTaskRequest> _createValidator;
    private readonly IValidator<UpdateWorkTaskRequest> _updateValidator;
    private readonly IValidator<AssignWorkTaskRequest> _assignValidator;
    private readonly IValidator<UpdateWorkTaskStatusRequest> _statusValidator;
    private readonly IValidator<StartWorkTaskRequest> _startValidator;

    public TasksController(
        IWorkTaskService taskService,
        IValidator<CreateWorkTaskRequest> createValidator,
        IValidator<UpdateWorkTaskRequest> updateValidator,
        IValidator<AssignWorkTaskRequest> assignValidator,
        IValidator<UpdateWorkTaskStatusRequest> statusValidator,
        IValidator<StartWorkTaskRequest> startValidator)
    {
        _taskService = taskService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _assignValidator = assignValidator;
        _statusValidator = statusValidator;
        _startValidator = startValidator;
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<PagedResult<WorkTaskDto>>>> GetTasks(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] WorkTaskStatus? status = null,
        [FromQuery] Guid? assigneeEmployeeId = null,
        [FromQuery] WorkTaskPriority? priority = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _taskService.GetTasksAsync(
            new WorkTaskListQuery(pageNumber, pageSize, search, status, assigneeEmployeeId, priority),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("my")]
    public async Task<ActionResult<ApiResponse<PagedResult<WorkTaskDto>>>> GetMyTasks(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] WorkTaskStatus? status = null,
        [FromQuery] WorkTaskPriority? priority = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<PagedResult<WorkTaskDto>>.Fail("Unauthorized."));
        }

        var result = await _taskService.GetMyTasksAsync(
            userId,
            new WorkTaskListQuery(pageNumber, pageSize, null, status, null, priority),
            cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<WorkTaskDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _taskService.GetByIdAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<WorkTaskDto>>> Create(
        [FromBody] CreateWorkTaskRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<WorkTaskDto>.Fail("Unauthorized."));
        }

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<WorkTaskDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _taskService.CreateAsync(userId, request, cancellationToken);
        return result.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result)
            : BadRequest(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<WorkTaskDto>>> Update(
        Guid id,
        [FromBody] UpdateWorkTaskRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<WorkTaskDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _taskService.UpdateAsync(id, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id:guid}/assign")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<WorkTaskDto>>> Assign(
        Guid id,
        [FromBody] AssignWorkTaskRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<WorkTaskDto>.Fail("Unauthorized."));
        }

        var validation = await _assignValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<WorkTaskDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _taskService.AssignAsync(userId, id, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<WorkTaskDto>>> UpdateStatus(
        Guid id,
        [FromBody] UpdateWorkTaskStatusRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _statusValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<WorkTaskDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _taskService.UpdateStatusAsync(id, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id:guid}/start")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor},{AppRoles.Employee}")]
    public async Task<ActionResult<ApiResponse<WorkTaskDto>>> Start(
        Guid id,
        [FromBody] StartWorkTaskRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<WorkTaskDto>.Fail("Unauthorized."));
        }

        var validation = await _startValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<WorkTaskDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _taskService.StartAsync(userId, id, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<WorkTaskDto>>> Approve(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<WorkTaskDto>.Fail("Unauthorized."));
        }

        var result = await _taskService.ApproveAsync(userId, id, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
