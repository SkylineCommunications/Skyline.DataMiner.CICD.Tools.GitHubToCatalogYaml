namespace CICD.Tools.GitHubToCatalogYamlTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using FluentAssertions;

    using Microsoft.Extensions.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Serilog;

    using Skyline.DataMiner.CICD.FileSystem;
    using Skyline.DataMiner.CICD.Tools.GitHubToCatalogYaml;

    [TestClass]
    public class CatalogManagerTests
    {
        private Mock<IFileSystem> mockFileSystem;
        private Microsoft.Extensions.Logging.ILogger logger;
        private Mock<IGitHubService> mockGitHubService;
        private CatalogManager catalogManager;
        private const string workspace = "testWorkspace";
        private const string catalogFilePath = "testWorkspace/catalog.yml";
        private const string manifestFilePath = "testWorkspace/manifest.yml";

        [TestInitialize]
        public void Setup()
        {
            // Arrange
            mockFileSystem = new Mock<IFileSystem>();
            mockGitHubService = new Mock<IGitHubService>();

            var logConfig = new LoggerConfiguration().WriteTo.Console();
            logConfig.MinimumLevel.Is(Serilog.Events.LogEventLevel.Debug);
            var seriLog = logConfig.CreateLogger();

            using var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog(seriLog));
            logger = loggerFactory.CreateLogger("Skyline.DataMiner.CICD.Tools.GitHubToCatalogYaml");

            mockFileSystem.Setup(fs => fs.Path.Combine(workspace, "catalog.yml")).Returns(catalogFilePath);
            mockFileSystem.Setup(fs => fs.Path.Combine(workspace, "manifest.yml")).Returns(manifestFilePath);

            catalogManager = new CatalogManager(mockFileSystem.Object, logger, mockGitHubService.Object, workspace);
        }

        [TestMethod]
        public async Task ProcessCatalogYamlAsync_ShouldCreateNewCatalogYaml_WhenNoFileExists()
        {
            // Arrange
            var repoName = "SLC-AS-testRepo";

            mockFileSystem.Setup(fs => fs.File.Exists(catalogFilePath)).Returns(false); // No catalog.yml exists
            mockFileSystem.Setup(fs => fs.File.Exists(manifestFilePath)).Returns(false); // No manifest.yml exists
            mockGitHubService.Setup(s => s.GetRepositoryDescriptionAsync()).ReturnsAsync("new description");
            mockGitHubService.Setup(s => s.GetRepositoryTopicsAsync()).ReturnsAsync(new List<string> { "newTopic" });

            // Act
            await catalogManager.ProcessCatalogYamlAsync(repoName);

            // Assert
            mockFileSystem.Verify(fs => fs.File.WriteAllText(catalogFilePath, It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task ProcessCatalogYamlAsync_ShouldLoadExistingCatalogYaml_WhenFileExists()
        {
            // Arrange
            var repoName = "SLC-AS-testRepo";
            var yamlContent = "id: testId\nshort_description: test description\ntags: [testTag]";
            mockFileSystem.Setup(fs => fs.File.Exists(catalogFilePath)).Returns(true);
            mockFileSystem.Setup(fs => fs.File.ReadAllText(catalogFilePath)).Returns(yamlContent);

            // Act
            await catalogManager.ProcessCatalogYamlAsync(repoName);

            // Assert
            mockFileSystem.Verify(fs => fs.File.WriteAllText(catalogFilePath, It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task ProcessCatalogYamlAsync_ShouldCreateNewManifestFile_WhenCatalogYamlNotExistsButManifestExists()
        {
            // Arrange
            var repoName = "SLC-AS-testRepo";
            var yamlContent = "id: testId\nshort_description: test description\ntags: [testTag]";
            mockFileSystem.Setup(fs => fs.File.Exists(catalogFilePath)).Returns(false); // No catalog.yml exists
            mockFileSystem.Setup(fs => fs.File.Exists(manifestFilePath)).Returns(true);  // manifest.yml exists
            mockFileSystem.Setup(fs => fs.File.ReadAllText(manifestFilePath)).Returns(yamlContent);

            // Act
            await catalogManager.ProcessCatalogYamlAsync(repoName);

            // Assert
            mockFileSystem.Verify(fs => fs.File.WriteAllText(manifestFilePath, It.IsAny<string>()), Times.Once); // Ensure it saves to catalog.yml
        }

        [TestMethod]
        public async Task ProcessCatalogYamlAsync_ShouldAssignNewId_WhenIdIsMissing()
        {
            // Arrange
            var repoName = "SLC-AS-testRepo";
            var yamlContent = "short_description: test description\ntags: [testTag]";
            mockFileSystem.Setup(fs => fs.File.Exists(catalogFilePath)).Returns(true); // catalog.yml exists
            mockFileSystem.Setup(fs => fs.File.ReadAllText(catalogFilePath)).Returns(yamlContent);

            // Act
            await catalogManager.ProcessCatalogYamlAsync(repoName, "newCatalogId");

            // Assert
            mockFileSystem.Verify(fs => fs.File.WriteAllText(catalogFilePath, It.Is<string>(s => s.Contains("id: newCatalogId"))), Times.Once);
        }

        [TestMethod]
        public async Task ProcessCatalogYamlAsync_ShouldUseDefaultDescription_WhenRepositoryDescriptionIsNull()
        {
            // Arrange
            var repoName = "SLC-AS-testRepo";
            var yamlContent = "id: testId\ntags: [testTag]";
            mockFileSystem.Setup(fs => fs.File.Exists(catalogFilePath)).Returns(true); // catalog.yml exists
            mockFileSystem.Setup(fs => fs.File.ReadAllText(catalogFilePath)).Returns(yamlContent);
            mockGitHubService.Setup(s => s.GetRepositoryDescriptionAsync()).ReturnsAsync((string)null);

            // Act
            await catalogManager.ProcessCatalogYamlAsync(repoName);

            // Assert
            mockFileSystem.Verify(fs => fs.File.WriteAllText(catalogFilePath, It.Is<string>(s => s.Contains("short_description: No description available"))), Times.Once);
        }

        [TestMethod]
        public async Task ProcessCatalogYamlAsync_ShouldAddTagsFromRepository_WhenTagsAreMissing()
        {
            // Arrange
            var repoName = "SLC-AS-testRepo";
            var yamlContent = "id: testId\nshort_description: test description";
            mockFileSystem.Setup(fs => fs.File.Exists(catalogFilePath)).Returns(true); // catalog.yml exists
            mockFileSystem.Setup(fs => fs.File.ReadAllText(catalogFilePath)).Returns(yamlContent);
            mockGitHubService.Setup(s => s.GetRepositoryTopicsAsync()).ReturnsAsync(new List<string> { "newTag" });

            // Act
            await catalogManager.ProcessCatalogYamlAsync(repoName);

            // Assert
            mockFileSystem.Verify(fs => fs.File.WriteAllText(catalogFilePath, It.Is<string>(s => s.Contains($"tags:{Environment.NewLine}- newTag"))), Times.Once);
        }

        [TestMethod]
        public async Task ProcessCatalogYamlAsync_ShouldUseRepositoryNameAsTitle_WhenTitleIsMissing()
        {
            // Arrange
            var repoName = "SLC-AS-testRepo";
            var yamlContent = "id: testId\nshort_description: test description";
            mockFileSystem.Setup(fs => fs.File.Exists(catalogFilePath)).Returns(true); // catalog.yml exists
            mockFileSystem.Setup(fs => fs.File.ReadAllText(catalogFilePath)).Returns(yamlContent);

            // Act
            await catalogManager.ProcessCatalogYamlAsync(repoName);

            // Assert
            mockFileSystem.Verify(fs => fs.File.WriteAllText(catalogFilePath, It.Is<string>(s => s.Contains("title: testRepo"))), Times.Once);
        }

        //[TestMethod]
        //public async Task ProcessCatalogYamlAsync_ShouldNotAddTags_WhenTagsAlreadyExist()
        //{
        //	// Arrange
        //	var repoName = "SLC-AS-testRepo";
        //	var yamlContent = "id: testId\nshort_description: test description\ntags:\n- existingTag";
        //	mockFileSystem.Setup(fs => fs.File.Exists(catalogFilePath)).Returns(true); // catalog.yml exists
        //	mockFileSystem.Setup(fs => fs.File.ReadAllText(catalogFilePath)).Returns(yamlContent);

        //	// Act
        //	await catalogManager.ProcessCatalogYamlAsync(repoName);

        //	// Assert
        //	mockFileSystem.Verify(fs => fs.File.WriteAllText(catalogFilePath, It.Is<string>(s => s.Contains($"tags:{Environment.NewLine}- existingTag"))), Times.Once);
        //	mockGitHubService.Verify(s => s.GetRepositoryTopicsAsync(), Times.Never); // Ensure no topics are fetched
        //}

        [TestMethod]
        public async Task ProcessCatalogYamlAsync_ShouldUseExistingShortDescription_WhenItExists()
        {
            // Arrange
            var repoName = "SLC-AS-testRepo";
            var yamlContent = "id: testId\nshort_description: Existing description\ntags: [testTag]";
            mockFileSystem.Setup(fs => fs.File.Exists(catalogFilePath)).Returns(true); // catalog.yml exists
            mockFileSystem.Setup(fs => fs.File.ReadAllText(catalogFilePath)).Returns(yamlContent);

            // Act
            await catalogManager.ProcessCatalogYamlAsync(repoName);

            // Assert
            mockFileSystem.Verify(fs => fs.File.WriteAllText(catalogFilePath, It.Is<string>(s => s.Contains("short_description: Existing description"))), Times.Once);
            mockGitHubService.Verify(s => s.GetRepositoryDescriptionAsync(), Times.Never); // Ensure description is not fetched
        }

        [TestMethod]
        public async Task ProcessCatalogYamlAsync_ShouldInferTypeFromRepositoryName_WhenNoTypeIsDefined()
        {
            // Arrange{ "DISMACRO", "dismacro" },
            var repoName = "SLC-DISMACRO-testRepo";
            var yamlContent = "id: testId\nshort_description: test description\ntags: [testTag]";
            mockFileSystem.Setup(fs => fs.File.Exists(catalogFilePath)).Returns(true); // catalog.yml exists
            mockFileSystem.Setup(fs => fs.File.ReadAllText(catalogFilePath)).Returns(yamlContent);

            // Act
            await catalogManager.ProcessCatalogYamlAsync(repoName);

            // Assert
            mockFileSystem.Verify(fs => fs.File.WriteAllText(catalogFilePath, It.Is<string>(s => s.Contains("type: dismacro"))), Times.Once);
        }

        [TestMethod]
        public async Task ProcessCatalogYamlAsync_ShouldNotOverwriteExistingTitle_WhenItExists()
        {
            // Arrange
            var repoName = "SLC-AS-testRepo";
            var yamlContent = "id: testId\ntitle: ExistingTitle\nshort_description: test description\ntags: [testTag]";
            mockFileSystem.Setup(fs => fs.File.Exists(catalogFilePath)).Returns(true); // catalog.yml exists
            mockFileSystem.Setup(fs => fs.File.ReadAllText(catalogFilePath)).Returns(yamlContent);

            // Act
            await catalogManager.ProcessCatalogYamlAsync(repoName);

            // Assert
            mockFileSystem.Verify(fs => fs.File.WriteAllText(catalogFilePath, It.Is<string>(s => s.Contains("title: ExistingTitle"))), Times.Once);
        }

        [TestMethod]
        public async Task ProcessCatalogYamlAsync_ShouldSaveYamlFileToCorrectPath_WhenNoFilePathIsGiven()
        {
            // Arrange
            var repoName = "SLC-AS-testRepo";
            mockFileSystem.Setup(fs => fs.File.Exists(catalogFilePath)).Returns(false); // catalog.yml does not exist
            mockFileSystem.Setup(fs => fs.File.Exists(manifestFilePath)).Returns(false);  // manifest.yml does not exist

            // Act
            await catalogManager.ProcessCatalogYamlAsync(repoName);

            // Assert
            mockFileSystem.Verify(fs => fs.File.WriteAllText(It.Is<string>(p => p == catalogFilePath), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task ProcessCatalogYamlAsync_ShouldLogInformationMessage_WhenProcessingStartsAndEnds()
        {
            // Arrange

            var mockFileSystem = new Mock<IFileSystem>();
            var mockGitHubService = new Mock<IGitHubService>();
            var mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger>();

            mockFileSystem.Setup(fs => fs.Path.Combine(workspace, "catalog.yml")).Returns(catalogFilePath);
            mockFileSystem.Setup(fs => fs.Path.Combine(workspace, "manifest.yml")).Returns(manifestFilePath);

            catalogManager = new CatalogManager(mockFileSystem.Object, mockLogger.Object, mockGitHubService.Object, workspace);


            var repoName = "SLC-AS-testRepo";
            var yamlContent = "id: testId\ntitle: ExistingTitle\nshort_description: test description\ntags: [testTag]";
            mockFileSystem.Setup(fs => fs.File.Exists(catalogFilePath)).Returns(true); // catalog.yml exists
            mockFileSystem.Setup(fs => fs.File.ReadAllText(catalogFilePath)).Returns(yamlContent);

            // Act
            await catalogManager.ProcessCatalogYamlAsync(repoName);

            // Assert
            //	mockLogger.Verify(l => l.Log(LogLevel.Information, 0, It.Is<It.IsAnyType>((@object, @type) => @object.ToString() == "myMessage" && @type.Name == "FormattedLogValues"), It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => .Contains("Extracting Information from GitHub...")), Times.Once);
            //	mockLogger.Verify(l => l.Log(LogLevel.Information, 0, new FormattedLogValues(message, args), null, It.Is<string>(msg => msg.Contains("Finished. Updated or Created file"))), Times.Once);

            mockLogger.Verify(
    x => x.Log(
        LogLevel.Information,
        It.IsAny<EventId>(),
        It.Is<It.IsAnyType>((v, t) => true),
        It.IsAny<Exception>(),
        It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true))
    , Times.AtLeast(2));
        }

        [TestMethod]
        public async Task ProcessCatalogYamlAsync_ShouldThrowException_WhenFileSystemThrows()
        {
            // Arrange
            var repoName = "SLC-AS-testRepo";
            mockFileSystem.Setup(fs => fs.File.Exists(It.IsAny<string>())).Throws(new Exception("File system failure"));

            // Act
            Func<Task> act = async () => await catalogManager.ProcessCatalogYamlAsync(repoName);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("File system failure");
        }

        [TestMethod]
        public async Task ProcessCatalogYamlAsync_ShouldThrowInvalidOperationException_WhenTypeCannotBeInferred()
        {
            // Arrange
            var repoName = "SLC-testRepo";
            var yamlContent = "id: testId\nshort_description: test description\ntags: [unknownTag]";
            mockFileSystem.Setup(fs => fs.File.Exists(catalogFilePath)).Returns(true); // catalog.yml exists
            mockFileSystem.Setup(fs => fs.File.ReadAllText(catalogFilePath)).Returns(yamlContent);

            // Act
            Func<Task> act = async () => await catalogManager.ProcessCatalogYamlAsync(repoName);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().Where(e => e.Message.StartsWith("Could not identify Type from GitHub"));
        }

        [TestMethod]
        public async Task ProcessCatalogYamlAsync_ShouldThrowException_WhenReadAllTextFails()
        {
            // Arrange
            var repoName = "SLC-AS-testRepo";
            mockFileSystem.Setup(fs => fs.File.Exists(catalogFilePath)).Returns(true);
            mockFileSystem.Setup(fs => fs.File.ReadAllText(It.IsAny<string>())).Throws(new Exception("File read failure"));

            // Act
            Func<Task> act = async () => await catalogManager.ProcessCatalogYamlAsync(repoName);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("File read failure");
        }

        [TestMethod]
        public async Task ProcessCatalogYamlAsync_ShouldLogError_WhenSavingFails()
        {
            // Arrange
            var repoName = "SLC-AS-testRepo";
            var yamlContent = "id: testId\nshort_description: test description";
            mockFileSystem.Setup(fs => fs.File.Exists(catalogFilePath)).Returns(true); // catalog.yml exists
            mockFileSystem.Setup(fs => fs.File.ReadAllText(catalogFilePath)).Returns(yamlContent);
            mockFileSystem.Setup(fs => fs.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>())).Throws(new Exception("File write failure"));

            // Act
            Func<Task> act = async () => await catalogManager.ProcessCatalogYamlAsync(repoName);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("File write failure");
        }

        [TestMethod]
        public async Task ProcessCatalogYamlAsync_ShouldHandleNullOrEmptyRepoName_Gracefully()
        {
            // Arrange
            string repoName = null;

            // Act
            Func<Task> act = async () => await catalogManager.ProcessCatalogYamlAsync(repoName);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task ProcessCatalogYamlAsync_ShouldHandleEmptyCatalogFileGracefully()
        {
            // Arrange
            var repoName = "SLC-AS-testRepo";
            var yamlContent = "";  // Simulate empty file content
            mockFileSystem.Setup(fs => fs.File.Exists(catalogFilePath)).Returns(true); // catalog.yml exists
            mockFileSystem.Setup(fs => fs.File.ReadAllText(catalogFilePath)).Returns(yamlContent);

            // Act
            Func<Task> act = async () => await catalogManager.ProcessCatalogYamlAsync(repoName);

            // Assert
            await act.Should().NotThrowAsync();  // The method should handle empty content gracefully
        }

        [TestMethod]
        public async Task ProcessCatalogYamlAsync_ShouldHandleInvalidYamlFileGracefully()
        {
            // Arrange
            var repoName = "SLC-AS-testRepo";
            var yamlContent = "invalid: [this is not valid YAML";  // Malformed YAML
            mockFileSystem.Setup(fs => fs.File.Exists(catalogFilePath)).Returns(true); // catalog.yml exists
            mockFileSystem.Setup(fs => fs.File.ReadAllText(catalogFilePath)).Returns(yamlContent);

            // Act
            Func<Task> act = async () => await catalogManager.ProcessCatalogYamlAsync(repoName);

            // Assert
            await act.Should().ThrowAsync<YamlDotNet.Core.YamlException>();  // Should throw YAML parse exception
        }

        [TestMethod]
        public async Task CheckType_ShouldInferTypeFromMultipleTopicsCorrectly()
        {
            // Arrange
            var repoName = "SLC-testRepo";
            var yamlContent = "id: testId\nshort_description: test description\ntags: [fromYamlOne, fromYamlTwo]";
            mockFileSystem.Setup(fs => fs.File.Exists(catalogFilePath)).Returns(true);
            mockFileSystem.Setup(fs => fs.File.ReadAllText(catalogFilePath)).Returns(yamlContent);

            // Simulate topics and types mapping
            mockGitHubService.Setup(s => s.GetRepositoryTopicsAsync()).ReturnsAsync(new List<string> { "GQIDS", "secondTopic" });

            // Act
            await catalogManager.ProcessCatalogYamlAsync(repoName);

            // Assert
            mockFileSystem.Verify(fs => fs.File.WriteAllText(catalogFilePath, It.Is<string>(s => s.Contains("type: gqidatasource"))), Times.Once);
        }

        [TestMethod]
        public async Task ProcessCatalogYamlAsync_ShouldUseCatalogYmlOverManifestWhenBothExist()
        {
            // Arrange
            var repoName = "SLC-AS-testRepo";
            var catalogYamlContent = "id: catalogId\nshort_description: from catalog";
            var manifestYamlContent = "id: manifestId\nshort_description: from manifest";

            mockFileSystem.Setup(fs => fs.File.Exists(catalogFilePath)).Returns(true);  // catalog.yml exists
            mockFileSystem.Setup(fs => fs.File.Exists(manifestFilePath)).Returns(true);  // manifest.yml also exists
            mockFileSystem.Setup(fs => fs.File.ReadAllText(catalogFilePath)).Returns(catalogYamlContent);
            mockFileSystem.Setup(fs => fs.File.ReadAllText(manifestFilePath)).Returns(manifestYamlContent);

            // Act
            await catalogManager.ProcessCatalogYamlAsync(repoName);

            // Assert
            mockFileSystem.Verify(fs => fs.File.WriteAllText(catalogFilePath, It.Is<string>(s => s.Contains("short_description: from catalog"))), Times.Once);
        }
    }
}
