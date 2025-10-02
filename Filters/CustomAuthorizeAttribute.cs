using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Flappr.Filters
{
    public class CustomAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var httpContext = context.HttpContext;

            var userIdString = httpContext.Session.GetString("userId");
            Guid? userId = !string.IsNullOrEmpty(userIdString) ? Guid.Parse(userIdString) : (Guid?)null;
            var mail = httpContext.Session.GetString("Mail");

            if (userId == null || string.IsNullOrEmpty(mail))
            {
                context.Result = new RedirectToActionResult("ErrorMessages", "Interaction", null);
            }
        }
    }

}
