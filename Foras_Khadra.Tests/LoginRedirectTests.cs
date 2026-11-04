using Foras_Khadra.Helpers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Foras_Khadra.Tests
{
    public class LoginRedirectTests
    {
        private static IAuthorizeData[] Roles(params string?[] roles) =>
            roles.Select(r => (IAuthorizeData)new AuthorizeAttribute { Roles = r }).ToArray();

        [Fact]
        public void PlainAuthorize_GoesToIndividualLogin_WithReturnUrl()
        {
            var url = LoginRedirect.BuildLoginUrl("/User/Dashboard", "?tab=saved", Roles((string?)null));
            Assert.Equal("/Account/Login?returnUrl=%2FUser%2FDashboard%3Ftab%3Dsaved", url);
        }

        [Fact]
        public void AdminOnly_GoesToIndividualLogin()
        {
            var url = LoginRedirect.BuildLoginUrl("/Admin/Review", null, Roles("Admin"));
            Assert.StartsWith("/Account/Login?", url);
        }

        [Theory]
        [InlineData("Organization")]
        [InlineData("Admin,Organization")]
        [InlineData("Admin, Organization")]
        public void OrganizationRoles_GoToOrganizationLogin(string roles)
        {
            var url = LoginRedirect.BuildLoginUrl("/Opportunity/Create", null, Roles(roles));
            Assert.Equal("/Organization/Login?returnUrl=%2FOpportunity%2FCreate", url);
        }

        [Fact]
        public void KeepsLanguagePrefix()
        {
            var url = LoginRedirect.BuildLoginUrl("/en/Opportunity/Saved", null, Roles((string?)null));
            Assert.Equal("/en/Account/Login?returnUrl=%2Fen%2FOpportunity%2FSaved", url);
        }

        [Fact]
        public void NoAuthorizeMetadata_DefaultsToIndividual()
        {
            var url = LoginRedirect.BuildLoginUrl("/fr/Tracker/Prepare", null, Array.Empty<IAuthorizeData>());
            Assert.Equal("/fr/Account/Login?returnUrl=%2Ffr%2FTracker%2FPrepare", url);
        }
    }
}
