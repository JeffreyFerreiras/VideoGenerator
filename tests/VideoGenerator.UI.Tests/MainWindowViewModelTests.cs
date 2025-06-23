using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using VideoGenerator.Services;
using VideoGenerator.UI.ViewModels;

namespace VideoGenerator.UI.Tests
{
    [TestFixture, Apartment(System.Threading.ApartmentState.STA)]
    public class MainWindowViewModelTests
    {
        private IVideoGenerationService _service = null!;
        private ILogger<MainWindowViewModel> _logger = null!;
        private MainWindowViewModel _viewModel = null!;

        [SetUp]
        public void SetUp()
        {
            _service = Substitute.For<IVideoGenerationService>();
            _logger = Substitute.For<ILogger<MainWindowViewModel>>();
            _viewModel = new MainWindowViewModel(_service, _logger);
        }

        [Test]
        public async Task LoadModelAsync_WhenServiceReturnsTrue_SetsIsModelLoadedAndStatus()
        {
            _service.LoadModelAsync(Arg.Any<string>()).Returns(Task.FromResult(true));

            await _viewModel.LoadModelCommand.ExecuteAsync(null);

            _viewModel.IsModelLoaded.Should().BeTrue();
            _viewModel.StatusMessage.Should().Be("Model loaded successfully");
        }

        [Test]
        public void GenerateVideoCommand_CanExecute_ReflectsModelLoadedState()
        {
            // Before loading model
            _viewModel.GenerateVideoCommand.CanExecute(null).Should().BeFalse();

            // Simulate model loaded
            _viewModel.IsModelLoaded = true;
            _viewModel.GenerateVideoCommand.CanExecute(null).Should().BeTrue();
        }
    }
}