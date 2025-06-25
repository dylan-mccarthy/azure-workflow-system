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

    [Fact]
    public async Task GetSlaConfigurations_WithMultipleConfigurationsAndFiltering_ReturnsFilteredResults()
    {
        // Arrange
        using var context = GetDbContext();
        var configs = new List<SlaConfiguration>
        {
            new SlaConfiguration
            {
                Priority = TicketPriority.Emergency,
                Category = TicketCategory.Incident,
                ResponseTimeMinutes = 5,
                ResolutionTimeMinutes = 30,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new SlaConfiguration
            {
                Priority = TicketPriority.Critical,
                Category = TicketCategory.Alert,
                ResponseTimeMinutes = 10,
                ResolutionTimeMinutes = 60,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new SlaConfiguration
            {
                Priority = TicketPriority.High,
                Category = TicketCategory.Incident,
                ResponseTimeMinutes = 15,
                ResolutionTimeMinutes = 120,
                IsActive = false, // Inactive
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };
        context.SlaConfigurations.AddRange(configs);
        await context.SaveChangesAsync();

        var controller = GetController(context);

        // Act
        var result = await controller.GetSlaConfigurations();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var configurations = Assert.IsAssignableFrom<IEnumerable<SlaConfigurationDto>>(okResult.Value);
        var configList = configurations.ToList();
        
        Assert.Equal(3, configList.Count); // Should return all configurations (active and inactive)
        Assert.Contains(configList, c => c.Priority == TicketPriority.Emergency && c.IsActive);
        Assert.Contains(configList, c => c.Priority == TicketPriority.Critical && c.IsActive);
        Assert.Contains(configList, c => c.Priority == TicketPriority.High && !c.IsActive);
    }

    [Fact]
    public async Task CreateSlaConfiguration_WithMinimumValidData_CreatesSuccessfully()
    {
        // Arrange
        using var context = GetDbContext();
        var controller = GetController(context);
        var createDto = new CreateSlaConfigurationDto
        {
            Priority = TicketPriority.Emergency,  // Use a unique combination not in seed data
            Category = TicketCategory.Incident,
            ResponseTimeMinutes = 5,
            ResolutionTimeMinutes = 30,
            IsActive = true
        };

        // Act
        var result = await controller.CreateSlaConfiguration(createDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var configDto = Assert.IsType<SlaConfigurationDto>(createdResult.Value);
        
        Assert.Equal(createDto.Priority, configDto.Priority);
        Assert.Equal(createDto.Category, configDto.Category);
        Assert.Equal(createDto.ResponseTimeMinutes, configDto.ResponseTimeMinutes);
        Assert.Equal(createDto.ResolutionTimeMinutes, configDto.ResolutionTimeMinutes);
        Assert.Equal(createDto.IsActive, configDto.IsActive);
        Assert.True(configDto.Id > 0);
        Assert.True(configDto.CreatedAt <= DateTime.UtcNow);
        Assert.True(configDto.UpdatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task CreateSlaConfiguration_WithInvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        using var context = GetDbContext();
        var controller = GetController(context);
        var createDto = new CreateSlaConfigurationDto
        {
            Priority = TicketPriority.Emergency,  // Use unique combination
            Category = TicketCategory.Access,
            ResponseTimeMinutes = 10,
            ResolutionTimeMinutes = 60,
            IsActive = true
        };

        // Simulate model validation failure
        controller.ModelState.AddModelError("ResponseTimeMinutes", "ResponseTimeMinutes must be greater than 0");

        // Act
        var result = await controller.CreateSlaConfiguration(createDto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateSlaConfiguration_WithPartialUpdate_UpdatesOnlySpecifiedFields()
    {
        // Arrange
        using var context = GetDbContext();
        var originalConfig = new SlaConfiguration
        {
            Priority = TicketPriority.Medium,
            Category = TicketCategory.Alert,
            ResponseTimeMinutes = 30,
            ResolutionTimeMinutes = 180,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        context.SlaConfigurations.Add(originalConfig);
        await context.SaveChangesAsync();

        var controller = GetController(context);
        var updateDto = new UpdateSlaConfigurationDto
        {
            ResponseTimeMinutes = 45, // Only update this field
            // ResolutionTimeMinutes and IsActive not specified - should remain unchanged
        };

        // Act
        var result = await controller.UpdateSlaConfiguration(originalConfig.Id, updateDto);

        // Assert
        Assert.IsType<NoContentResult>(result);

        // Verify only specified fields were updated
        var updatedConfig = await context.SlaConfigurations.FindAsync(originalConfig.Id);
        Assert.NotNull(updatedConfig);
        Assert.Equal(45, updatedConfig.ResponseTimeMinutes); // Updated
        Assert.Equal(180, updatedConfig.ResolutionTimeMinutes); // Unchanged
        Assert.True(updatedConfig.IsActive); // Unchanged
        Assert.Equal(originalConfig.Priority, updatedConfig.Priority); // Unchanged
        Assert.Equal(originalConfig.Category, updatedConfig.Category); // Unchanged
        Assert.True(updatedConfig.UpdatedAt > originalConfig.UpdatedAt); // UpdatedAt should be refreshed
    }

    [Fact]
    public async Task UpdateSlaConfiguration_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        using var context = GetDbContext();
        var controller = GetController(context);
        var updateDto = new UpdateSlaConfigurationDto
        {
            ResponseTimeMinutes = 30,
            ResolutionTimeMinutes = 120,
            IsActive = false
        };

        // Act
        var result = await controller.UpdateSlaConfiguration(9999, updateDto);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteSlaConfiguration_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        using var context = GetDbContext();
        var controller = GetController(context);

        // Act
        var result = await controller.DeleteSlaConfiguration(9999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task CreateSlaConfiguration_WithCompleteData_PreservesAllFields()
    {
        // Arrange
        using var context = GetDbContext();
        var controller = GetController(context);
        var createDto = new CreateSlaConfigurationDto
        {
            Priority = TicketPriority.Low,  // Use unique combination
            Category = TicketCategory.Access,
            ResponseTimeMinutes = 60,
            ResolutionTimeMinutes = 240,
            IsActive = false // Test with inactive configuration
        };

        // Act
        var result = await controller.CreateSlaConfiguration(createDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var configDto = Assert.IsType<SlaConfigurationDto>(createdResult.Value);
        
        Assert.Equal(TicketPriority.Low, configDto.Priority);
        Assert.Equal(TicketCategory.Access, configDto.Category);
        Assert.Equal(60, configDto.ResponseTimeMinutes);
        Assert.Equal(240, configDto.ResolutionTimeMinutes);
        Assert.False(configDto.IsActive); // Should preserve inactive state
        
        // Verify in database
        var savedConfig = await context.SlaConfigurations.FindAsync(configDto.Id);
        Assert.NotNull(savedConfig);
        Assert.Equal(createDto.Priority, savedConfig.Priority);
        Assert.Equal(createDto.Category, savedConfig.Category);
        Assert.Equal(createDto.ResponseTimeMinutes, savedConfig.ResponseTimeMinutes);
        Assert.Equal(createDto.ResolutionTimeMinutes, savedConfig.ResolutionTimeMinutes);
        Assert.Equal(createDto.IsActive, savedConfig.IsActive);
    }

    [Fact]
    public async Task GetSlaConfiguration_ReturnsCorrectRouteForCreatedAt()
    {
        // Arrange
        using var context = GetDbContext();
        var config = new SlaConfiguration
        {
            Priority = TicketPriority.Low,  // Use unique combination  
            Category = TicketCategory.NewResource,
            ResponseTimeMinutes = 30,
            ResolutionTimeMinutes = 120,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.SlaConfigurations.Add(config);
        await context.SaveChangesAsync();

        var controller = GetController(context);
        var createDto = new CreateSlaConfigurationDto
        {
            Priority = TicketPriority.High,  // Use unique combination
            Category = TicketCategory.NewResource,
            ResponseTimeMinutes = 15,
            ResolutionTimeMinutes = 90,
            IsActive = true
        };

        // Act
        var result = await controller.CreateSlaConfiguration(createDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal("GetSlaConfiguration", createdResult.ActionName);
        Assert.NotNull(createdResult.RouteValues);
        Assert.True(createdResult.RouteValues.ContainsKey("id"));
    }
}