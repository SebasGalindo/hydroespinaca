using BiService.Application.Features.Production.Queries.GetProductionRecordById;
using BiService.Domain.Entities;
using BiService.Domain.Interfaces;

namespace BiService.Application.Tests.Features.Production.Queries;

public class GetProductionRecordByIdQueryHandlerTests
{
    private readonly Mock<IProductionRecordRepository> _repo = new();
    private readonly GetProductionRecordByIdQueryHandler _handler;

    public GetProductionRecordByIdQueryHandlerTests()
    {
        _handler = new GetProductionRecordByIdQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_RecordExists_ShouldReturnDto()
    {
        var record = new ProductionRecord
        {
            CropName = "Espinaca",
            StartDate = new DateTime(2025, 1, 1),
            HarvestDate = new DateTime(2025, 2, 1),
            KilosProduced = 25.5m,
            PricePerKilo = 12000m,
            Currency = "COP",
            Note = "Good harvest"
        };
        record.SetId("p-1");
        _repo.Setup(r => r.GetByIdAsync("p-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        var result = await _handler.Handle(new GetProductionRecordByIdQuery("p-1"), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be("p-1");
        result.CropName.Should().Be("Espinaca");
        result.KilosProduced.Should().Be(25.5m);
    }

    [Fact]
    public async Task Handle_RecordNotFound_ShouldReturnNull()
    {
        _repo.Setup(r => r.GetByIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductionRecord?)null);

        var result = await _handler.Handle(new GetProductionRecordByIdQuery("missing"), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldMapAllFields()
    {
        var record = new ProductionRecord
        {
            CropName = "Lechuga",
            StartDate = new DateTime(2025, 3, 1),
            HarvestDate = new DateTime(2025, 4, 15),
            KilosProduced = 10m,
            PricePerKilo = 8000m,
            Currency = "USD",
            Note = "Second cycle",
            CreatedAt = new DateTime(2025, 3, 1)
        };
        record.SetId("p-2");
        _repo.Setup(r => r.GetByIdAsync("p-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        var result = await _handler.Handle(new GetProductionRecordByIdQuery("p-2"), CancellationToken.None);

        result!.StartDate.Should().Be(new DateTime(2025, 3, 1));
        result.HarvestDate.Should().Be(new DateTime(2025, 4, 15));
        result.Currency.Should().Be("USD");
        result.Note.Should().Be("Second cycle");
        result.CreatedAt.Should().Be(new DateTime(2025, 3, 1));
    }
}
