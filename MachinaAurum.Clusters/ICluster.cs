using MachinaAurum.Clusters.Processes;
using System.Collections.Generic;

namespace MachinaAurum.Clusters
{
    public interface ICluster
    {
        int ProcessCount { get; }
        IProcess GetProcess(int i);
        IEnumerable<int> GetMorePriorityProcessess(int id);
        IEnumerable<int> GetLessPriorityProcessess(int id);
    }
}
