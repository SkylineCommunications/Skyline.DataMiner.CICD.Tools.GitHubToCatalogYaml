﻿namespace Skyline.DataMiner.CICD.Tools.GitHubToCatalogYaml
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;

	using Microsoft.Extensions.Logging;

	using Skyline.DataMiner.CICD.FileSystem;

	using YamlDotNet.Serialization;
	using YamlDotNet.Serialization.NamingConventions;

	public class CatalogManager
	{
		private readonly IFileSystem fs;
		private readonly ILogger logger;
		private readonly IGitHubService service;
		private readonly string workspace;

		/// <summary>
		/// Initializes a new instance of the <see cref="CatalogManager"/> class.
		/// </summary>
		/// <param name="fs">The file system abstraction to handle file operations.</param>
		/// <param name="logger">The logger instance for logging messages.</param>
		/// <param name="service">The GitHub service for interacting with repository data.</param>
		/// <param name="workspace">The workspace directory where the catalog and manifest files are located.</param>
		public CatalogManager(IFileSystem fs, ILogger logger, IGitHubService service, string workspace)
		{
			this.fs = fs;
			this.logger = logger;
			this.service = service;
			this.workspace = workspace;
		}

		/// <summary>
		/// Processes the catalog YAML file for the specified GitHub repository. It checks and updates various fields such as ID, description, tags, title, and type.
		/// If no catalog file is found, it attempts to create one from the manifest file.
		/// </summary>
		/// <param name="repoName">The name of the GitHub repository to process.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="repoName"/> is null or empty.</exception>
		/// <returns>A task that represents the asynchronous operation.</returns>
		public async Task ProcessCatalogYamlAsync(string repoName)
		{
			if (String.IsNullOrWhiteSpace(repoName)) throw new ArgumentNullException(nameof(repoName));

			logger.LogInformation("Extracting Information from GitHub...");

			string filePath;
			CatalogYaml catalogYaml;
			ISerializer serializer;
			var deserializer = new DeserializerBuilder()
				.WithNamingConvention(UnderscoredNamingConvention.Instance)
				.Build();

			serializer = new SerializerBuilder()
				.WithNamingConvention(UnderscoredNamingConvention.Instance)
				.Build();


			catalogYaml = CreateCatalogYaml(deserializer, out filePath);

			await CheckId(catalogYaml);

			await CheckShortDescription(catalogYaml);

			await CheckTags(catalogYaml);

			CleanTitle parsedRepoName = CheckTitle(repoName, catalogYaml);

			// Perform this after CheckTags
			CheckType(catalogYaml, parsedRepoName);

			string outputPath;
			if (filePath == null)
			{
				outputPath = fs.Path.Combine(workspace, "catalog.yml");
			}
			else
			{
				outputPath = filePath;
			}

			SaveFile(catalogYaml, serializer, outputPath);
			logger.LogInformation($"Finished. Updated or Created file with path: {outputPath}");
		}

		/// <summary>
		/// Checks if the catalog ID exists in the YAML file, and if not, it retrieves or generates a new catalog ID from GitHub and updates the file accordingly.
		/// </summary>
		/// <param name="catalogYaml">The catalog YAML object to check and update.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		private async Task CheckId(CatalogYaml catalogYaml)
		{
			logger.LogDebug($"Checking if ID exists, otherwise retrieve or create it...");
			if (string.IsNullOrWhiteSpace(catalogYaml.Id))
			{
				var catalogId = await service.GetCatalogIdentifierAsync();
				if (string.IsNullOrWhiteSpace(catalogId))
				{
					logger.LogDebug($"Creating new ID...");
					catalogId = Guid.NewGuid().ToString();
					await service.CreateCatalogIdentifierAsync(catalogId);
					logger.LogDebug($"New Catalog ID created and stored in GitHub Variable.");
				}

				catalogYaml.Id = catalogId;
			}
		}

		/// <summary>
		/// Checks if the short description exists in the YAML file. If it does not, it retrieves the repository description from GitHub and updates the YAML file.
		/// If no description is found, it defaults to "No description available."
		/// </summary>
		/// <param name="catalogYaml">The catalog YAML object to check and update.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		private async Task CheckShortDescription(CatalogYaml catalogYaml)
		{
			logger.LogDebug($"Checking if Short_description exists, otherwise retrieve the GitHub repository description...");
			if (string.IsNullOrWhiteSpace(catalogYaml.Short_description))
			{
				var description = await service.GetRepositoryDescriptionAsync();

				if (description == null)
				{
					description = "No description available";
				}

				catalogYaml.Short_description = description;
				logger.LogDebug($"Description applied: {description}");
			}
		}

		/// <summary>
		/// Checks if the tags exist in the YAML file. If they are missing, it retrieves repository topics from GitHub and adds them to the YAML file, ensuring that the tags are distinct.
		/// </summary>
		/// <param name="catalogYaml">The catalog YAML object to check and update.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		private async Task CheckTags(CatalogYaml catalogYaml)
		{
			logger.LogDebug($"Checking if Tags exist, extending with retrieved GitHub repository topics...");
			if (catalogYaml.Tags == null || !catalogYaml.Tags.Any())
			{
				catalogYaml.Tags = new List<string>();
			}

			var topics = await service.GetRepositoryTopicsAsync();
			if (topics != null && topics.Any())
			{
				catalogYaml.Tags.AddRange(topics);
				catalogYaml.Tags = catalogYaml.Tags.Distinct().ToList();
				logger.LogDebug($"Distinct GitHub Topics found and applied.");
			}
		}

		/// <summary>
		/// Checks if the title exists in the YAML file. If it does not, a cleaned version of the repository name is used as the title.
		/// </summary>
		/// <param name="repoName">The repository name to generate a title if needed.</param>
		/// <param name="catalogYaml">The catalog YAML object to check and update.</param>
		/// <returns>A <see cref="CleanTitle"/> object representing the cleaned repository name.</returns>
		private CleanTitle CheckTitle(string repoName, CatalogYaml catalogYaml)
		{
			logger.LogDebug($"Checking if Title exists, otherwise use a cleaned-up version of the repository name...");
			var parsedRepoName = new CleanTitle(repoName);
			if (string.IsNullOrWhiteSpace(catalogYaml.Title))
			{
				catalogYaml.Title = parsedRepoName.Value;
				logger.LogDebug($"GitHub Repository Name cleaned and applied {catalogYaml.Title}.");
			}

			return parsedRepoName;
		}

		/// <summary>
		/// Checks if the type exists in the YAML file. If not, it attempts to infer the type from the repository name or tags.
		/// If it cannot identify the type, it throws an <see cref="InvalidOperationException"/>.
		/// </summary>
		/// <param name="catalogYaml">The catalog YAML object to check and update.</param>
		/// <param name="parsedRepoName">The parsed repository name to use for type inference.</param>
		/// <exception cref="InvalidOperationException">Thrown when the type cannot be identified from the repository name or tags.</exception>
		private void CheckType(CatalogYaml catalogYaml, CleanTitle parsedRepoName)
		{
			logger.LogDebug($"Checking if Type exists, otherwise infer it from repository name or topics...");
			if (string.IsNullOrWhiteSpace(catalogYaml.Type))
			{
				// First, check repository name for item type acronym
				if (parsedRepoName.FoundItemType != null)
				{
					catalogYaml.Type = parsedRepoName.FoundItemType;
					logger.LogDebug($"Item Type could be inferred from repository name {catalogYaml.Type}.");
				}

				// If we still don't have a type, check topics for matching ArtifactContentType
				if (string.IsNullOrWhiteSpace(catalogYaml.Type) && catalogYaml.Tags != null)
				{
					foreach (var topic in catalogYaml.Tags)
					{
						var inferredType = InferArtifactContentType(topic);
						if (!string.IsNullOrWhiteSpace(inferredType))
						{
							catalogYaml.Type = inferredType;
							logger.LogDebug($"Item Type could be inferred from repository topics {catalogYaml.Type}.");
							break;
						}
					}
				}

				if (string.IsNullOrWhiteSpace(catalogYaml.Type))
				{
					// On request of ECS
					throw new InvalidOperationException("Could not identify Type from GitHub. Please specify the type either through naming or topic guidelines.");
				}
			}
		}

		/// <summary>
		/// Creates a new catalog YAML object by reading the existing catalog.yml or manifest.yml file in the workspace.
		/// If no file is found, it creates an empty catalog YAML object.
		/// </summary>
		/// <param name="deserializer">The YAML deserializer to parse the existing YAML file.</param>
		/// <param name="foundFile">The path to the found YAML file, or null if no file is found.</param>
		/// <returns>The deserialized <see cref="CatalogYaml"/> object, or a new object if no file is found.</returns>
		private CatalogYaml CreateCatalogYaml(IDeserializer deserializer, out string foundFile)
		{
			logger.LogDebug("Checking if user has provided a catalog.yml or manifest.yml file within the workspace root.");
			foundFile = null;

			string filePath = fs.Path.Combine(workspace, "catalog.yml");
			if (!fs.File.Exists(filePath))
			{
				filePath = fs.Path.Combine(workspace, "manifest.yml");
				if (!fs.File.Exists(filePath))
				{
					logger.LogDebug($"No existing configuration file found.");
				}
				else
				{
					foundFile = filePath;
					logger.LogDebug($"Found file at: {filePath}");
				}
			}
			else
			{
				foundFile = filePath;
				logger.LogDebug($"Found file at: {filePath}");
			}

			CatalogYaml catalogYaml = null;

			if (!String.IsNullOrWhiteSpace(foundFile))
			{
				var yamlContent = fs.File.ReadAllText(filePath);
				catalogYaml = deserializer.Deserialize<CatalogYaml>(yamlContent);
				logger.LogDebug($"Existing Configuration File Parsed.");
			}

			if (catalogYaml == null)
			{
				catalogYaml = new CatalogYaml();
			}

			return catalogYaml;
		}

		/// <summary>
		/// Infers the artifact content type based on the provided keyword. It checks if the keyword is in the artifact type map and returns the corresponding content type.
		/// </summary>
		/// <param name="keyword">The keyword to infer the artifact content type from.</param>
		/// <returns>The inferred content type, or an empty string if the keyword is not recognized.</returns>
		private string InferArtifactContentType(string keyword)
		{
			// Check if the keyword exists in the dictionary
			if (!Constants.ArtifactTypeMap.TryGetValue(keyword.ToUpper(), out var contentType))
			{
				foreach (var typeName in Constants.ArtifactTypeMap.Values)
				{
					if (typeName.Equals(keyword, StringComparison.OrdinalIgnoreCase))
					{
						contentType = typeName;
					}
				}
			}

			return contentType ?? string.Empty;
		}

		/// <summary>
		/// Serializes the catalog YAML object and saves it to the specified output path.
		/// </summary>
		/// <param name="catalogYaml">The catalog YAML object to serialize.</param>
		/// <param name="serializer">The YAML serializer used to convert the object to YAML format.</param>
		/// <param name="outputPath">The file path where the updated YAML file will be saved.</param>
		private void SaveFile(CatalogYaml catalogYaml, ISerializer serializer, string outputPath)
		{
			logger.LogDebug($"Serializing and saving the updated catalog.yml file with path: {outputPath}.");

			var updatedYaml = serializer.Serialize(catalogYaml);
			fs.File.WriteAllText(outputPath, updatedYaml);
		}
	}
}