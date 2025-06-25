using AzureWorkflowSystem.Api.Controllers;
using AzureWorkflowSystem.Api.Data;
using AzureWorkflowSystem.Api.DTOs;
using AzureWorkflowSystem.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AzureWorkflowSystem.Api.Tests.Controllers;

public class SlaConfigurationsControllerTests
{
    private static WorkflowDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var context = new WorkflowDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static SlaConfigurationsController GetController(WorkflowDbContext context)
    {
        var logger = new Mock<ILogger<SlaConfigurationsController>>();
        return new SlaConfigurationsController(context, logger.Object);
    }

    [Fact]
    public async Task GetSlaConfigurations_ReturnsAllConfigurations()
    {
        // Arrange
        using var context = GetDbContext();
        var config = new SlaConfiguration
        {
            Priority = TicketPriority.Emergency,
            Category = TicketCategory.Incident,
            ResponseTimeMinutes = 15,
            ResolutionTimeMinutes = 60,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.SlaConfigurations.Add(config);
        await context.SaveChangesAsync();

        var controller = GetController(context);

        // Act
        var result = await controller.GetSlaConfigurations();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var configurations = Assert.IsAssignableFrom<IEnumerable<SlaConfigurationDto>>(okResult.Value);
        Assert.NotEmpty(configurations);
    }

    [Fact]
    public async Task GetSlaConfiguration_WithValidId_ReturnsConfiguration()
    {
        // Arrange
        using var context = GetDbContext();
        var config = new SlaConfiguration
        {
            Priority = TicketPriority.Emergency,
            Category = TicketCategory.Incident,
            ResponseTimeMinutes = 15,
            ResolutionTimeMinutes = 60,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.SlaConfigurations.Add(config);
        await context.SaveChangesAsync();

        var controller = GetController(context);

        // Act
        var result = await controller.GetSlaConfiguration(config.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var configDto = Assert.IsType<SlaConfigurationDto>(okResult.Value);
        Assert.Equal(config.Priority, configDto.Priority);
        Assert.Equal(config.Category, configDto.Category);
    }

    [Fact]
    public async Task GetSlaConfiguration_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        using var context = GetDbContext();
        var controller = GetController(context);

        // Act
        var result = await controller.GetSlaConfiguration(999);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateSlaConfiguration_WithValidData_CreatesConfiguration()
    {
        // Arrange
        using var context = GetDbContext();
        var controller = GetController(context);
        var createDto = new CreateSlaConfigurationDto
        {
            Priority = TicketPriority.Low,
            Category = TicketCategory.Alert,
            ResponseTimeMinutes = 30,
            ResolutionTimeMinutes = 120,
            IsActive = true
        };

        // Act
        var result = await controller.CreateSlaConfiguration(createDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var configDto = Assert.IsType<SlaConfigurationDto>(createdResult.Value);
        Assert.Equal(createDto.Priority, configDto.Priority);
        Assert.Equal(createDto.Category, configDto.Category);
    }

    [Fact]
    public async Task CreateSlaConfiguration_WithDuplicatePriorityCategory_ReturnsConflict()
    {
        // Arrange
        using var context = GetDbContext();
        var existingConfig = new SlaConfiguration
        {
            Priority = TicketPriority.Emergency,
            Category = TicketCategory.Incident,
            ResponseTimeMinutes = 15,
            ResolutionTimeMinutes = 60,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.SlaConfigurations.Add(existingConfig);
        await context.SaveChangesAsync();

        var controller = GetController(context);
        var createDto = new CreateSlaConfigurationDto
        {
            Priority = TicketPriority.Emergency,
            Category = TicketCategory.Incident,
            ResponseTimeMinutes = 10,
            ResolutionTimeMinutes = 30,
            IsActive = true
        };

        // Act
        var result = await controller.CreateSlaConfiguration(createDto);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.NotNull(conflictResult.Value);
    }

    [Fact]
    public async Task UpdateSlaConfiguration_WithValidData_UpdatesConfiguration()
    {
        // Arrange
        using var context = GetDbContext();
        var config = new SlaConfiguration
        {
            Priority = TicketPriority.Emergency,
            Category = TicketCategory.Incident,
            ResponseTimeMinutes = 15,
            ResolutionTimeMinutes = 60,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.SlaConfigurations.Add(config);
        await context.SaveChangesAsync();

        var controller = GetController(context);
        var updateDto = new UpdateSlaConfigurationDto
        {
            ResolutionTimeMinutes = 90,
            IsActive = false
        };

        // Act
        var result = await controller.UpdateSlaConfiguration(config.Id, updateDto);

        // Assert
        Assert.IsType<NoContentResult>(result);

        // Verify the update
        var updatedConfig = await context.SlaConfigurations.FindAsync(config.Id);
        Assert.NotNull(updatedConfig);
        Assert.Equal(90, updatedConfig.ResolutionTimeMinutes);
        Assert.False(updatedConfig.IsActive);
    }

    [Fact]
    public async Task DeleteSlaConfiguration_WithValidId_DeletesConfiguration()
    {
        // Arrange
        using var context = GetDbContext();
        var config = new SlaConfiguration
        {
            Priority = TicketPriority.Emergency,
            Category = TicketCategory.Incident,
            ResponseTimeMinutes = 15,
            ResolutionTimeMinutes = 60,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.SlaConfigurations.Add(config);
        await context.SaveChangesAsync();

        var controller = GetController(context);

        // Act
        var result = await controller.DeleteSlaConfiguration(config.Id);

        // Assert
        Assert.IsType<NoContentResult>(result);

        // Verify deletion
        var deletedConfig = await context.SlaConfigurations.FindAsync(config.Id);
        Assert.Null(deletedConfig);
    }
}