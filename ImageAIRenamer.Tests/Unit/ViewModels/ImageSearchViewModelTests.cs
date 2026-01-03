using ImageAIRenamer.Application.ViewModels;
using ImageAIRenamer.Domain.Entities;
using ImageAIRenamer.Tests.Mocks;
using Moq;

namespace ImageAIRenamer.Tests.Unit.ViewModels;

/// <summary>
/// Unit tests for ImageSearchViewModel
/// </summary>
public class ImageSearchViewModelTests
{
    private ImageSearchViewModel CreateViewModel(
        Mock<Domain.Interfaces.INavigationService>? navigationService = null,
        Mock<Domain.Interfaces.IGeminiService>? geminiService = null,
        Mock<Domain.Interfaces.IFileService>? fileService = null,
        Mock<Domain.Interfaces.IConfigurationService>? configurationService = null,
        Mock<Domain.Interfaces.IImageProcessingService>? imageProcessingService = null)
    {
        return new ImageSearchViewModel(
            (navigationService ?? MockServices.CreateNavigationService()).Object,
            (geminiService ?? MockServices.CreateGeminiService()).Object,
            (fileService ?? MockServices.CreateFileService()).Object,
            (configurationService ?? MockServices.CreateConfigurationService()).Object,
            (imageProcessingService ?? MockServices.CreateImageProcessingService()).Object
        );
    }

    #region Initial State Tests

    [Fact]
    public void Constructor_InitializesEmptyMatchedImages()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.Empty(viewModel.MatchedImages);
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
    public void Constructor_InitializesEmptySearchDescription()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.Equal(string.Empty, viewModel.SearchDescription);
    }

    [Fact]
    public void Constructor_SearchIsInitiallyDisabled()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.False(viewModel.IsSearchEnabled);
    }

    #endregion

    #region ClearListCommand Tests

    [Fact]
    public void ClearListCommand_ClearsAllData()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.MatchedImages.Add(new ImageItem { FilePath = "test.jpg", OriginalName = "test" });
        viewModel.SourceFolder = @"C:\Source";
        viewModel.OutputFolder = @"C:\Output";
        viewModel.SearchDescription = "Test description";
        viewModel.StatusText = "Some status";

        // Act
        viewModel.ClearListCommand.Execute(null);

        // Assert
        Assert.Empty(viewModel.MatchedImages);
        Assert.Equal(string.Empty, viewModel.SourceFolder);
        Assert.Equal(string.Empty, viewModel.OutputFolder);
        Assert.Equal(string.Empty, viewModel.SearchDescription);
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

    [Fact]
    public void ProgressText_InitiallyEmpty()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.Equal(string.Empty, viewModel.ProgressText);
    }

    #endregion

    #region SearchDescription Change Tests

    [Fact]
    public void SearchDescription_ChangesAreTracked()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.SearchDescription = "Find cats";

        // Assert
        Assert.Equal("Find cats", viewModel.SearchDescription);
    }

    #endregion
}
