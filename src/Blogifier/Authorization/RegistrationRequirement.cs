using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;

namespace Blogifier
{
    public class RegistrationRequirement : IAuthorizationRequirement
    {
        private readonly IConfiguration _configuration;

        public RegistrationRequirement(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public bool UnauthenticatedRegisterEnabled => _configuration.GetValue<bool>("Blogifier:UnauthenticatedRegisterEnabled");
    }
}
