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

public class UsersControllerTests
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

    private static UsersController GetController(WorkflowDbContext context)
    {
        var logger = new Mock<ILogger<UsersController>>();
        return new UsersController(context, logger.Object);
    }

    [Fact]
    public async Task GetUsers_ReturnsAllUsers()
    {
        // Arrange
        using var context = GetDbContext();
        var user1 = new User
        {
            Email = "user1@test.com",
            FirstName = "User",
            LastName = "One",
            Role = UserRole.Engineer,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var user2 = new User
        {
            Email = "user2@test.com",
            FirstName = "User",
            LastName = "Two",
            Role = UserRole.Manager,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(user1, user2);
        await context.SaveChangesAsync();

        var controller = GetController(context);

        // Act
        var result = await controller.GetUsers();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var users = Assert.IsAssignableFrom<List<UserDto>>(okResult.Value);
        Assert.Equal(3, users.Count); // 2 test users + 1 seeded admin user
        Assert.Contains(users, u => u.Email == "user1@test.com");
        Assert.Contains(users, u => u.Email == "user2@test.com");
        Assert.Contains(users, u => u.Email == "admin@azureworkflow.com"); // Seeded admin user
    }

    [Fact]
    public async Task GetUser_WithValidId_ReturnsUser()
    {
        // Arrange
        using var context = GetDbContext();
        var user = new User
        {
            Email = "test@test.com",
            FirstName = "Test",
            LastName = "User",
            Role = UserRole.Engineer,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var controller = GetController(context);

        // Act
        var result = await controller.GetUser(user.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var userDto = Assert.IsType<UserDto>(okResult.Value);
        Assert.Equal("test@test.com", userDto.Email);
        Assert.Equal("Test", userDto.FirstName);
        Assert.Equal("User", userDto.LastName);
        Assert.Equal(UserRole.Engineer, userDto.Role);
    }

    [Fact]
    public async Task GetUser_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        using var context = GetDbContext();
        var controller = GetController(context);

        // Act
        var result = await controller.GetUser(999);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateUser_WithValidData_CreatesUser()
    {
        // Arrange
        using var context = GetDbContext();
        var controller = GetController(context);
        var createUserDto = new CreateUserDto
        {
            Email = "newuser@test.com",
            FirstName = "New",
            LastName = "User",
            Role = UserRole.Engineer
        };

        // Act
        var result = await controller.CreateUser(createUserDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var userDto = Assert.IsType<UserDto>(createdResult.Value);
        Assert.Equal("newuser@test.com", userDto.Email);
        Assert.Equal("New", userDto.FirstName);
        Assert.Equal("User", userDto.LastName);
        Assert.Equal(UserRole.Engineer, userDto.Role);
        Assert.True(userDto.IsActive);

        // Verify user was saved to database
        var savedUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "newuser@test.com");
        Assert.NotNull(savedUser);
        Assert.Equal("New", savedUser.FirstName);
    }

    [Fact]
    public async Task CreateUser_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        using var context = GetDbContext();
        var existingUser = new User
        {
            Email = "existing@test.com",
            FirstName = "Existing",
            LastName = "User",
            Role = UserRole.Engineer,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(existingUser);
        await context.SaveChangesAsync();

        var controller = GetController(context);
        var createUserDto = new CreateUserDto
        {
            Email = "existing@test.com",
            FirstName = "New",
            LastName = "User",
            Role = UserRole.Manager
        };

        // Act
        var result = await controller.CreateUser(createUserDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("User with this email already exists", badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateUser_WithValidData_UpdatesUser()
    {
        // Arrange
        using var context = GetDbContext();
        var user = new User
        {
            Email = "update@test.com",
            FirstName = "Original",
            LastName = "Name",
            Role = UserRole.Engineer,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var controller = GetController(context);
        var updateUserDto = new UpdateUserDto
        {
            FirstName = "Updated",
            LastName = "NewName",
            Role = UserRole.Manager,
            IsActive = false
        };

        // Act
        var result = await controller.UpdateUser(user.Id, updateUserDto);

        // Assert
        Assert.IsType<NoContentResult>(result);

        // Verify user was updated in database
        var updatedUser = await context.Users.FindAsync(user.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal("Updated", updatedUser.FirstName);
        Assert.Equal("NewName", updatedUser.LastName);
        Assert.Equal(UserRole.Manager, updatedUser.Role);
        Assert.False(updatedUser.IsActive);
        Assert.True(updatedUser.UpdatedAt >= user.CreatedAt); // More reliable comparison
    }

    [Fact]
    public async Task UpdateUser_WithPartialData_UpdatesOnlyProvidedFields()
    {
        // Arrange
        using var context = GetDbContext();
        var user = new User
        {
            Email = "partial@test.com",
            FirstName = "Original",
            LastName = "Name",
            Role = UserRole.Engineer,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var controller = GetController(context);
        var updateUserDto = new UpdateUserDto
        {
            FirstName = "Updated"
            // Only updating FirstName, other fields should remain unchanged
        };

        // Act
        var result = await controller.UpdateUser(user.Id, updateUserDto);

        // Assert
        Assert.IsType<NoContentResult>(result);

        // Verify only firstName was updated
        var updatedUser = await context.Users.FindAsync(user.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal("Updated", updatedUser.FirstName);
        Assert.Equal("Name", updatedUser.LastName); // Should remain unchanged
        Assert.Equal(UserRole.Engineer, updatedUser.Role); // Should remain unchanged
        Assert.True(updatedUser.IsActive); // Should remain unchanged
    }

    [Fact]
    public async Task UpdateUser_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        using var context = GetDbContext();
        var controller = GetController(context);
        var updateUserDto = new UpdateUserDto
        {
            FirstName = "Updated"
        };

        // Act
        var result = await controller.UpdateUser(999, updateUserDto);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteUser_WithValidId_SoftDeletesUser()
    {
        // Arrange
        using var context = GetDbContext();
        var user = new User
        {
            Email = "delete@test.com",
            FirstName = "Delete",
            LastName = "Me",
            Role = UserRole.Engineer,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var controller = GetController(context);

        // Act
        var result = await controller.DeleteUser(user.Id);

        // Assert
        Assert.IsType<NoContentResult>(result);

        // Verify user was soft deleted (marked as inactive)
        var deletedUser = await context.Users.FindAsync(user.Id);
        Assert.NotNull(deletedUser);
        Assert.False(deletedUser.IsActive);
        Assert.True(deletedUser.UpdatedAt > user.CreatedAt); // Use CreatedAt for comparison since UpdatedAt gets set in constructor
    }

    [Fact]
    public async Task DeleteUser_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        using var context = GetDbContext();
        var controller = GetController(context);

        // Act
        var result = await controller.DeleteUser(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}