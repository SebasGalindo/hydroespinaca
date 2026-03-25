using BiService.Application.Features.Production.Commands.CreateProductionRecord;
using BiService.Domain.Entities;
using BiService.Domain.Interfaces;

namespace BiService.Application.Tests.Features.Production.Commands;

public class CreateProductionRecordCommandHandlerTests
{
    private readonly Mock<IProductionRecordRepository> _repo = new();
    private readonly CreateProductionRecordCommandHandler _handler;

    public CreateProductionRecordCommandHandlerTests()
    {
        _handler = new CreateProductionRecordCommandHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_ShouldCreateAndReturnDto()
    {
        _repo.Setup(r => r.CreateAsync(It.IsAny<ProductionRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductionRecord r, CancellationToken _) => { r.SetId("prod-1"); return r; });

        var cmd = new CreateProductionRecordCommand(
            "Espinaca", new DateTime(2025, 1, 1), new DateTime(2025, 2, 1),
            25.5m, 12000m, "COP", "First harvest", "user-1");

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be("prod-1");
        result.CropName.Should().Be("Espinaca");
        result.KilosProduced.Should().Be(25.5m);
        result.PricePerKilo.Should().Be(12000m);
        result.Currency.Should().Be("COP");
        result.Note.Should().Be("First harvest");
    }

    [Fact]
    public async Task Handle_CropNameShouldBeTrimmed()
    {
        _repo.Setup(r => r.CreateAsync(It.IsAny<ProductionRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductionRecord r, CancellationToken _) => { r.SetId("p-1"); return r; });

        var cmd = new CreateProductionRecordCommand(
            "  Espinaca  ", new DateTime(2025, 1, 1), new DateTime(2025, 2, 1),
            10m, 10000m, "COP", null, "user-1");

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.CropName.Should().Be("Espinaca");
    }

    [Fact]
    public async Task Handle_EmptyCurrency_ShouldDefaultToCOP()
    {
        _repo.Setup(r => r.CreateAsync(It.IsAny<ProductionRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductionRecord r, CancellationToken _) => { r.SetId("p-1"); return r; });

        var cmd = new CreateProductionRecordCommand(
            "Lechuga", new DateTime(2025, 1, 1), new DateTime(2025, 2, 1),
            5m, 8000m, "", null, "user-1");

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Currency.Should().Be("COP");
    }

    [Fact]
    public async Task Handle_CurrencyShouldBeUppercase()
    {
        _repo.Setup(r => r.CreateAsync(It.IsAny<ProductionRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductionRecord r, CancellationToken _) => { r.SetId("p-1"); return r; });

        var cmd = new CreateProductionRecordCommand(
            "Lechuga", new DateTime(2025, 1, 1), new DateTime(2025, 2, 1),
            5m, 8000m, "usd", null, "user-1");

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task Handle_NoteCanBeNull()
    {
        _repo.Setup(r => r.CreateAsync(It.IsAny<ProductionRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductionRecord r, CancellationToken _) => { r.SetId("p-1"); return r; });

        var cmd = new CreateProductionRecordCommand(
            "Cilantro", new DateTime(2025, 1, 1), new DateTime(2025, 2, 1),
            3m, 5000m, "COP", null, "user-1");

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Note.Should().BeNull();
    }
}
