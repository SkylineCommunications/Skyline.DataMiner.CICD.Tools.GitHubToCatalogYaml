namespace Skyline.DataMiner.CICD.Tools.GitHubToCatalogYaml
{
    using System;
    using System.Linq;

    /// <summary>
    /// Represents a cleaned-up version of a GitHub repository name.
    /// It extracts and formats the repository name according to specific guidelines, and optionally infers the item type from a predefined mapping.
    /// </summary>
    internal class CleanTitle
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CleanTitle"/> class.
        /// Processes the GitHub repository name to remove the owner prefix, 
        /// extract a meaningful title, and optionally infer the item type based on naming conventions.
        /// </summary>
        /// <param name="githubRepoName">The full GitHub repository name in the format 'owner/repo'.</param>
        public CleanTitle(string githubRepoName)
        {
            // SkylineCommunications/SLC-AS-MediaOps-Apps
            // MediaOps-Apps

            bool hasGuideLineFormat = false;
            var ownerSeparatorIndex = githubRepoName.IndexOf('/');
            if (ownerSeparatorIndex != -1)
            {
                githubRepoName = githubRepoName.Substring(ownerSeparatorIndex + 1);
            }

            var splitDash = githubRepoName.Split('-');
            if (splitDash.Length > 2 && splitDash[0].Length < 10)
            {
                var foundItemType = Constants.ArtifactTypeMap.FirstOrDefault(p => p.IsMatch(splitDash[1]));

                if (foundItemType != null)
                {
                    FoundItemType = foundItemType.CatalogName;
                    hasGuideLineFormat = true;
                }
                else
                {
                    if (splitDash[1].ToUpper() == splitDash[1] && splitDash[1].Length < 8)
                    {
                        hasGuideLineFormat = true;
                    }
                }
            }

            if (hasGuideLineFormat)
            {
                Value = String.Join("-", splitDash.Skip(2)).Trim().Replace("_", " ");
            }
            else
            {
                Value = githubRepoName.Trim().Replace("_", " ");
            }
        }

        /// <summary>
        /// Gets the inferred item type from the repository name, if available.
        /// This value is determined based on a mapping of known artifact types in the repository name.
        /// </summary>
        public string FoundItemType { get; }

        /// <summary>
        /// Gets the cleaned-up version of the repository name.
        /// The name is formatted by removing underscores and any owner prefixes, and applying specific guidelines if the name follows the expected format.
        /// </summary>
        public string Value { get; }
    }
}