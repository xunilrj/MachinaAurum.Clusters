using System;

namespace MachinaAurum.Clusters.Processes
{
    public class CallEventArgs : EventArgs
    {
        public int From { get; private set; }
        public int To { get; private set; }
        public string Name { get; private set; }

        public CallEventArgs(string name, int from, int to)
        {
            Name = name;
            From = from;
            To = to;
        }
    }
}
