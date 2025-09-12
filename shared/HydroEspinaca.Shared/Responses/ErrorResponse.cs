namespace HydroEspinaca.Shared.Responses;
public class ErrorResponse
{
    public string Message { get; set; } = "Internal Server Error";
    public string? Detail { get; set; }
    public string? Stack { get; set; }
    public List<string>? Errors { get; set; }
}
