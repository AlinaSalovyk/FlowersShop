using Domain.Flowers;

namespace Tests.Data.Flowers;

public static class FlowersData
{
    public static Flower FirstTestFlower()
        => Flower.New(
            FlowerId.New(),
            "Red Rose",
            "Beautiful red rose",
            19.99m,
            100,
            []);

    public static Flower SecondTestFlower()
        => Flower.New(
            FlowerId.New(),
            "White Tulip",
            "Elegant white tulip",
            14.99m,
            50,
            []);

    public static Flower ThirdTestFlower()
        => Flower.New(
            FlowerId.New(),
            "Purple Orchid",
            "Exotic purple orchid",
            29.99m,
            30,
            []);

    public static FlowerImage FirstTestFlowerImage(FlowerId flowerId)
        => FlowerImage.New(flowerId, "test-image-1.jpg");

    public static FlowerImage SecondTestFlowerImage(FlowerId flowerId)
        => FlowerImage.New(flowerId, "test-image-2.jpg");
}