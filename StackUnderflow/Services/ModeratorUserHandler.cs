using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using StackUnderflow.Models;

namespace StackUnderflow.Services;

public class ModeratorUserHandler(UserManager<User> userManager) : AuthorizationHandler<ModeratorUserRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ModeratorUserRequirement requirement)
    {
        var user = await userManager.GetUserAsync(context.User);

        if (user?.IsModerator == true)
        {
            context.Succeed(requirement);
        }
    }
}
