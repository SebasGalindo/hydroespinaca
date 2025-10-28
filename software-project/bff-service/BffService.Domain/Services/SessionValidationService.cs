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

    /// <summary>
    /// Checks if the session requires token refresh.
    /// Uses proactive refresh strategy: refreshes 30 seconds before expiration.
    /// </summary>
    public bool RequiresRefresh(Session session)
    {
        return session.IsExpiringSoon() && session.CanRefresh();
    }
}