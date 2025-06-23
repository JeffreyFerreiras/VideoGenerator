using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using VideoGenerator.Services;
using VideoGenerator.UI;
using VideoGenerator.UI.ViewModels;

namespace VideoGenerator.UI.Tests;

public class DependencyInjectionTests
{
    private IHost _host = default!;

    [SetUp]
    public void SetUp()
    {
        _host = Host.CreateDefaultBuilder()
            .AddVideoGeneratorUI()
            .Build();
    }

    [Test]
    public void Should_Resolve_IVideoGenerationService_As_PythonVideoGenerationService()
    {
        var service = _host.Services.GetRequiredService<IVideoGenerationService>();
        service.Should().BeOfType<PythonVideoGenerationService>();
    }

    [Test]
    public void Should_Resolve_MainWindowViewModel()
    {
        var viewModel = _host.Services.GetRequiredService<MainWindowViewModel>();
        viewModel.Should().NotBeNull();
    }

    [Test]
    public void IVideoGenerationService_Should_Be_Singleton()
    {
        var service1 = _host.Services.GetRequiredService<IVideoGenerationService>();
        var service2 = _host.Services.GetRequiredService<IVideoGenerationService>();
        service1.Should().BeSameAs(service2);
    }

    [Test]
    public void MainWindowViewModel_Should_Be_Transient()
    {
        var vm1 = _host.Services.GetRequiredService<MainWindowViewModel>();
        var vm2 = _host.Services.GetRequiredService<MainWindowViewModel>();
        vm1.Should().NotBeSameAs(vm2);
    }
}