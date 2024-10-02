namespace Skyline.DataMiner.CICD.Tools.GitHubToCatalogYaml
{
	using System;
	using System.Collections.Generic;

	internal static class Constants
	{
		// Dictionary mapping keywords to ArtifactContentType
		public static readonly Dictionary<string, string> ArtifactTypeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		{ "AS", "automationscript" },
		{ "C", "connector" },
		{ "CF", "companionfile" },
		{ "CHATOPS", "chatopsextension" },
		{ "D", "dashboard" },
		{ "DISMACRO", "dismacro" },
		{ "DOC", "documentation" },
		{ "F", "functiondefinition" },
		{ "GQIDS", "gqidatasource" },
		{ "GQIO", "gqioperator" },
		{ "LSO", "lifecycleserviceorchestration" },
		{ "PA", "processautomation" },
		{ "PLS", "profileloadscript" },
		{ "S", "solution" },
		{ "SC", "scriptedconnector" },
		{ "T", "testingsolution" },
		{ "UDAPI", "userdefinedapi" },
		{ "V", "visio" }
	};
	}
}