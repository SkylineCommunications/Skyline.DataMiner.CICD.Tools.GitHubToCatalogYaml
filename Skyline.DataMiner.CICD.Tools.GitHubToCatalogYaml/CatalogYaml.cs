﻿namespace Skyline.DataMiner.CICD.Tools.GitHubToCatalogYaml
{
    using System.Collections.Generic;
    using YamlDotNet.Serialization;

    /// <summary>
    /// Represents the metadata of a catalog entry in YAML format.
    /// </summary>
    public class CatalogYaml
    {
        private const string documentationUrlComment = "[Optional]\r\n" +
                                                       "A valid URL that points to documentation.\r\n" +
                                                       "  A valid URL\r\n" +
                                                       "  Max length: 2048 characters\r\n" +
                                                       "Currently not shown in the Catalog UI but will be supported in the near future.";

        private const string idComment = "[Required]\r\n" +
                                         "The ID of the Catalog item.\r\n" +
                                         "All registered versions for the same ID are shown together in the Catalog.\r\n" +
                                         "This ID can not be changed.\r\n" +
                                         "If the ID is not filled in, the registration will fail with HTTP status code 500.\r\n" +
                                         "If the ID is filled in but does not exist yet, a new Catalog item will be registered with this ID.\r\n" +
                                         "If the ID is filled in but does exist, properties of the item will be overwritten.\r\n" +
                                         "  Must be a valid GUID.";

        private const string ownersComment = "[Optional]\r\n" +
                                             "People who are responsible for this Catalog item. Might be developers, but this is not required.\r\n" +
                                             "Format: 'name <email> (url)'\r\n" +
                                             "  The name is required; max 256 characters.\r\n" +
                                             "  The email and url are optional, and should be in valid email/URL formats.";

        private const string shortDescriptionComment = "[Optional]\r\n" +
                                                       "General information about the Catalog item.\r\n" +
                                                       "  Max length: 100,000 characters\r\n" +
                                                       "Currently not shown in the Catalog UI but will be supported in the near future.";

        private const string sourceCodeUrlComment = "[Optional]\r\n" +
                                                    "A valid URL that points to the source code.\r\n" +
                                                    "  A valid URL\r\n" +
                                                    "  Max length: 2048 characters";

        private const string tagsComment = "[Optional]\r\n" +
                                           "Tags that allow you to categorize your Catalog items.\r\n" +
                                           "  Max number of tags: 5\r\n" +
                                           "  Max length: 50 characters.\r\n" +
                                           "  Cannot contain newlines.\r\n" +
                                           "  Cannot contain leading or trailing whitespace characters.";

        private const string titleComment = "[Required]\r\n" +
                                            "The human-friendly name of the Catalog item.\r\n" +
                                            "Can be changed at any time.\r\n" +
                                            "  Max length: 100 characters.\r\n" +
                                            "  Cannot contain newlines.\r\n" +
                                            "  Cannot contain leading or trailing whitespace characters.";

        private const string typeComment = "WARNING! DO NOT CHANGE THIS FILE.\r\n" +
                                           "If you wish to make adjustments based on the `auto-generated-catalog.yml` file, you can do so by creating a `catalog.yml` file in the root of your repository.\r\n\r\n" +
                                           "[Required]\r\n" +
                                           "Possible values for the Catalog item that can be deployed on a DataMiner System:\r\n" +
                                           "  - automationscript: If the Catalog item is a general-purpose DataMiner Automation script.\r\n" +
                                           "  - lifecycleserviceorchestration: If the Catalog item is a DataMiner Automation script designed to manage the life cycle of a service.\r\n" +
                                           "  - profileloadscript: If the Catalog item is a DataMiner Automation script designed to load a standard DataMiner profile.\r\n" +
                                           "  - userdefinedapi: If the Catalog item is a DataMiner Automation script designed as a user-defined API.\r\n" +
                                           "  - adhocdatasource: If the Catalog item is a DataMiner Automation script designed for an ad hoc data source integration.\r\n" +
                                           "  - chatopsextension: If the Catalog item is a DataMiner Automation script designed as a ChatOps extension.\r\n" +
                                           "  - connector: If the Catalog item is a DataMiner XML connector.\r\n" +
                                           "  - slamodel: If the Catalog item is a DataMiner XML connector designed as DataMiner Service Level Agreement model.\r\n" +
                                           "  - enhancedservicemodel: If the Catalog item is a DataMiner XML connector designed as DataMiner enhanced service model.\r\n" +
                                           "  - visio: If the Catalog item is a Microsoft Visio design.\r\n" +
                                           "  - solution: If the Catalog item is a DataMiner Solution.\r\n" +
                                           "  - testingsolution: If the Catalog item is a DataMiner Solution designed for automated testing and validation.\r\n" +
                                           "  - samplesolution: If the Catalog item is a DataMiner Solution used for training and education.\r\n" +
                                           "  - standardsolution: If the Catalog item is a DataMiner Solution that is an out-of-the-box solution for a specific use case or application.\r\n" +
                                           "  - dashboard: If the Catalog item is a DataMiner dashboard.\r\n" +
                                           "  - lowcodeapp: If the Catalog item is a DataMiner low-code app.\r\n" +
                                           "  - datatransformer: If the Catalog item is a Data Transformer.\r\n" +
                                           "  - dataquery: If the Catalog item is a GQI data query.\r\n" +
                                           "  - functiondefinition: If the Catalog item is a DataMiner function definition.\r\n" +
                                           "  - scriptedconnector: If the Catalog item is a DataMiner scripted connector.\r\n" +
                                           "  - bestpracticesanalyzer: If the Catalog item is a DataMiner Best Practices Analysis file.";

        /// <summary>
        /// Initializes a new instance of the <see cref="CatalogYaml"/> class.
        /// </summary>
        public CatalogYaml()
        {
            Owners = new List<CatalogYamlOwner>();
            Tags = new List<string>();
        }

        /// <summary>
        /// Gets or sets the URL to the documentation related to the catalog entry.
        /// </summary>
        /// <value>A string representing the documentation URL.</value>
        [YamlMember(Description = documentationUrlComment, Order = 5)]
        public string DocumentationUrl { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for the catalog entry.
        /// </summary>
        /// <value>A string representing the unique ID of the catalog entry.</value>
        [YamlMember(Description = idComment, Order = 1)]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the owners of the catalog entry.
        /// </summary>
        /// <value>A list of <see cref="CatalogYamlOwner"/> objects representing the owners.</value>
        [YamlMember(Description = ownersComment, Order = 6)]
        public List<CatalogYamlOwner> Owners { get; set; }

        /// <summary>
        /// Gets or sets the short description of the catalog entry.
        /// </summary>
        /// <value>A string representing the short description of the entry.</value>
        [YamlMember(Description = shortDescriptionComment, Order = 3)]
        public string ShortDescription { get; set; }

        /// <summary>
        /// Gets or sets the URL to the source code related to the catalog entry.
        /// </summary>
        /// <value>A string representing the source code URL.</value>
        [YamlMember(Description = sourceCodeUrlComment, Order = 4)]
        public string SourceCodeUrl { get; set; }

        /// <summary>
        /// Gets or sets the tags associated with the catalog entry.
        /// </summary>
        /// <value>A list of strings representing the tags.</value>
        [YamlMember(Description = tagsComment, Order = 7)]
        public List<string> Tags { get; set; }

        /// <summary>
        /// Gets or sets the title of the catalog entry.
        /// </summary>
        /// <value>A string representing the title of the entry.</value>
        [YamlMember(Description = titleComment, Order = 2)]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the type of the catalog entry.
        /// </summary>
        /// <value>A string representing the type of the entry.</value>
        [YamlMember(Description = typeComment, Order = 0)]
        public string Type { get; set; }
    }

    /// <summary>
    /// Represents an owner of the catalog entry in the YAML format.
    /// </summary>
    public class CatalogYamlOwner
    {
        /// <summary>
        /// Gets or sets the email of the owner.
        /// </summary>
        /// <value>A string representing the owner's email address.</value>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the name of the owner.
        /// </summary>
        /// <value>A string representing the owner's name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the URL of the owner's profile or webpage.
        /// </summary>
        /// <value>A string representing the URL of the owner.</value>
        public string Url { get; set; }
    }
}