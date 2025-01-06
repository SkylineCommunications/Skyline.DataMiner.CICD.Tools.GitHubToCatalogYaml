namespace CICD.Tools.GitHubToCatalogYamlTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Skyline.DataMiner.CICD.Tools.GitHubToCatalogYaml;

    [TestClass]
    public class CleanTitleTests
    {
        [TestMethod]
        [DataRow("SkylineCommunications/SLC-AS-MediaOps-Apps", "MediaOps-Apps", "Automation")]
        [DataRow("SLC-AS-MediaOps-Apps", "MediaOps-Apps", "Automation")]
        [DataRow("SkylineCommunications/PCKTV-AS-RegressionTests", "RegressionTests", "Automation")]
        [DataRow("SkylineCommunications/RBM-AS-Playout", "Playout", "Automation")]
        [DataRow("SkylineCommunications/SLC-Doc-Vodafone-Deutschland-GmbH", "Vodafone-Deutschland-GmbH", "Custom Solution")]
        [DataRow("SkylineCommunications/FOXA-GQIDS-GetAppearTVData", "GetAppearTVData", "Ad Hoc Data Source")]
        [DataRow("SkylineCommunications/YLE-C-Avid-iNewsOrder-Ingest", "Avid-iNewsOrder-Ingest", "Connector")]
        [DataRow("SkylineCommunications/ngx-dwa-theme-creation-helper", "ngx-dwa-theme-creation-helper", null)]
        public void CleanTitleTestHappy(string input, string expectedTitle, string expectedType)
        {
            var ct = new CleanTitle(input);
            Assert.AreEqual(expectedTitle, ct.Value, "ct.Value");
            Assert.AreEqual(expectedType, ct.FoundItemType, "ct.FoundItemType");
        }
    }
}