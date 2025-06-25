using AzureWorkflowSystem.Api.Data;
using AzureWorkflowSystem.Api.DTOs;
using AzureWorkflowSystem.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AzureWorkflowSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SlaConfigurationsController : ControllerBase
{
    private readonly WorkflowDbContext _context;
    private readonly ILogger<SlaConfigurationsController> _logger;

    public SlaConfigurationsController(WorkflowDbContext context, ILogger<SlaConfigurationsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all SLA configurations
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SlaConfigurationDto>>> GetSlaConfigurations()
    {
        var configurations = await _context.SlaConfigurations
            .OrderBy(s => s.Priority)
            .ThenBy(s => s.Category)
            .ToListAsync();

        var configurationDtos = configurations.Select(MapToDto).ToList();
        return Ok(configurationDtos);
    }

    /// <summary>
    /// Get a specific SLA configuration by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<SlaConfigurationDto>> GetSlaConfiguration(int id)
    {
        var configuration = await _context.SlaConfigurations.FindAsync(id);

        if (configuration == null)
        {
            return NotFound();
        }

        return Ok(MapToDto(configuration));
    }

    /// <summary>
    /// Create a new SLA configuration
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SlaConfigurationDto>> CreateSlaConfiguration(CreateSlaConfigurationDto createDto)
    {
        // Check if configuration already exists for this priority/category combination
        var existingConfig = await _context.SlaConfigurations
            .FirstOrDefaultAsync(s => s.Priority == createDto.Priority && s.Category == createDto.Category);

        if (existingConfig != null)
        {
            return Conflict(new { message = "SLA configuration already exists for this priority and category combination." });
        }

        var configuration = new SlaConfiguration
        {
            Priority = createDto.Priority,
            Category = createDto.Category,
            ResponseTimeMinutes = createDto.ResponseTimeMinutes,
            ResolutionTimeMinutes = createDto.ResolutionTimeMinutes,
            IsActive = createDto.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.SlaConfigurations.Add(configuration);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created SLA configuration for {Priority} {Category}", 
            createDto.Priority, createDto.Category);

        return CreatedAtAction(nameof(GetSlaConfiguration), new { id = configuration.Id }, MapToDto(configuration));
    }

    /// <summary>
    /// Update an existing SLA configuration
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateSlaConfiguration(int id, UpdateSlaConfigurationDto updateDto)
    {
        var configuration = await _context.SlaConfigurations.FindAsync(id);
        if (configuration == null)
        {
            return NotFound();
        }

        if (updateDto.ResponseTimeMinutes.HasValue)
            configuration.ResponseTimeMinutes = updateDto.ResponseTimeMinutes.Value;

        if (updateDto.ResolutionTimeMinutes.HasValue)
            configuration.ResolutionTimeMinutes = updateDto.ResolutionTimeMinutes.Value;

        if (updateDto.IsActive.HasValue)
            configuration.IsActive = updateDto.IsActive.Value;

        configuration.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated SLA configuration {Id} for {Priority} {Category}", 
                id, configuration.Priority, configuration.Category);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await SlaConfigurationExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    /// <summary>
    /// Delete an SLA configuration
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteSlaConfiguration(int id)
    {
        var configuration = await _context.SlaConfigurations.FindAsync(id);
        if (configuration == null)
        {
            return NotFound();
        }

        _context.SlaConfigurations.Remove(configuration);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted SLA configuration {Id} for {Priority} {Category}", 
            id, configuration.Priority, configuration.Category);

        return NoContent();
    }

    private static SlaConfigurationDto MapToDto(SlaConfiguration configuration)
    {
        return new SlaConfigurationDto
        {
            Id = configuration.Id,
            Priority = configuration.Priority,
            Category = configuration.Category,
            ResponseTimeMinutes = configuration.ResponseTimeMinutes,
            ResolutionTimeMinutes = configuration.ResolutionTimeMinutes,
            IsActive = configuration.IsActive,
            CreatedAt = configuration.CreatedAt,
            UpdatedAt = configuration.UpdatedAt
        };
    }

    private async Task<bool> SlaConfigurationExists(int id)
    {
        return await _context.SlaConfigurations.AnyAsync(e => e.Id == id);
    }
}