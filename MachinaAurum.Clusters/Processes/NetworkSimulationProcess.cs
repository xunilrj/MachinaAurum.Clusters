using System;
using System.Threading.Tasks;

namespace MachinaAurum.Clusters.Processes
{
    public class NetworkSimulationProcess : IProcess
    {
        IProcess RealProcess;
        Random Random = new Random();

        public ProcessState State
        {
            get
            {
                return RealProcess.State;
            }
        }

        public int Coordinator
        {
            get
            {
                return RealProcess.Coordinator;
            }
        }

        public int Command
        {
            get
            {
                return RealProcess.Command;
            }
        }

        public int Id
        {
            get
            {
                return RealProcess.Id;
            }
        }

        public NetworkSimulationProcess(IProcess realProcess)
        {
            RealProcess = realProcess;
        }

        public async Task AreYouThere()
        {
            await Delay();
            await RealProcess.AreYouThere();
        }

        public async Task<bool> AreYouNormal()
        {
            await Delay();
            return await RealProcess.AreYouNormal();
        }

        public async Task Halt(int sender)
        {
            await Delay();
            await RealProcess.Halt(sender);
        }

        public async Task<bool> NewCoordinator(int sender)
        {
            await Delay();
            return await RealProcess.NewCoordinator(sender);
        }

        public async Task<bool> Ready(int sender, int command)
        {
            await Delay();
            return await RealProcess.Ready(sender, command);
        }

        public void WriteState()
        {
            RealProcess.WriteState();
        }

        private async Task Delay()
        {
            Guid g = Guid.NewGuid();
            Console.WriteLine($"{g}: In the network ...");
            await Task.Delay(Random.Next(1000));
            Console.WriteLine($"{g}: Arrived!");
        }
    }
}
