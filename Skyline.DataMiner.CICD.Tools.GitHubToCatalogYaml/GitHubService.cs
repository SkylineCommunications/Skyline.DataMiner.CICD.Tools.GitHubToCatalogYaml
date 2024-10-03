namespace Skyline.DataMiner.CICD.Tools.GitHubToCatalogYaml
{
	using System.Collections.Generic;
	using System.Net.Http;
	using System.Text.Json;
	using System.Threading.Tasks;

	using Microsoft.Extensions.Logging;

	/// <summary>
	/// Provides services for interacting with GitHub repositories, including retrieving and setting repository variables, description, and topics.
	/// </summary>
	public class GitHubService : IGitHubService
	{
		private readonly HttpClient _httpClient;
		private readonly ILogger _logger;
		private readonly string githubRoot;
		private readonly string key;

		/// <summary>
		/// Initializes a new instance of the <see cref="GitHubService"/> class.
		/// </summary>
		/// <param name="httpClient">The HttpClient instance used for making requests to GitHub's API.</param>
		/// <param name="logger">The logger instance for logging messages.</param>
		/// <param name="key">The GitHub API token used for authorization.</param>
		/// <param name="githubRepository">The GitHub repository in the format 'owner/repo'.</param>
		public GitHubService(HttpClient httpClient, ILogger logger, string key, string githubRepository)
		{
			this.key = key;
			_logger = logger;
			_httpClient = httpClient;
			githubRoot = $"https://api.github.com/repos/{githubRepository}";
		}

		/// <summary>
		/// Creates a GitHub repository variable named 'catalogIdentifier'.
		/// </summary>
		/// <param name="catalogIdentifier">The catalog identifier to store as a repository variable.</param>
		/// <returns>A task representing the asynchronous operation, containing a boolean indicating whether the operation was successful.</returns>
		public async Task<bool> CreateCatalogIdentifierAsync(string catalogIdentifier)
		{
			var requestUrl = $"{githubRoot}/actions/variables";
			var content = new StringContent(JsonSerializer.Serialize(new
			{
				name = "catalogIdentifier",
				value = catalogIdentifier
			}), System.Text.Encoding.UTF8, "application/json");

			var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
			{
				Content = content
			};
			request.Headers.Add("Authorization", $"Bearer {key}");
			request.Headers.Add("User-Agent", "GitHubToCatalogYaml");

			var response = await _httpClient.SendAsync(request);
			if (response.IsSuccessStatusCode)
			{
				_logger.LogInformation("Successfully created catalogIdentifier in GitHub repository.");
				return true;
			}

			_logger.LogError($"Failed to create catalogIdentifier in GitHub repository: {response.StatusCode}");
			return false;
		}

		/// <summary>
		/// Retrieves the 'catalogIdentifier' variable from the GitHub repository.
		/// </summary>
		/// <returns>A task representing the asynchronous operation, containing the catalog identifier as a string, or null if the retrieval fails.</returns>
		public async Task<string> GetCatalogIdentifierAsync()
		{
			var requestUrl = $"{githubRoot}/actions/variables/catalogIdentifier";
			var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
			request.Headers.Add("Authorization", $"Bearer {key}");
			request.Headers.Add("User-Agent", "GitHubToCatalogYaml");

			var response = await _httpClient.SendAsync(request);
			if (response.IsSuccessStatusCode)
			{
				var jsonResponse = await response.Content.ReadAsStringAsync();
				var jsonDocument = JsonDocument.Parse(jsonResponse);
				if (jsonDocument.RootElement.TryGetProperty("value", out var catalogIdentifier))
				{
					return catalogIdentifier.GetString();
				}
			}

			_logger.LogError($"Failed to retrieve catalogIdentifier from GitHub: {response.StatusCode}");
			return null;
		}

		/// <summary>
		/// Deletes a GitHub repository variable named 'catalogIdentifier'.
		/// </summary>
		/// <returns>A task representing the asynchronous operation, containing a boolean indicating whether the operation was successful.</returns>
		public async Task<bool> DeleteCatalogIdentifierAsync()
		{
			var requestUrl = $"{githubRoot}/actions/variables/catalogIdentifier";

			var request = new HttpRequestMessage(HttpMethod.Delete, requestUrl);
			request.Headers.Add("Authorization", $"Bearer {key}");
			request.Headers.Add("User-Agent", "GitHubToCatalogYaml");

			var response = await _httpClient.SendAsync(request);
			if (response.IsSuccessStatusCode)
			{
				_logger.LogInformation("Successfully deleted catalogIdentifier in GitHub repository.");
				return true;
			}

			_logger.LogError($"Failed to delete catalogIdentifier in GitHub repository: {response.StatusCode}");
			return false;
		}


		/// <summary>
		/// Retrieves the repository description from the GitHub repository.
		/// </summary>
		/// <returns>A task representing the asynchronous operation, containing the repository description as a string, or null if the retrieval fails.</returns>
		public async Task<string> GetRepositoryDescriptionAsync()
		{
			var requestUrl = $"{githubRoot}";  // URL should already be in the format 'https://api.github.com/repos/{owner}/{repo}/'
			var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
			request.Headers.Add("Authorization", $"Bearer {key}");
			request.Headers.Add("User-Agent", "GitHubToCatalogYaml");

			var response = await _httpClient.SendAsync(request);
			if (response.IsSuccessStatusCode)
			{
				var jsonResponse = await response.Content.ReadAsStringAsync();
				var jsonDocument = JsonDocument.Parse(jsonResponse);
				if (jsonDocument.RootElement.TryGetProperty("description", out var description))
				{
					return description.GetString();
				}
				else
				{
					_logger.LogWarning("Repository description not found in the response.");
				}
			}
			else
			{
				_logger.LogError($"Failed to retrieve repository description: {response.StatusCode} - {response.ReasonPhrase}");
			}

			return null;
		}


		/// <summary>
		/// Retrieves the topics (tags) from the GitHub repository's ABOUT section.
		/// </summary>
		/// <returns>A task representing the asynchronous operation, containing a list of repository topics (tags), or null if the retrieval fails.</returns>
		public async Task<List<string>> GetRepositoryTopicsAsync()
		{
			var requestUrl = $"{githubRoot}/topics";
			var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
			request.Headers.Add("Authorization", $"Bearer {key}");
			request.Headers.Add("Accept", "application/vnd.github.mercy-preview+json");
			request.Headers.Add("User-Agent", "GitHubToCatalogYaml");

			var response = await _httpClient.SendAsync(request);
			if (response.IsSuccessStatusCode)
			{
				var jsonResponse = await response.Content.ReadAsStringAsync();
				var jsonDocument = JsonDocument.Parse(jsonResponse);
				if (jsonDocument.RootElement.TryGetProperty("names", out var topics))
				{
					var topicList = new List<string>();
					foreach (var topic in topics.EnumerateArray())
					{
						topicList.Add(topic.GetString());
					}
					return topicList;
				}
			}

			_logger.LogError($"Failed to retrieve repository topics: {response.StatusCode}");
			return null;
		}
	}
}