namespace ActuatorService.Application.Services;

public interface ICommandIdGenerator
{
    string Generate(string routineId);
}

public class CommandIdGenerator : ICommandIdGenerator
{
    public string Generate(string routineId)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddTHHmm");
        return $"{routineId}_{timestamp}";
    }
}