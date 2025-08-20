using System.IO;
using System.Threading.Tasks;
using NotificationService.Infrastructure.Templating;
using Xunit;

namespace NotificationService.Test.Integration;

public class FluidTemplateRendererIntegrationTests
{
    [Fact]
    public async Task Renders_Base_Layout_From_Files()
    {
        var renderer = new FluidTemplateRenderer();
        var html = await renderer.RenderAsync("layouts/base", "<p>Hola</p>");
        Assert.Contains("Hola", html);
        Assert.Contains("HydroEspinaca", html);
        Assert.Contains("<html", html);
    }
}
