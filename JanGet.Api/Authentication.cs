using System;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JanGet.Api
{
    public class JanGetAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (Context.Request.Headers["JanGet-Token"] == Program.Config.MasterToken)
                return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(
                    new ClaimsIdentity(new Claim[]
                    {
                        new("JanGet", "admin")
                    })), "JanGet")));
            return Task.FromResult(AuthenticateResult.Fail("Incorrect token."));
        }

        public JanGetAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
            UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
        {
        }
    }

    public class JanGetAuthorizationHandler : AuthorizationHandler<JanGetAuthorizationRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
            JanGetAuthorizationRequirement requirement)
        {
            if (context.User.HasClaim("JanGet", "admin"))
                context.Succeed(requirement);
            else
                context.Fail();
            return Task.CompletedTask;
        }
    }

    public class JanGetAuthorizationRequirement : IAuthorizationRequirement
    {
    }
}