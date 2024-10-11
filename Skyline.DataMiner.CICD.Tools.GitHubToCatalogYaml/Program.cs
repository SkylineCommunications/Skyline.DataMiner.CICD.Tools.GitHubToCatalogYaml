namespace Skyline.DataMiner.CICD.Tools.GitHubToCatalogYaml
{
    using System;
    using System.CommandLine;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;

    using Serilog;

    using Skyline.DataMiner.CICD.FileSystem;

    /// <summary>
    /// Extends or creates a catalog.yml file with data retrieved from GitHub.
    /// </summary>
    public static class Program
    {
        /*
         * Design guidelines for command line tools: https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#design-guidance
         */

        /// <summary>
        /// Code that will be called when running the tool.
        /// </summary>
        /// <param name="args">Extra arguments.</param>
        /// <returns>0 if successful.</returns>
        public static async Task<int> Main(string[] args)
        {
            var isDebug = new Option<bool>(
            name: "--debug",
            description: "Indicates the tool should write out debug logging.")
            {
                IsRequired = false,
            };

            isDebug.SetDefaultValue(false);

            var workspace = new Option<string>(
            name: "--workspace",
            description: "Path to the workspace")
            {
                IsRequired = true
            };

            var githubToken = new Option<string>(
                name: "--github-token",
                description: "Either a PAT or the secrets.GITHUB_TOKEN.")
            {
                IsRequired = true
            };

            var githubRepository = new Option<string>(
            name: "--github-repository",
            description: "The github.repository or (owner/repo).")
            {
                IsRequired = true
            };

            var catalogIdentifier = new Option<string>(
              name: "--catalog-identifier",
              description: "(optional) The catalog identifier. If not provided, then the provided token must be a PAT with access to Actions/Variables.")
            {
                IsRequired = false
            };

            var rootCommand = new RootCommand("Extends or creates a catalog.yml file with data retrieved from GitHub")
            {
                isDebug,
                githubToken,
                githubRepository,
                workspace,
                catalogIdentifier
            };

            rootCommand.SetHandler(Process, isDebug, githubToken, githubRepository, workspace, catalogIdentifier);

            return await rootCommand.InvokeAsync(args);
        }
        private static async Task<int> Process(bool isDebug, string githubToken, string githubRepository, string workspace, string catalogIdentifier)
        {
            try
            {
                // Set up logging
                var logConfig = new LoggerConfiguration().WriteTo.Console();
                logConfig.MinimumLevel.Is(isDebug ? Serilog.Events.LogEventLevel.Debug : Serilog.Events.LogEventLevel.Information);
                var seriLog = logConfig.CreateLogger();

                using var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog(seriLog));
                var logger = loggerFactory.CreateLogger("Skyline.DataMiner.CICD.Tools.GitHubToCatalogYaml");

                IFileSystem fs = FileSystem.Instance;
                try
                {
                    // Set up GitHubService and CatalogManager
                    var httpClient = new HttpClient();
                    var gitHubService = new GitHubService(httpClient, logger, githubToken, githubRepository);
                    var catalogManager = new CatalogManager(fs, logger, gitHubService, workspace);

                    // Call the method to process the catalog.yml file
                    await catalogManager.ProcessCatalogYamlAsync(githubRepository, catalogIdentifier);

                    logger.LogInformation("Process completed successfully.");
                }
                catch (Exception e)
                {
                    logger.LogError($"Exception during Process Run: {e}");
                    return 1;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception on Logger Creation: {e}");
                return 1;
            }

            return 0;
        }
    }
}