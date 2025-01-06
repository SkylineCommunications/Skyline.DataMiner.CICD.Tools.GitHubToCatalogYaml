namespace Skyline.DataMiner.CICD.Tools.GitHubToCatalogYaml
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal static class Constants
    {
        public static readonly List<ArtifactType> ArtifactTypeMap = new List<ArtifactType>()
        {
            { new ArtifactType(new[] { "AS" }, "Automation", "Automation Script", "automationscript") },
            { new ArtifactType(new[] { "C" }, "Connector", "Connector", "connector")},
            { new ArtifactType(new[] { "CF" }, "Custom Solution", "Companion File", "companionfile")},
            { new ArtifactType(new[] { "CHATOPS" }, "ChatOps Extension", "ChatOps Extension", "chatopsextension")},
            { new ArtifactType(new[] { "D" }, "Dashboard", "Dashboard", "dashboard")},
            { new ArtifactType(new[] { "DOC" }, "Custom Solution", "Documentation", "documentation")},
            { new ArtifactType(new[] { "F" }, "Custom Solution", "Function Definition", "functiondefinition")},
            { new ArtifactType(new[] { "GQIDS" }, "Ad Hoc Data Source", "gqidatasource", "Ad Hoc Data Source", "adhocdatasource")},
            { new ArtifactType(new[] { "GQIO" }, "Data Transformer", "GQI Operator", "gqioperator")},
            { new ArtifactType(new[] { "LSO" }, "Automation", "Live Cycle Service Orchestration", "lifecycleserviceorchestration")},
            { new ArtifactType(new[] { "PA" }, "Automation", "Process Automation", "processautomation") },
            { new ArtifactType(new[] { "PLS" }, "Automation", "Profile Load Script", "profileloadscript") },
            { new ArtifactType(new[] { "S" }, "Custom Solution", "Solution", "solution") },
            { new ArtifactType(new[] { "SC" }, "Scripted Connector", "Scripted Connector", "scriptedconnector") },
            { new ArtifactType(new[] { "T" }, "Custom Solution", "Testing Solution", "testingsolution") },
            { new ArtifactType(new[] { "UDAPI" }, "User-Defined API", "User Defined API", "userdefinedapi") },
            { new ArtifactType(new[] { "V" }, "Visual Overview", "Visio", "visio") },
            { new ArtifactType(new[] { "LCA" }, "Custom Solution", "Low-Code App", "lowcodeapp") }
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
