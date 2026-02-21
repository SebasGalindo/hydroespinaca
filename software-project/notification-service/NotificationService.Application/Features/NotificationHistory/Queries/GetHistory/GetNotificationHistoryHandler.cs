using MediatR;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Application.Features.NotificationHistory.Queries.GetHistory;

public class GetNotificationHistoryHandler
    : IRequestHandler<GetNotificationHistoryQuery, IEnumerable<NotificationLog>>
{
    private readonly INotificationLogRepository _repository;

    public GetNotificationHistoryHandler(INotificationLogRepository repository)
        => _repository = repository;

    public async Task<IEnumerable<NotificationLog>> Handle(
        GetNotificationHistoryQuery request, CancellationToken ct)
        => await _repository.GetByUserIdAsync(
            request.UserId, request.Channel, request.From, request.To, request.Limit, ct);
}
