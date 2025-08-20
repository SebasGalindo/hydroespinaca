namespace HydroEspinaca.Shared.Utils;
public static class ObjectIdHelper
{
    public static bool IsValidObjectId(string? id) =>
        !string.IsNullOrEmpty(id) &&
        id.Length == 24 &&
        id.All(c =>
            char.IsDigit(c) ||
            (c >= 'a' && c <= 'f') ||
            (c >= 'A' && c <= 'F'));
}