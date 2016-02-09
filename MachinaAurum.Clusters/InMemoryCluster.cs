using MachinaAurum.Clusters.Processes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MachinaAurum.Clusters
{
    public class InMemoryCluster : ICluster
    {
        int _ProcessCount;
        ConcurrentDictionary<int, IProcess> Processess = new ConcurrentDictionary<int, IProcess>();

        public int ProcessCount { get { return _ProcessCount; } }

        public IProcess NewProcess(int? pid = null)
        {
            int i = 0;

            if (pid.HasValue)
            {
                _ProcessCount = pid.Value;
                i = pid.Value;
            }
            else
            {
                i = Interlocked.Increment(ref _ProcessCount);
            }

            var process = new Process(this)
            {
                Id = i
            };

            process.ProcessChanged += (s, e) =>
            {
                Console.WriteLine($"ProcessChanges {e.From}: {e.Id} {e.Name}");
            };


            Processess.TryAdd(i, process);

            process.Start();

            return process;
        }

        public IProcess GetProcess(int i)
        {
            return new NetworkSimulationProcess(Processess[i]);
        }

        public void WriteClusterState()
        {
            Console.WriteLine("Cluster State");
            foreach (var i in Processess)
            {
                var p = GetProcess(i.Key);
                p.WriteState();
            }
        }

        public IEnumerable<int> GetMorePriorityProcessess(int id)
        {
            return Processess.Where(x => x.Key > id).Select(x => x.Key);
        }

        public IEnumerable<int> GetLessPriorityProcessess(int id)
        {
            return Processess.Where(x => x.Key < id).Select(x => x.Key);
        }

        public bool IsConsistent()
        {
            /// If (S(i) - s = "Reorganization" or S(i) * s = "Normal") and if (S(j) * s = "Reorganization" orS(j) - = "Normal")
            /// then S(i) c = S(j) c. 

            var ps1 = Processess.Select(x => x.Key).ToArray();
            var ps2 = Processess.Select(x => x.Key).ToArray();

            var inconsistents = from p1Id in ps1
                                from p2Id in ps2
                                where p1Id != p2Id
                                let p1 = Processess[p1Id]
                                let p2 = Processess[p1Id]
                                let p1ReorganizingOrNormal = p1.State == ProcessState.Reorganization || p1.State == ProcessState.Normal
                                let p2ReorganizingOrNormal = p2.State == ProcessState.Reorganization || p2.State == ProcessState.Normal
                                let conclusion = (p1ReorganizingOrNormal || p2ReorganizingOrNormal) ? (p1.Coordinator == p2.Coordinator) : (false)
                                where conclusion == false
                                select conclusion;

            /// IfS(i) s ="Normal"andS(j) s = "Normal,"then S(i)*-d =S(j) d.
            /// 
            var inconsistents2 = from p1Id in ps1
                                 from p2Id in ps2
                                 where p1Id != p2Id
                                 let p1 = Processess[p1Id]
                                 let p2 = Processess[p1Id]
                                 let conclusion = (p1.State == ProcessState.Normal && p2.State == ProcessState.Normal) ? (p1.Command == p2.Command) : (false)
                                 where conclusion == false
                                 select conclusion;

            return (inconsistents.Union(inconsistents2)).Any() == false;
        }
    }
}
