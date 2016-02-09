using System;

namespace MachinaAurum.Clusters.Processes
{
    public class ProcessChangedEventArgs : EventArgs
    {
        public int From { get; private set; }
        public int Id { get; private set; }
        public string Name { get; private set; }

        public ProcessChangedEventArgs(int from, int id, string name)
        {
            Name = name;
            From = from;
            Id = id;
        }
    }
}
