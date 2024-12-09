namespace Skyline.DataMiner.CICD.Tools.GitHubToCatalogYaml
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal static class Constants
    {
        public static readonly List<ArtifactType> ArtifactTypeMap = new List<ArtifactType>()
        {
            { new ArtifactType(new[] { "AS" }, "automationscript", "Automation Script") },
            { new ArtifactType(new[] { "C" }, "connector", "Connector")},
            { new ArtifactType(new[] { "CF" }, "companionfile", "Companion File")},
            { new ArtifactType(new[] { "CHATOPS" }, "chatopsextension", "ChatOps Extension")},
            { new ArtifactType(new[] { "D" }, "dashboard", "Dashboard")},
            { new ArtifactType(new[] { "DISMACRO" }, "dismacro", "DIS Macro")},
            { new ArtifactType(new[] { "DOC" }, "documentation", "Documentation")},
            { new ArtifactType(new[] { "F" }, "functiondefinition", "Function Definition")},
            { new ArtifactType(new[] { "GQIDS" }, "adhocdatasource", "gqidatasource", "Ad Hoc Data Source")},
            { new ArtifactType(new[] { "GQIO" }, "gqioperator", "GQI Operator")},
            { new ArtifactType(new[] { "LSO" }, "lifecycleserviceorchestration", "Live Cycle Service Orchestration")},
            { new ArtifactType(new[] { "PA" }, "processautomation", "Process Automation") },
            { new ArtifactType(new[] { "PLS" }, "profileloadscript", "Profile Load Script") },
            { new ArtifactType(new[] { "S" }, "solution", "Solution") },
            { new ArtifactType(new[] { "SC" }, "scriptedconnector", "Scriped Connector") },
            { new ArtifactType(new[] { "T" }, "testingsolution", "Testing Solution") },
            { new ArtifactType(new[] { "UDAPI" }, "userdefinedapi", "User Defined API") },
            { new ArtifactType(new[] { "V" }, "visio", "Visio") },
            { new ArtifactType(new[] { "LCA" }, "lowcodeapp", "Low-Code App") }
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
