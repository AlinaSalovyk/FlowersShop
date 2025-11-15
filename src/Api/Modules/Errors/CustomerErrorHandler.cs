using Application.Customers.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class CustomerErrorHandler
{
    public static ObjectResult ToObjectResult(this CustomerException error)
    {
        return new ObjectResult(error.Message)
        {
            StatusCode = error switch
            {
                CustomerAlreadyExistException => StatusCodes.Status409Conflict,
                CustomerEmailAlreadyExistsException => StatusCodes.Status409Conflict,
                CustomerNotFoundException => StatusCodes.Status404NotFound,
                UnhandledCustomerException => StatusCodes.Status500InternalServerError,
                _ => StatusCodes.Status500InternalServerError
            }
        };
    }
}