namespace CICD.Tools.GitHubToCatalogYamlTests
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using FluentAssertions;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Serilog;

    using Skyline.DataMiner.CICD.Tools.GitHubToCatalogYaml;

    /// <summary>
    /// These tests require setting up a secret to run and will perform actual HTTP calls to GitHub.
    /// Make sure you provide a valid GitHub API key and repository for these tests to succeed. 
    /// 
    /// Right-click on the project in Visual Studio and select "Manage User Secrets", then add the following JSON structure to store your secrets:
    /// 
    /// <code>
    /// {
    ///   "GitHubToken": "&lt;YOUR_GITHUB_TOKEN&gt;",
    ///   "GitHubRepository": "&lt;OWNER/REPOSITORY&gt;"
    /// }
    /// </code>
    /// </summary>
    [TestClass, Ignore]
    public class GitHubServiceIntegrationTests
    {
        private GitHubService _service;
        private string _githubToken;
        private string _githubRepository;

        [TestInitialize]
        public void Setup()
        {
            // Use ConfigurationBuilder to read the secrets from User Secrets
            var config = new ConfigurationBuilder()
                .AddUserSecrets<GitHubServiceIntegrationTests>()  // Load secrets for this class's assembly
                .AddEnvironmentVariables()
                .Build();

            // Retrieve the GitHub token and repository from the secrets
            _githubToken = config["GitHubToken"];
            _githubRepository = config["GitHubRepository"];

            if (String.IsNullOrEmpty(_githubToken) || String.IsNullOrEmpty(_githubRepository))
            {
                throw new InvalidOperationException("GitHubToken and GitHubRepository must be provided in User Secrets.");
            }

            var httpClient = new HttpClient();

            // Configure Serilog for logging
            var loggerConfig = new LoggerConfiguration().WriteTo.Console();
            var serilogLogger = loggerConfig.CreateLogger();
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog(serilogLogger));
            var logger = loggerFactory.CreateLogger("GitHubService");

            // Create an instance of the GitHubService
            _service = new GitHubService(httpClient, logger, _githubToken, _githubRepository);
        }

        /// <summary>
        /// Integration test for retrieving the repository description from the GitHub repository.
        /// </summary>
        [TestMethod]
        public async Task GetRepositoryDescriptionAsyncTest()
        {
            // Act
            var description = await _service.GetRepositoryDescriptionAsync();

            // Assert
            Assert.IsNotNull(description, "Failed to retrieve the repository description from the GitHub repository.");
            description.Should().Be("This repository is used by integration tests for the github to yaml dotnet tool.");
        }

        /// <summary>
        /// Integration test for retrieving the topics (tags) from the GitHub repository's ABOUT section.
        /// </summary>
        [TestMethod]
        public async Task GetRepositoryTopicsAsyncTest()
        {
            // Act
            var topics = await _service.GetRepositoryTopicsAsync();

            List<string> expected = ["blablabla", "testing", "automationscript"];

            // Assert
            Assert.IsNotNull(topics, "Failed to retrieve the repository topics from the GitHub repository.");
            Assert.IsTrue(topics.Count > 0, "The repository should have at least one topic.");
            topics.Should().BeEquivalentTo(expected);
        }
    }
}
