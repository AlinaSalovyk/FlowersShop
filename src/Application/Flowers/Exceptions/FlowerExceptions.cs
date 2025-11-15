using Domain.Flowers;

namespace Application.Flowers.Exceptions;

public abstract class FlowerException(FlowerId flowerId, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public FlowerId FlowerId { get; } = flowerId;
}

public class FlowerAlreadyExistException(FlowerId flowerId) 
    : FlowerException(flowerId, $"Flower already exists under id {flowerId}");

public class FlowerNotFoundException(FlowerId flowerId) 
    : FlowerException(flowerId, $"Flower not found under id {flowerId}");

public class FlowerCategoriesNotFoundException(FlowerId flowerId) 
    : FlowerException(flowerId, $"One or more categories not found for flower {flowerId}");

public class UnhandledFlowerException(FlowerId flowerId, Exception? innerException = null)
    : FlowerException(flowerId, "Unexpected error occurred", innerException);