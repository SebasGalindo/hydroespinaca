using Xunit;

namespace AuthService.Test.Fixtures;

/// <summary>
/// Ensures all integration tests run sequentially to avoid database conflicts
/// </summary>
[CollectionDefinition("IntegrationTests", DisableParallelization = true)]
public class TestCollectionDefinition : ICollectionFixture<IntegrationTestBase>
{
}