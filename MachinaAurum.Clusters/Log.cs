using System;
using System.Linq;

namespace MachinaAurum.Clusters
{
    public static class Log
    {
        public static void WriteLine(int id, string msg)
        {
            Console.WriteLine($"{string.Join("", Enumerable.Range(0, id).Select(x => '\t'))} {msg}");
        }
    }
}
