using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Blogifier
{
    public class RegistrationRequirementHandler : AuthorizationHandler<RegistrationRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RegistrationRequirement requirement)
        {
            if(!context.User.Identity.IsAuthenticated && !requirement.UnauthenticatedRegisterEnabled)
            {
                context.Fail();
            }
            else
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
