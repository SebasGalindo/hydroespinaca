using BffService.Domain.Entities;
using BffService.Domain.Exceptions;

namespace BffService.Domain.Services;

public class SessionValidationService
{
    public bool IsSessionValid(Session session)
    {
        return session != null && session.HasValidTokens();
    }

    public void ValidateSessionOrThrow(Session? session, string sessionId)
    {
        if (session == null)
        {
            throw new SessionNotFoundException(sessionId);
        }

        if (session.IsExpired())
        {
            throw new SessionExpiredException(sessionId);
        }
    }

    public bool RequiresRefresh(Session session)
    {
        if (session.IsExpired())
        {
            return session.CanRefresh();
        }

        var timeToExpiry = session.ExpiresAt - DateTime.UtcNow;
        return timeToExpiry.TotalMinutes < 15 && session.CanRefresh();
    }
}