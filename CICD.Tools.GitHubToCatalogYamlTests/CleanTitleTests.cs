namespace CICD.Tools.GitHubToCatalogYamlTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Skyline.DataMiner.CICD.Tools.GitHubToCatalogYaml;

    [TestClass]
    public class CleanTitleTests
    {
        [TestMethod]
        [DataRow("SkylineCommunications/SLC-AS-MediaOps-Apps", "MediaOps-Apps", "automationscript")]
        [DataRow("SLC-AS-MediaOps-Apps", "MediaOps-Apps", "automationscript")]
        [DataRow("SkylineCommunications/PCKTV-AS-RegressionTests", "RegressionTests", "automationscript")]
        [DataRow("SkylineCommunications/RBM-AS-Playout", "Playout", "automationscript")]
        [DataRow("SkylineCommunications/SLC-Doc-Vodafone-Deutschland-GmbH", "Vodafone-Deutschland-GmbH", "documentation")]
        [DataRow("SkylineCommunications/FOXA-GQIDS-GetAppearTVData", "GetAppearTVData", "gqidatasource")]
        [DataRow("SkylineCommunications/YLE-C-Avid-iNewsOrder-Ingest", "Avid-iNewsOrder-Ingest", "connector")]
        [DataRow("SkylineCommunications/ngx-dwa-theme-creation-helper", "ngx-dwa-theme-creation-helper", null)]
        public void CleanTitleTestHappy(string input, string expectedTitle, string expectedType)
        {
            var ct = new CleanTitle(input);
            Assert.AreEqual(expectedTitle, ct.Value, "ct.Value");
            Assert.AreEqual(expectedType, ct.FoundItemType, "ct.FoundItemType");
        }
    }
}