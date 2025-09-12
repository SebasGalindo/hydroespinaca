namespace HydroEspinaca.Shared.Responses;
public class SuccessResponse
{
    public string Message { get; set; } = "Operation successful";
    public object? Data { get; set; }
}