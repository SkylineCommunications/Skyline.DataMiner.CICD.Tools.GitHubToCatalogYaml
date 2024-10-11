namespace Skyline.DataMiner.CICD.Tools.GitHubToCatalogYaml
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides services for interacting with GitHub repositories, including retrieving and setting repository variables, description, and topics.
    /// </summary>
    internal interface IGitHubService
    {
        /// <summary>
        /// Retrieves the repository description from the GitHub repository.
        /// </summary>
        /// <returns>A task representing the asynchronous operation, containing the repository description as a string, or null if the retrieval fails.</returns>
        Task<string> GetRepositoryDescriptionAsync();

        /// <summary>
        /// Retrieves the topics (tags) from the GitHub repository's ABOUT section.
        /// </summary>
        /// <returns>A task representing the asynchronous operation, containing a list of repository topics (tags), or null if the retrieval fails.</returns>
        Task<List<string>> GetRepositoryTopicsAsync();
    }
}