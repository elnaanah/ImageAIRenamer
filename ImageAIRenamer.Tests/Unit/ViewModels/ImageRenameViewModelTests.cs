using ImageAIRenamer.Application.ViewModels;
using ImageAIRenamer.Domain.Entities;
using ImageAIRenamer.Tests.Mocks;
using Moq;

namespace ImageAIRenamer.Tests.Unit.ViewModels;

/// <summary>
/// Unit tests for ImageRenameViewModel
/// </summary>
public class ImageRenameViewModelTests
{
    private ImageRenameViewModel CreateViewModel(
        Mock<Domain.Interfaces.INavigationService>? navigationService = null,
        Mock<Domain.Interfaces.IGeminiService>? geminiService = null,
        Mock<Domain.Interfaces.IFileService>? fileService = null,
        Mock<Domain.Interfaces.IConfigurationService>? configurationService = null,
        Mock<Domain.Interfaces.IImageProcessingService>? imageProcessingService = null)
    {
        return new ImageRenameViewModel(
            (navigationService ?? MockServices.CreateNavigationService()).Object,
            (geminiService ?? MockServices.CreateGeminiService()).Object,
            (fileService ?? MockServices.CreateFileService()).Object,
            (configurationService ?? MockServices.CreateConfigurationService()).Object,
            (imageProcessingService ?? MockServices.CreateImageProcessingService()).Object
        );
    }

    #region Initial State Tests

    [Fact]
    public void Constructor_InitializesEmptyImages()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.Empty(viewModel.Images);
    }

    [Fact]
    public void Constructor_InitializesEmptySourceFolder()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.Equal(string.Empty, viewModel.SourceFolder);
    }

    [Fact]
    public void Constructor_InitializesEmptyOutputFolder()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.Equal(string.Empty, viewModel.OutputFolder);
    }

    [Fact]
    public void Constructor_ProcessIsInitiallyDisabled()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.False(viewModel.IsProcessEnabled);
    }

    #endregion

    #region IsProcessEnabled Tests

    [Fact]
    public void IsProcessEnabled_FalseWhenNoImages()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.OutputFolder = @"C:\Output";

        // Assert
        Assert.False(viewModel.IsProcessEnabled);
    }

    [Fact]
    public void IsProcessEnabled_FalseWhenNoOutputFolder()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Images.Add(new ImageItem { FilePath = "test.jpg", OriginalName = "test" });

        // Assert - still false because CheckReady isn't called automatically
        Assert.False(viewModel.IsProcessEnabled);
    }

    #endregion

    #region ClearListCommand Tests

    [Fact]
    public void ClearListCommand_ClearsAllData()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Images.Add(new ImageItem { FilePath = "test.jpg", OriginalName = "test" });
        viewModel.SourceFolder = @"C:\Source";
        viewModel.OutputFolder = @"C:\Output";
        viewModel.CustomInstructions = "Test instructions";
        viewModel.StatusText = "Some status";

        // Act
        viewModel.ClearListCommand.Execute(null);

        // Assert
        Assert.Empty(viewModel.Images);
        Assert.Equal(string.Empty, viewModel.SourceFolder);
        Assert.Equal(string.Empty, viewModel.OutputFolder);
        Assert.Equal(string.Empty, viewModel.CustomInstructions);
        Assert.Equal(string.Empty, viewModel.StatusText);
    }

    #endregion

    #region BackToHomeCommand Tests

    [Fact]
    public void BackToHomeCommand_NavigatesToWelcome()
    {
        // Arrange
        var navMock = MockServices.CreateNavigationService();
        var viewModel = CreateViewModel(navigationService: navMock);

        // Act
        viewModel.BackToHomeCommand.Execute(null);

        // Assert
        navMock.Verify(x => x.NavigateToWelcome(), Times.Once);
    }

    #endregion

    #region Progress Tests

    [Fact]
    public void Progress_InitiallyNotVisible()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.False(viewModel.IsProgressVisible);
        Assert.Equal(0, viewModel.ProgressValue);
    }

    #endregion

    #region API Keys Tests

    [Fact]
    public async Task ViewModel_LoadsApiKeysFromConfiguration()
    {
        // Arrange
        var configMock = MockServices.CreateConfigurationService(new[] { "key1", "key2" });
        var viewModel = CreateViewModel(configurationService: configMock);

        // Wait for async load
        await Task.Delay(100);

        // Assert
        configMock.Verify(x => x.GetApiKeysAsync(), Times.Once);
    }

    #endregion
}
