using HydroEspinaca.Shared.Interfaces;

namespace HydroEspinaca.Shared.Utils;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
