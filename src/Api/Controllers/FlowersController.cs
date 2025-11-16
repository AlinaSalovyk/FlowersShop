using Api.Dtos;
using Api.Modules.Errors;
using Application.Common.Interfaces.Queries;
using Application.Flowers.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/flowers")]
public class FlowersController(
    ISender sender,
    IFlowerQueries flowerQueries) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FlowerDto>>> GetAll(CancellationToken cancellationToken)
    {
        var flowers = await flowerQueries.GetAllAsync(cancellationToken);
        return flowers.Select(FlowerDto.FromDomainModel).ToList();
    }

    [HttpGet("category/{categoryId:guid}")]
    public async Task<ActionResult<IReadOnlyList<FlowerDto>>> GetByCategory(
        [FromRoute] Guid categoryId,
        CancellationToken cancellationToken)
    {
        var flowers = await flowerQueries.GetByCategoryIdAsync(categoryId, cancellationToken);
        return flowers.Select(FlowerDto.FromDomainModel).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<FlowerDto>> Create(
        [FromBody] CreateFlowerDto request,
        CancellationToken cancellationToken)
    {
        var command = new CreateFlowerCommand
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            Categories = request.Categories
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<FlowerDto>>(
            f => FlowerDto.FromDomainModel(f),
            e => e.ToObjectResult());
    }

    [HttpPut]
    public async Task<ActionResult<FlowerDto>> Update(
        [FromBody] UpdateFlowerDto request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateFlowerCommand
        {
            FlowerId = request.Id,
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            Categories = request.Categories
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<FlowerDto>>(
            f => FlowerDto.FromDomainModel(f),
            e => e.ToObjectResult());
    }

    [HttpDelete("{flowerId:guid}")]
    public async Task<ActionResult<FlowerDto>> Delete(
        [FromRoute] Guid flowerId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteFlowerCommand { FlowerId = flowerId };
        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<FlowerDto>>(
            f => FlowerDto.FromDomainModel(f),
            e => e.ToObjectResult());
    }

    [HttpPost("{flowerId:guid}/images")]
    public async Task<ActionResult<FlowerDto>> UploadImages(
        [FromRoute] Guid flowerId,
        [FromForm] IFormFileCollection files,
        CancellationToken cancellationToken)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest("No files provided");
        }

        var imageDtos = files.Select(file => new ImageFileDto
        {
            OriginalName = file.FileName,
            FileStream = file.OpenReadStream()
        }).ToList();

        var command = new UploadFlowerImagesCommand
        {
            FlowerId = flowerId,
            Images = imageDtos
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<FlowerDto>>(
            f => FlowerDto.FromDomainModel(f),
            e => e.ToObjectResult());
    }

    [HttpDelete("{flowerId:guid}/images/{imageId:guid}")]
    public async Task<ActionResult<FlowerDto>> DeleteImage(
        [FromRoute] Guid flowerId,
        [FromRoute] Guid imageId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteFlowerImageCommand
        {
            FlowerId = flowerId,
            ImageId = imageId
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<FlowerDto>>(
            f => FlowerDto.FromDomainModel(f),
            e => e.ToObjectResult());
    }
}