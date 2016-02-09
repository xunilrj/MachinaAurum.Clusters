using System.Threading.Tasks;

namespace MachinaAurum.Clusters.Processes
{
    public interface IProcess
    {
        Task AreYouThere();
        Task<bool> AreYouNormal();
        Task Halt(int sender);
        Task<bool> NewCoordinator(int sender);
        Task<bool> Ready(int sender, int command);
        void WriteState();

        int Id { get; }
        ProcessState State { get; }
        int Coordinator { get; }
        int Command { get; }
    }
}
