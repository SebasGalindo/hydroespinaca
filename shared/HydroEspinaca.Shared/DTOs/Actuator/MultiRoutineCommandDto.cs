namespace HydroEspinaca.Shared.DTOs.Actuator;

public class MultiRoutineCommandDto
{
    public List<RoutineCommandDto> Routines { get; set; } = new();
}