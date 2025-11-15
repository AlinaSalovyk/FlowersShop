using Application.Orders.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class OrderErrorHandler
{
    public static ObjectResult ToObjectResult(this OrderException error)
    {
        return new ObjectResult(error.Message)
        {
            StatusCode = error switch
            {
                OrderNotFoundException => StatusCodes.Status404NotFound,
                OrderCustomerNotFoundException => StatusCodes.Status404NotFound,
                OrderFlowerNotFoundException => StatusCodes.Status404NotFound,
                OrderEmptyException => StatusCodes.Status400BadRequest,
                InsufficientStockForOrderException => StatusCodes.Status400BadRequest,
                UnhandledOrderException => StatusCodes.Status500InternalServerError,
                _ => StatusCodes.Status500InternalServerError
            }
        };
    }
}