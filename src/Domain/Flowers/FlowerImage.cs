namespace Domain.Flowers;

public class FlowerImage
{
    public FlowerImageId Id { get; }
    public string OriginalName { get; }
    public FlowerId FlowerId { get; }

    private FlowerImage(FlowerImageId id, string originalName, FlowerId flowerId)
    {
        Id = id;
        OriginalName = originalName;
        FlowerId = flowerId;
    }

    public static FlowerImage New(FlowerId flowerId, string originalName)
    {
        return new FlowerImage(FlowerImageId.New(), originalName, flowerId);
    }

    public string GetFilePath()
        => $"{FlowerId}/{Id}{Path.GetExtension(OriginalName)}";
}