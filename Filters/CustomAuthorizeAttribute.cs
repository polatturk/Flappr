using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Flappr.Filters
{
    public class CustomAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var httpContext = context.HttpContext;

            var userId = httpContext.Session.GetInt32("userId");
            var mail = httpContext.Session.GetString("Mail");

            if (userId == null || string.IsNullOrEmpty(mail))
            {
                context.Result = new RedirectToActionResult("ErrorMessages", "Interaction", null);
            }
        }
    }

}
