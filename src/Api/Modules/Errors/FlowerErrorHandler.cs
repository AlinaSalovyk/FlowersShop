using Application.Flowers.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class FlowerErrorHandler
{
    public static ObjectResult ToObjectResult(this FlowerException error)
    {
        return new ObjectResult(error.Message)
        {
            StatusCode = error switch
            {
                FlowerAlreadyExistException => StatusCodes.Status409Conflict,
                FlowerNotFoundException => StatusCodes.Status404NotFound,
                FlowerCategoriesNotFoundException => StatusCodes.Status404NotFound,
                UnhandledFlowerException => StatusCodes.Status500InternalServerError,
                _ => StatusCodes.Status500InternalServerError
            }
        };
    }
}