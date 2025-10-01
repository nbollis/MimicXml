using MimicXml;
using Microsoft.Extensions.DependencyInjection;

namespace Test;
[SetUpFixture]
internal class SetUpTests
{
    [OneTimeSetUp]
    public void GlobalSetup()
    {
        // Code that runs once before any tests in the assembly
        Console.WriteLine("Global setup before any tests run.");

        var services = AppHost.CreateBaseServices("appsettings.json");
        AppHost.Services = services.BuildServiceProvider();
    }

    [OneTimeTearDown]
    public void GlobalTeardown()
    {
        // Code that runs once after all tests in the assembly
        Console.WriteLine("Global teardown after all tests have run.");
    }
}
