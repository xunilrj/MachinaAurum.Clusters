using MachinaAurum.Clusters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace MachinaAurum.Elections.Tests
{
    [TestClass]
    public class ClusterTests
    {
        [TestMethod]
        public void OneProcessFormAConsistentCluster()
        {
            var cluster = new InMemoryCluster();
            cluster.NewProcess(10);

            Assert.IsTrue(cluster.IsConsistent());

            cluster.WriteClusterState();
        }

        [TestMethod]
        public async Task ClusterThatStartAllProccessAtTheSameTimeMustAlwaysConverge()
        {   
            for (int i = 0; i < 2; i++)
            {
                await StartNewCluster(5);
            }            
        }

        private static async Task StartNewCluster(int size)
        {
            var cluster = new InMemoryCluster();

            await Task.Factory.StartNew(async () =>
            {
                for (int i = 0; i < size; i++)
                {
                    await Task.Delay(1);
                    cluster.NewProcess();
                }
            }).Unwrap();

            while (true)
            {
                var isConsistent = cluster.IsConsistent();
                if (isConsistent)
                {
                    break;
                }

                await Task.Delay(1000);
            }

            cluster.WriteClusterState();
        }

    }
}
