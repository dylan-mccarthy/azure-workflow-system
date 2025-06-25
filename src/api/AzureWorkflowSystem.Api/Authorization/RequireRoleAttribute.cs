using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using AzureWorkflowSystem.Api.Models;

namespace AzureWorkflowSystem.Api.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireRoleAttribute : Attribute, IAuthorizationFilter
{
    private readonly UserRole[] _allowedRoles;

    public RequireRoleAttribute(params UserRole[] allowedRoles)
    {
        _allowedRoles = allowedRoles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // For development/testing, we'll simulate the current user role
        // In a real implementation, this would check the JWT token claims

        // For now, assume the user has Manager role (this would come from JWT in production)
        var currentUserRole = UserRole.Manager;

        if (!_allowedRoles.Contains(currentUserRole))
        {
            context.Result = new ForbidResult();
        }
    }
}