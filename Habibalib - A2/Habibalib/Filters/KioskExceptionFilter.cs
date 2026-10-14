using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Habibalib.Controllers;

namespace Habibalib.Filters
{
    // Applied to KioskController only: any unhandled exception shows the kiosk-styled
    // error page instead of the site-wide error page or a stack trace.
    public class KioskExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<KioskExceptionFilter> log;

        public KioskExceptionFilter(ILogger<KioskExceptionFilter> logger) => log = logger;

        public void OnException(ExceptionContext context)
        {
            log.LogError(context.Exception, "Kiosk error on {Path}", context.HttpContext.Request.Path);

            context.Result = new ViewResult
            {
                ViewName = "~/Views/Kiosk/KioskError.cshtml",
                StatusCode = 500,
                ViewData = new ViewDataDictionary<KioskErrorModel>(new EmptyModelMetadataProvider(), context.ModelState)
                {
                    Model = KioskErrorModel.For(500)
                }
            };
            context.ExceptionHandled = true;
        }
    }
}
