using BiService.Application.Features.Production.Queries.GetProductionRecords;
using BiService.Domain.Entities;
using BiService.Domain.Interfaces;

namespace BiService.Application.Tests.Features.Production.Queries;

public class GetProductionRecordsQueryHandlerTests
{
    private readonly Mock<IProductionRecordRepository> _repo = new();
    private readonly GetProductionRecordsQueryHandler _handler;

    public GetProductionRecordsQueryHandlerTests()
    {
        _handler = new GetProductionRecordsQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnMappedRecords()
    {
        var r1 = new ProductionRecord { CropName = "Espinaca", KilosProduced = 10m, PricePerKilo = 12000m };
        r1.SetId("p-1");
        var r2 = new ProductionRecord { CropName = "Lechuga", KilosProduced = 5m, PricePerKilo = 8000m };
        r2.SetId("p-2");

        _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductionRecord> { r1, r2 });

        var result = await _handler.Handle(new GetProductionRecordsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Id.Should().Be("p-1");
        result[0].CropName.Should().Be("Espinaca");
        result[1].Id.Should().Be("p-2");
        result[1].CropName.Should().Be("Lechuga");
    }

    [Fact]
    public async Task Handle_NoRecords_ShouldReturnEmptyList()
    {
        _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductionRecord>());

        var result = await _handler.Handle(new GetProductionRecordsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
