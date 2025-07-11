using System.ComponentModel.DataAnnotations;

namespace SensorService.Domain.Config;
public class MongoSettings
{
    [Required]
    public string ConnectionString { get; set; }

    [Required]
    public string Database { get; set; }
}
