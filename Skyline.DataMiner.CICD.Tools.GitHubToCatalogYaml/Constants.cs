namespace Skyline.DataMiner.CICD.Tools.GitHubToCatalogYaml
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal static class Constants
    {
        public static readonly List<ArtifactType> ArtifactTypeMap = new List<ArtifactType>()
        {
            { new ArtifactType(new[] { "AS" }, "Automation", "Automation Script", "automationscript", "dataminer-automation-script") },
            { new ArtifactType(new[] { "C" }, "Connector", "Connector", "connector","dataminer-connector")},
            { new ArtifactType(new[] { "CF" }, "Custom Solution", "Companion File", "companionfile","dataminer-companion-file")},
            { new ArtifactType(new[] { "CHATOPS" }, "ChatOps Extension", "ChatOps Extension", "chatopsextension", "dataminer-bot", "dataminer-chatops")},
            { new ArtifactType(new[] { "D" }, "Dashboard", "Dashboard", "dashboard", "dataminer-dashboard")},
            { new ArtifactType(new[] { "DOC" }, "Custom Solution", "Documentation", "documentation", "dataminer-doc")},
            { new ArtifactType(new[] { "F" }, "Custom Solution", "Function Definition", "functiondefinition", "dataminer-function")},
            { new ArtifactType(new[] { "GQIDS" }, "Ad Hoc Data Source", "gqidatasource", "Ad Hoc Data Source", "adhocdatasource", "dataminer-gqi-data-source")},
            { new ArtifactType(new[] { "GQIO" }, "Data Transformer", "GQI Operator", "gqioperator", "dataminer-gqi-operator")},
            { new ArtifactType(new[] { "LSO" }, "Automation", "Live Cycle Service Orchestration", "lifecycleserviceorchestration", "dataminer-life-service-orchestration")},
            { new ArtifactType(new[] { "PA" }, "Automation", "Process Automation", "processautomation", "dataminer-process-automation-script") },
            { new ArtifactType(new[] { "PLS" }, "Automation", "Profile Load Script", "profileloadscript", "dataminer-profile-load-script") },
            { new ArtifactType(new[] { "S" }, "Custom Solution", "Solution", "solution", "dataminer","dataminer-solution", "dataminer-dis-macro", "dataminer-nuget") },
            { new ArtifactType(new[] { "SC" }, "Scripted Connector", "Scripted Connector", "scriptedconnector","dataminer-scripted-connector") },
            { new ArtifactType(new[] { "T" }, "Custom Solution", "Testing Solution", "testingsolution", "dataminer-regression-test", "dataminer-UI-test") },
            { new ArtifactType(new[] { "UDAPI" }, "User-Defined API", "User Defined API", "userdefinedapi", "dataminer-user-defined-api") },
            { new ArtifactType(new[] { "V" }, "Visual Overview", "Visio", "visio","dataminer-visio") },
            { new ArtifactType(new[] { "LCA" }, "Custom Solution", "Low-Code App", "lowcodeapp", "dataminer-low-code-app") }
        };
    }

    internal class ArtifactType
    {
        public ArtifactType(string[] abbreviations, string catalogName, params string[] githubNames)
        {
            GitHubAbbreviations = abbreviations;

            GitHubNames = githubNames;

            CatalogName = catalogName;
        }

        public string CatalogName { get; set; }

        public string[] GitHubAbbreviations { get; set; }

        public string[] GitHubNames { get; set; }

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