namespace Skyline.DataMiner.CICD.Tools.GitHubToCatalogYaml
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal static class Constants
    {
        public static readonly List<ArtifactType> ArtifactTypeMap = new List<ArtifactType>()
        {
            { new ArtifactType(new[] { "AS" }, "Automation", "Automation Script") },
            { new ArtifactType(new[] { "C" }, "Connector", "Connector")},
            { new ArtifactType(new[] { "CF" }, "Custom Solution", "Companion File")},
            { new ArtifactType(new[] { "CHATOPS" }, "ChatOps Extension", "ChatOps Extension")},
            { new ArtifactType(new[] { "D" }, "Dashboard", "Dashboard")},
            { new ArtifactType(new[] { "DISMACRO" }, "ChatOps Extension", "DIS Macro")},
            { new ArtifactType(new[] { "DOC" }, "ChatOps Extension", "Documentation")},
            { new ArtifactType(new[] { "F" }, "Automation", "Function Definition")},
            { new ArtifactType(new[] { "GQIDS" }, "Ad Hoc Data Source", "gqidatasource", "Ad Hoc Data Source")},
            { new ArtifactType(new[] { "GQIO" }, "Data Transformer", "GQI Operator")},
            { new ArtifactType(new[] { "LSO" }, "Automation", "Live Cycle Service Orchestration")},
            { new ArtifactType(new[] { "PA" }, "Automation", "Process Automation") },
            { new ArtifactType(new[] { "PLS" }, "Automation", "Profile Load Script") },
            { new ArtifactType(new[] { "S" }, "Custom Solution", "Solution") },
            { new ArtifactType(new[] { "SC" }, "Scripted Connector", "Scriped Connector") },
            { new ArtifactType(new[] { "T" }, "Custom Solution", "Testing Solution") },
            { new ArtifactType(new[] { "UDAPI" }, "User-Defined API", "User Defined API") },
            { new ArtifactType(new[] { "V" }, "Visual Overview", "Visio") },
            { new ArtifactType(new[] { "LCA" }, "Custom Solution", "Low-Code App") }
        };
    }

    internal class ArtifactType
    {
        public string[] GitHubNames { get; set; }

        public string[] GitHubAbbreviations { get; set; }

        public string CatalogName { get; set; }

        public ArtifactType(string[] abbreviations, string catalogName, params string[] githubNames)
        {
            GitHubAbbreviations = abbreviations;

            GitHubNames = githubNames;

            CatalogName = catalogName;
        }

        public bool IsMatch(string searchTerm)
        {
            if (searchTerm.Equals(CatalogName, StringComparison.OrdinalIgnoreCase) ||
                 GitHubAbbreviations.Contains(searchTerm, StringComparer.InvariantCultureIgnoreCase) ||
                 GitHubNames.Contains(searchTerm, StringComparer.InvariantCultureIgnoreCase))
            {
                return true;
            }

            return false;
        }
    }

}
