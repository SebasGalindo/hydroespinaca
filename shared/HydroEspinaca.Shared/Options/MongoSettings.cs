using System.ComponentModel.DataAnnotations;

namespace HydroEspinaca.Shared.Options;

public class MongoSettings
{
    [Required]
    public string ConnectionString { get; set; }

    [Required]
    public string Database { get; set; }
}
