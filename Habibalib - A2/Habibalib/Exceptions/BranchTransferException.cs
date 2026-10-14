using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Habibalib.ViewModels;

namespace Habibalib.Exceptions
{
    public class BranchTransferExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<BranchTransferExceptionFilter> logger;

        public BranchTransferExceptionFilter(ILogger<BranchTransferExceptionFilter> logger)
        {
            this.logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            logger.LogError(context.Exception, "Branch transfer error on {Path}", context.HttpContext.Request.Path);

            context.Result = new ViewResult
            {
                ViewName = "~/Views/BranchTransfer/Error.cshtml",
                StatusCode = StatusCodes.Status500InternalServerError,
                ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<BranchTransferErrorViewModel>(
                    new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(),
                    context.ModelState)
                {
                    Model = new BranchTransferErrorViewModel
                    {
                        StatusCode = StatusCodes.Status500InternalServerError,
                        Message = "The transfer could not be completed due to an unexpected error. Please try again or contact an administrator if the problem continues."
                    }
                }
            };
            context.ExceptionHandled = true;
        }
    }
}
