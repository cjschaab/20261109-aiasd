using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TodoManager;
using Xunit;

namespace TodoManager.Tests;

public sealed class FeatureFlagsTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "FeatureFlagsTests", Guid.NewGuid().ToString("N"));
    private string TaskPath => Path.Combine(directory, "tasks.db");

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    public void IsEnabledReadsConfiguredValues(string value, bool expected)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["FeatureFlags:NewFeature"] = value })
            .Build();
        var featureFlags = new ConfigurationFeatureFlags(configuration);

        Assert.Equal(expected, featureFlags.IsEnabled("NewFeature"));
    }

    [Fact]
    public void MissingFeatureIsDisabled()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var featureFlags = new ConfigurationFeatureFlags(configuration);

        Assert.False(featureFlags.IsEnabled("UnknownFeature"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void IsEnabledRejectsInvalidFeatureNames(string? featureName)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var featureFlags = new ConfigurationFeatureFlags(configuration);

        Assert.ThrowsAny<ArgumentException>(() =>
        {
            featureFlags.IsEnabled(featureName!);
        });
    }

    [Fact]
    public void IsEnabledReadsUpdatedConfiguration()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var featureFlags = new ConfigurationFeatureFlags(configuration);

        Assert.False(featureFlags.IsEnabled("NewFeature"));
        configuration["FeatureFlags:NewFeature"] = "true";
        Assert.True(featureFlags.IsEnabled("NewFeature"));
    }

    [Fact]
    public void FeatureFlagsAreRegisteredAndReadApplicationConfiguration()
    {
        using var app = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseEnvironment("Development")
            .UseSetting("FeatureFlags:Login", "false")
            .UseSetting("TaskDatabase:Path", TaskPath)
            .UseSetting("FeatureFlags:NewFeature", "true"));

        var featureFlags = app.Services.GetRequiredService<IFeatureFlags>();

        Assert.True(featureFlags.IsEnabled("NewFeature"));
    }

    public void Dispose()
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = TaskPath,
            ForeignKeys = true
        }.ToString());
        SqliteConnection.ClearPool(connection);
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }
}
