namespace CICD.Tools.GitHubToCatalogYamlTests
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using FluentAssertions;

    using Microsoft.Extensions.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Serilog;

    using Skyline.DataMiner.CICD.FileSystem;
    using Skyline.DataMiner.CICD.Tools.GitHubToCatalogYaml;

    using YamlDotNet.Serialization;

    [TestClass]
    public class CatalogManagerTests
    {
        private Mock<IDirectoryIO> mockDirectory;
        private Mock<IFileSystem> mockFileSystem;
        private Microsoft.Extensions.Logging.ILogger logger;
        private Mock<IGitHubService> mockGitHubService;
        private CatalogManager catalogManager;
        private const string workspace = "testWorkspace";
        private const string catalogFilePath = "testWorkspace/catalog.yml";
        private const string manifestFilePath = "testWorkspace/manifest.yml";
        private const string autoGeneratorFilePath = "testWorkspace/.githubtocatalog/auto-generated-catalog.yml";

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
            mockFileSystem.Setup(fs => fs.Path.Combine(workspace, ".githubtocatalog", "auto-generated-catalog.yml")).Returns(autoGeneratorFilePath);

            mockDirectory = new Mock<IDirectoryIO>();
            mockDirectory.Setup(dir => dir.CreateDirectory(It.IsAny<string>()));

            mockFileSystem.Setup(fs => fs.Directory).Returns(mockDirectory.Object);

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
        public async Task ProcessCatalogYamlAsync_ShouldSetSourceCodeUrl_WhenMissing()
        {
            // Arrange
            var repoName = "SkylineCommunications/KPN-AS-OutageReport";
            var yamlContent = "id: testId\nshort_description: test description"; // No SourceCodeUrl in the initial YAML
            var expectedSourceCodeUrl = $"https://github.com/{repoName}";

            mockFileSystem.Setup(fs => fs.File.Exists(catalogFilePath)).Returns(true);
            mockFileSystem.Setup(fs => fs.File.ReadAllText(catalogFilePath)).Returns(yamlContent); // Simulate existing catalog.yml

            // Act
            await catalogManager.ProcessCatalogYamlAsync(repoName);

            // Assert
            mockFileSystem.Verify(fs => fs.File.WriteAllText(catalogFilePath, It.Is<string>(s => s.Contains($"source_code_url: {expectedSourceCodeUrl}"))), Times.Once);
        }


        [DataTestMethod]
        [DataRow("SLC-AS-testRepo", true, true)]
        [DataRow("SLC-AS-testRepo", false, false)]
        [DataRow("SLC-C-testRepo", true, false)]
        public async Task ProcessCatalogYamlAsync_ShouldHandleId_WhenIdIsMissing(string repoName, bool shouldAssignNewId, bool useDefault)
        {
            // Arrange
            var yamlContent = "short_description: test description\ntags: [testTag]";
            mockFileSystem.Setup(fs => fs.File.Exists(catalogFilePath)).Returns(true); // catalog.yml exists
            mockFileSystem.Setup(fs => fs.File.ReadAllText(catalogFilePath)).Returns(yamlContent);

            // Act
            if (useDefault)
            {
                await catalogManager.ProcessCatalogYamlAsync(repoName);
            }
            else
            {
                await catalogManager.ProcessCatalogYamlAsync(repoName, "", shouldAssignNewId);
            }

            // Assert
            mockFileSystem.Verify(fs => fs.File.WriteAllText(catalogFilePath, It.Is<string>(s => shouldAssignNewId
                ? Regex.IsMatch(s, @"(?m)^id: [0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}\r?$", RegexOptions.IgnoreCase)
                : Regex.IsMatch(s, @"(?m)^id: ''\r?$"))), Times.Once);
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
            mockGitHubService.Setup(s => s.GetRepositoryTopicsAsync()).ReturnsAsync(new List<string> { "automationscript", "newTag" });

            // Act
            await catalogManager.ProcessCatalogYamlAsync(repoName);

            // Assert
            mockFileSystem.Verify(fs => fs.File.WriteAllText(catalogFilePath, It.Is<string>(s => Regex.IsMatch(s, @"tags:\s*(?:-\s*\S+\s*)*-\s*newTag", RegexOptions.Multiline))), Times.Once);
        }

        [TestMethod]
        public async Task ProcessCatalogYamlAsync_ShouldFilterTagsThatMatchTypes()
        {
            // Arrange
            var repoName = "SLC-AS-testRepo";
            var yamlContent = "id: testId\nshort_description: test description";
            mockFileSystem.Setup(fs => fs.File.Exists(catalogFilePath)).Returns(true); // catalog.yml exists
            mockFileSystem.Setup(fs => fs.File.ReadAllText(catalogFilePath)).Returns(yamlContent);
            mockGitHubService.Setup(s => s.GetRepositoryTopicsAsync()).ReturnsAsync(new List<string> { "dataminer-automation-script", "newTag","dataminer","otherTag", "dataminer-connector" });

            // Act
            await catalogManager.ProcessCatalogYamlAsync(repoName);

            // Assert
            mockFileSystem.Verify(fs => fs.File.WriteAllText(catalogFilePath, It.Is<string>(s => Regex.IsMatch(s, @"tags:\s*\n\s*-\s*newTag\s*\n\s*-\s*otherTag\s*(\n\s*#.*|\s*)*$", RegexOptions.Multiline))), Times.Once);
        }

        [TestMethod]
        public async Task ProcessCatalogYamlAsync_ShouldFilterTagsThatMatchTypes_NoNameType()
        {
            // Arrange
            var repoName = "SLC-testRepo";
            var yamlContent = "id: testId\nshort_description: test description";
            mockFileSystem.Setup(fs => fs.File.Exists(catalogFilePath)).Returns(true); // catalog.yml exists
            mockFileSystem.Setup(fs => fs.File.ReadAllText(catalogFilePath)).Returns(yamlContent);
            mockGitHubService.Setup(s => s.GetRepositoryTopicsAsync()).ReturnsAsync(new List<string> { "dataminer-automation-script", "newTag", "dataminer", "otherTag", "dataminer-connector" });

            // Act
            await catalogManager.ProcessCatalogYamlAsync(repoName);

            // Assert
            mockFileSystem.Verify(fs => fs.File.WriteAllText(catalogFilePath, It.Is<string>(s => Regex.IsMatch(s, @"tags:\s*\n\s*-\s*newTag\s*\n\s*-\s*otherTag\s*(\n\s*#.*|\s*)*$", RegexOptions.Multiline))), Times.Once);
        }

        [TestMethod]
        public async Task ProcessCatalogYamlAsync_ShouldFilterTagsThatMatchTypes_TypeInYaml()
        {
            // Arrange
            var repoName = "SLC-testRepo";
            var yamlContent = "type: Automation Script\nid: testId\nshort_description: test description";
            mockFileSystem.Setup(fs => fs.File.Exists(catalogFilePath)).Returns(true); // catalog.yml exists
            mockFileSystem.Setup(fs => fs.File.ReadAllText(catalogFilePath)).Returns(yamlContent);
            mockGitHubService.Setup(s => s.GetRepositoryTopicsAsync()).ReturnsAsync(new List<string> { "dataminer-automation-script", "newTag", "dataminer", "otherTag", "dataminer-connector" });

            // Act
            await catalogManager.ProcessCatalogYamlAsync(repoName);

            // Assert
            mockFileSystem.Verify(fs => fs.File.WriteAllText(catalogFilePath, It.Is<string>(s => Regex.IsMatch(s, @"tags:\s*\n\s*-\s*newTag\s*\n\s*-\s*otherTag\s*(\n\s*#.*|\s*)*$", RegexOptions.Multiline))), Times.Once);
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
            // **SC**: `Scripted Connector`
            var repoName = "SLC-SC-testRepo";
            var yamlContent = "id: testId\nshort_description: test description\ntags: [testTag]";
            mockFileSystem.Setup(fs => fs.File.Exists(catalogFilePath)).Returns(true); // catalog.yml exists
            mockFileSystem.Setup(fs => fs.File.ReadAllText(catalogFilePath)).Returns(yamlContent);

            // Act
            await catalogManager.ProcessCatalogYamlAsync(repoName);

            // Assert
            mockFileSystem.Verify(fs => fs.File.WriteAllText(catalogFilePath, It.Is<string>(s => s.Contains("type: Scripted Connector"))), Times.Once);
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
            mockFileSystem.Setup(fs => fs.Directory.CreateDirectory(It.IsAny<string>()));

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
            mockFileSystem.Verify(fs => fs.File.WriteAllText(catalogFilePath, It.Is<string>(s => s.Contains("type: Ad Hoc Data Source"))), Times.Once);
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

        [TestMethod]
        public async Task ProcessCatalogYamlAsync_SaveFileShouldAccessFileSystemInCorrectOrderForBothFiles()
        {
            // Arrange
            var repoName = "SLC-AS-testRepo";
            var expectedCatalogPath = "testWorkspace/catalog.yml";
            var expectedAutoGenPath = "testWorkspace/.githubtocatalog/auto-generated-catalog.yml";
            var manifestYamlContent = "id: manifestId\nshort_description: from manifest";

            // Simulate the YAML serialization
            mockFileSystem.Setup(fs => fs.File.ReadAllText(manifestFilePath)).Returns(manifestYamlContent);
            // Setup mocks for file system interactions
            var mockSequence = new MockSequence();

            // Ensure the operations for 'catalog.yml' happen in sequence
            mockFileSystem.InSequence(mockSequence)
                .Setup(fs => fs.File.DeleteFile(expectedCatalogPath));
            mockFileSystem.InSequence(mockSequence)
                .Setup(fs => fs.Path.GetDirectoryName(expectedCatalogPath))
                .Returns("testDirectory");
            mockFileSystem.InSequence(mockSequence)
                .Setup(fs => fs.Directory.CreateDirectory("testDirectory"));
            mockFileSystem.InSequence(mockSequence)
                .Setup(fs => fs.Directory.TryAllowWritesOnDirectory("testDirectory"))
                .Returns(true);
            mockFileSystem.InSequence(mockSequence)
                .Setup(fs => fs.File.WriteAllText(expectedCatalogPath, It.IsAny<String>()));

            // Ensure the operations for 'auto-generated-catalog.yml' happen in sequence
            mockFileSystem.InSequence(mockSequence)
                .Setup(fs => fs.File.DeleteFile(expectedAutoGenPath));
            mockFileSystem.InSequence(mockSequence)
                .Setup(fs => fs.Path.GetDirectoryName(expectedAutoGenPath))
                .Returns("testAutoGenDirectory");
            mockFileSystem.InSequence(mockSequence)
                .Setup(fs => fs.Directory.CreateDirectory("testAutoGenDirectory"));
            mockFileSystem.InSequence(mockSequence)
                .Setup(fs => fs.Directory.TryAllowWritesOnDirectory("testAutoGenDirectory"))
                .Returns(true);
            mockFileSystem.InSequence(mockSequence)
                .Setup(fs => fs.File.WriteAllText(expectedAutoGenPath, It.IsAny<String>()));

            // Act
            await catalogManager.ProcessCatalogYamlAsync(repoName);

            // Assert
            // Verify that the catalog.yml file system interactions occurred in the correct order
            mockFileSystem.Verify(fs => fs.File.DeleteFile(expectedCatalogPath), Times.Once);
            mockFileSystem.Verify(fs => fs.Path.GetDirectoryName(expectedCatalogPath), Times.Once);
            mockFileSystem.Verify(fs => fs.Directory.CreateDirectory("testDirectory"), Times.Once);
            mockFileSystem.Verify(fs => fs.Directory.TryAllowWritesOnDirectory("testDirectory"), Times.Once);
            mockFileSystem.Verify(fs => fs.File.WriteAllText(expectedCatalogPath, It.IsAny<String>()), Times.Once);

            // Verify that the auto-generated-catalog.yml file system interactions occurred in the correct order
            mockFileSystem.Verify(fs => fs.File.DeleteFile(expectedAutoGenPath), Times.Once);
            mockFileSystem.Verify(fs => fs.Path.GetDirectoryName(expectedAutoGenPath), Times.Once);
            mockFileSystem.Verify(fs => fs.Directory.CreateDirectory("testAutoGenDirectory"), Times.Once);
            mockFileSystem.Verify(fs => fs.Directory.TryAllowWritesOnDirectory("testAutoGenDirectory"), Times.Once);
            mockFileSystem.Verify(fs => fs.File.WriteAllText(expectedAutoGenPath, It.IsAny<String>()), Times.Once);
        }

        [TestMethod]
        public async Task ProcessCatalogYamlAsync_ShouldSeparatePropertyCommentsAndDocumentValueFormats()
        {
            // Arrange
            var repoName = "SLC-AS-testRepo";
            var yamlContent = "type: Automation\n" +
                              "id: existing-id\n" +
                              "title: Existing title\n" +
                              "short_description: Existing description\n" +
                              "source_code_url: https://example.com/source\n" +
                              "documentation_url: https://example.com/docs\n" +
                              "owners:\n" +
                              "  - email: owner@example.com\n" +
                              "    name: Existing owner\n" +
                              "    url: https://example.com/owner\n" +
                              "tags:\n" +
                              "  - Existing tag\n" +
                              "vendor_id: existing-vendor-id\n" +
                              "market_name: Existing market\n" +
                              "element_type: Existing element";
            var generatedYaml = new List<string>();

            mockFileSystem.Setup(fs => fs.File.Exists(catalogFilePath)).Returns(true);
            mockFileSystem.Setup(fs => fs.File.Exists(autoGeneratorFilePath)).Returns(false);
            mockFileSystem.Setup(fs => fs.File.ReadAllText(catalogFilePath)).Returns(yamlContent);
            mockGitHubService.Setup(s => s.GetRepositoryTopicsAsync()).ReturnsAsync(new List<string>());
            mockFileSystem
                .Setup(fs => fs.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((_, contents) => generatedYaml.Add(contents));

            // Act
            await catalogManager.ProcessCatalogYamlAsync(repoName);

            // Assert
            generatedYaml.Should().HaveCount(2);
            var output = generatedYaml[0].Replace("\r\n", "\n");
            var expectedLines = new[]
            {
                "# WARNING! DO NOT CHANGE THIS FILE.",
                "# If you wish to make adjustments based on the `auto-generated-catalog.yml` file, you can do so by creating a `catalog.yml` file in the root of your repository.",
                String.Empty,
                "# [Required]",
                "# Possible values for the Catalog item that can be deployed on a DataMiner System:",
                "#   - Automation: If the Catalog item is a general-purpose DataMiner Automation script.",
                "#   - Ad Hoc Data Source: If the Catalog item is a DataMiner Automation script designed for an ad hoc data source integration.",
                "#   - ChatOps Extension: If the Catalog item is a DataMiner Automation script designed as a ChatOps extension.",
                "#   - Connector: If the Catalog item is a DataMiner XML connector.",
                "#   - Custom Solution: If the Catalog item is a DataMiner Solution.",
                "#   - Data Transformer: Includes a data transformer that enables you to modify data using a GQI data query before making it available to users in low-code apps or dashboards.",
                "#   - Dashboard: If the Catalog item is a DataMiner dashboard.",
                "#   - DevTool: If the Catalog item is a DevTool.",
                "#   - Learning & Sample: If the Catalog item is a sample.",
                "#   - Product Solution: If the Catalog item is a DataMiner Solution that is an out-of-the-box solution for a specific product.",
                "#   - Scripted Connector: If the Catalog item is a DataMiner scripted connector.",
                "#   - Standard Solution: If the Catalog item is a DataMiner Solution that is an out-of-the-box solution for a specific use case or application.",
                "#   - System Health: If the Catalog item is intended to monitor the health of a system.",
                "#   - User-Defined API: If the Catalog item is a DataMiner Automation script designed as a user-defined API.",
                "#   - Visual Overview: If the Catalog item is a Microsoft Visio design.",
                String.Empty,
                "type: Automation",
                String.Empty,
                "# [Required]",
                "# The ID of the Catalog item.",
                "# All registered versions for the same ID are shown together in the Catalog.",
                "# This ID can not be changed.",
                "# If the ID is not filled in, the registration will fail with HTTP status code 500.",
                "# If the ID is filled in but does not exist yet, a new Catalog item will be registered with this ID.",
                "# If the ID is filled in but does exist, properties of the item will be overwritten.",
                "#   Must be a valid GUID.",
                "id: existing-id",
                String.Empty,
                "# [Required]",
                "# The human-friendly name of the Catalog item.",
                "# Can be changed at any time.",
                "#   Max length: 100 characters.",
                "#   Cannot contain newlines.",
                "#   Cannot contain leading or trailing whitespace characters.",
                "title: Existing title",
                String.Empty,
                "# [Optional]",
                "# General information about the Catalog item.",
                "#   Max length: 100,000 characters",
                "# Currently not shown in the Catalog UI but will be supported in the near future.",
                "short_description: Existing description",
                String.Empty,
                "# [Optional]",
                "# A valid URL that points to the source code.",
                "#   A valid URL",
                "#   Max length: 2048 characters",
                "source_code_url: https://example.com/source",
                String.Empty,
                "# [Optional]",
                "# A valid URL that points to documentation.",
                "#   A valid URL",
                "#   Max length: 2048 characters",
                "# Currently not shown in the Catalog UI but will be supported in the near future.",
                "documentation_url: https://example.com/docs",
                String.Empty,
                "# [Optional]",
                "# People who are responsible for this Catalog item. Might be developers, but this is not required.",
                "#   The name is required; max 256 characters.",
                "#   The email and url are optional, and should be in valid email/URL formats.",
                "#   Example:",
                "#   owners:",
                "#     - name: 'Owner 1 name'",
                "#     - name: 'Owner 2 name'",
                "owners:",
                "  - email: owner@example.com",
                "    name: Existing owner",
                "    url: https://example.com/owner",
                String.Empty,
                "# [Optional]",
                "# Tags that allow you to categorize your Catalog items.",
                "#   Max number of tags: 5",
                "#   Max length: 50 characters.",
                "#   Cannot contain newlines.",
                "#   Example:",
                "#   tags:",
                "#     - MyTag1",
                "#     - MyTag2",
                "tags:",
                "  - Existing tag",
                String.Empty,
                "# [Optional]",
                "# The ID of the vendor.",
                "# This vendor ID can be retrieved using the public Catalog API.",
                "# If the vendor ID does not exist, the registration or update will fail with HTTP status code 400.",
                "# If the vendor ID is '00000000-0000-0000-0000-000000000000', the vendor will be unset.",
                "# If the vendor ID is not provided during the initial registration, the vendor will be unset.",
                "# If the vendor ID is not provided during an update, the vendor will be unchanged and keep the previously set value.",
                "#   Must be a valid GUID.",
                "vendor_id: existing-vendor-id",
                String.Empty,
                "# [Optional]",
                "# The name of the market the Catalog item belongs to.",
                "market_name: Existing market",
                String.Empty,
                "# [Optional]",
                "# The type of the element.",
                "# This can only be applied to Catalog items with the type Connector.",
                "element_type: Existing element",
                String.Empty,
            };

            output.Split('\n').Should().Equal(expectedLines);
            generatedYaml[1].Replace("\r\n", "\n").Should().Be(output);
        }
    }
}
