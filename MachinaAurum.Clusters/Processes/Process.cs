using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MachinaAurum.Clusters.Processes
{
    public class Process : IProcess
    {
        public event EventHandler<CallEventArgs> CallTimedOut;
        public event EventHandler<ProcessChangedEventArgs> ProcessChanged;

        void SafeInvoke<TArgs>(EventHandler<TArgs> handler, TArgs args) where TArgs : EventArgs
        {
            var h = handler;
            if (h != null)
            {
                h(this, args);
            }
        }

        public int Id { get; set; }
        HashSet<int> Up;

        public ProcessState State { get; private set; }
        int LastHalt;
        public int Coordinator { get; private set; }
        public int Command { get; private set; }

        ICluster Cluster;

        object l = new object();
        CancellationTokenSource Cancel;

        public Process(ICluster cluster)
        {
            Up = new HashSet<int>();
            Cluster = cluster;

            State = ProcessState.Uninitialized;
            Cancel = new CancellationTokenSource();
        }

        public void Start()
        {
            var electionTask = Election();

            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    //await Check();
                    await Task.Delay(1000);
                }
            });
        }


        ///IMMEDIATE PROCEDURE AreYouThere(i);
        /// BEGIN
        /// ((This procedure isusedby remotenodestodiscoverifnode i is operating.Nothing is actually done at node i.)) 
        /// END AreYouThere;
        public Task AreYouThere()
        {
            return Task.FromResult(true);
        }

        ///IMMEDIATE PROCEDURE AreYouNormal(i, answer);
        /// BEGIN
        /// ((This procedure is called by coordinators to discover the state ofa node..))
        /// [[IFS(i) * S = "Normal" 
        ///     THEN answer:= "Yes" 
        ///     ELSE answer:= "No";]]
        /// END AreYouNormal; 
        public Task<bool> AreYouNormal()
        {
            lock (l)
            {
                return Task.FromResult(State == ProcessState.Normal);
            }
        }

        ///IMMEDIATE PROCEDURE Halt(i,j);
        /// BEGIN
        /// ((When node i receives this message, it means that node j is trying to become coordinator.))
        /// [S(i).s:= "Election"; 
        /// S(i).h = j; 
        /// CALL STOP;]] 
        /// END Halt; 
        public Task Halt(int sender)
        {
            lock (l)
            {
                PrivateHalt(sender);

                return Task.FromResult(true);
            }
        }

        private void PrivateHalt(int sender)
        {
            State = ProcessState.Election;
            LastHalt = sender;

            //CALL STOP
            Cancel.Cancel();
            Cancel = new CancellationTokenSource();
        }

        ///IMMEDIATE PROCEDURE NewCoordinator(i,j)
        /// BEGIN
        /// ((Nodej uses this procedure to inform node i that nodej has becomecoordinator.))
        /// [[IF S(i).h = j AND S(i).S = "Election" 
        /// THEN 
        ///     BEGIN 
        ///         S(i).c = j; 
        ///         S(i).s = "Reorganization"; 
        ///     END;]]
        /// END NewCoordinator;
        public Task<bool> NewCoordinator(int sender)
        {
            lock (l)
            {
                if (LastHalt == sender && State == ProcessState.Election)
                {
                    Coordinator = sender;
                    State = ProcessState.Reorganization;

                    Log.WriteLine(Id, $"{Id}: {sender} is trying to be my coordinator. OK!");
                    return Task.FromResult(true);
                }
                else
                {
                    Log.WriteLine(Id, $"{Id}: {sender} is trying to be my coordinator. Sorry!");
                    return Task.FromResult(false);
                }


            }
        }

        ///IMMEDIATE PROCEDURE Ready(i,j, x);
        /// BEGIN
        /// ((Coordinatorj uses this procedure to distribute the new taskdescription x.))
        /// [[IF S(i) * C =j AND S(i) * S = "Reorganization"
        /// THEN
        ///     BEGIN
        ///         S(i).d = x;
        ///         S(i).s = "Normal";
        ///     END;]]
        /// END Ready; 
        public Task<bool> Ready(int sender, int command)
        {
            lock (l)
            {
                if (Coordinator == sender && State == ProcessState.Reorganization)
                {
                    Command = command;
                    State = ProcessState.Normal;

                    Log.WriteLine(Id, $"{Id}: {sender} is trying to put me in Ready for {command}. OK!");
                    return Task.FromResult(true);
                }
                else
                {
                    Log.WriteLine(Id, $"{Id}: {sender} is trying to put me in Ready for {command}. Sorry!");
                    return Task.FromResult(false);
                }
            }
        }

        ///PROCEDURE Election(i);
        /// BEGIN
        /// ((By executing this procedure, node i attempts to become coordinator. 
        /// First step is to check if any higher priority nodes are up.
        /// If any such node is up, quit.))
        /// FOR j = i + 1,i + 2, ..., n - 1, n DO
        /// BEGIN
        ///     ((n is number ofnodes in system.))
        ///     CALL AreYouThere(j),
        ///         ONTIMEOUT(T): NEXT ITERATION;
        ///         RETURN;
        /// END;
        /// 
        /// ((Next, halt all lower priority nodes, starting with this node.))
        /// [[CALL STOP;
        /// S(i).s = "Election";
        /// S(i).h = i;
        /// S(i).Up = {}
        /// FOR j = i-1, i-2, ..., 2, 1 DO
        /// BEGIN
        ///     CALL Halt (j, i), ONTIMEOUT(T) :
        ///             NEXT ITERATION;
        ///     [[S(i).Up = {j} UNION S(i).Up;]]
        /// END;
        /// ((Now node i has reached its "election point." Next step is to inform nodes of new coordinator.))
        /// [[S(i).c := i;
        /// S(i).s = "Reorganization";]]
        /// FOR j IN S(i).Up DO
        /// BEGIN
        ///     CALL NewCoordinator(j, i),
        ///         ONTIMEOUT(T):
        ///         BEGIN
        ///             CALL Election (i);
        ///             RETURN;
        ///             ((Node j failed. The simplest thing is to restart.))
        ///         END;
        /// END;
        /// ((The reorganization of the system occurs here.
        /// When done, node i has computed S(i).d, the new task description.
        /// (S(i).d probably contains a copy of S(i).Up.)
        /// If during the reorganization a node in S(i).Up fails, the
        /// reorganization can be stopped and a new reorganization started.))
        ///  FOR j IN S(i).Up DO
        ///  BEGIN 
        ///     CALL Ready (j, i, S(i).d),
        ///         ONTIMEOUT (T): 
        ///         BEGIN
        ///             CALL Election (i); 
        ///             RETURN; 
        ///         END; 
        /// END; 
        /// [[S(i).s = "Normal";]] 
        /// END Election;
        public async Task Election()
        {
            var token = Cancel.Token;

            Log.WriteLine(Id, $"{Id}: Election Started");

            Log.WriteLine(Id, $"{Id}: Checking any higher node is up");

            //for (int i = Id + 1; i < Cluster.ProcessCount; i++)
            foreach (int i in Cluster.GetMorePriorityProcessess(Id))
            {
                CancelIfNeeeded(token);

                try
                {
                    Log.WriteLine(Id, $"{Id}: {i} AreYouThere?");

                    await Cluster.GetProcess(i).AreYouThere();
                    //SafeInvoke(ProcessChanged, new ProcessChangedEventArgs(Id, i, "Up"));

                    Log.WriteLine(Id, $"{Id}: {i} is up");
                    Log.WriteLine(Id, $"{Id}: Election Aborted");
                    return;
                }
                catch (TimeoutException)
                {
                    SafeInvoke(CallTimedOut, new CallEventArgs("AreYouThere", Id, i));
                    continue;
                }
            }

            CancelIfNeeeded(token);

            Log.WriteLine(Id, $"{Id}: Checking finished");
            Log.WriteLine(Id, $"{Id}: Halting myself");

            lock (l)
            {
                CancelIfNeeeded(token);

                PrivateHalt(Id);
                token = Cancel.Token;

                Up.Clear();
            }

            Log.WriteLine(Id, $"{Id}: Halting finished");
            Log.WriteLine(Id, $"{Id}: Halting low priority nodes");

            CancelIfNeeeded(token);

            //for (int i = Id - 1; i >= 1; i--)
            foreach (var i in Cluster.GetLessPriorityProcessess(Id))
            {
                CancelIfNeeeded(token);

                try
                {
                    Log.WriteLine(Id, $"{Id}: {i} halting...");

                    await Cluster.GetProcess(i).Halt(Id);
                    lock (l)
                    {
                        Up.Add(i);
                    }

                    //SafeInvoke(ProcessChanged, new ProcessChangedEventArgs(Id, i, "Halted"));
                    Log.WriteLine(Id, $"{Id}: {i} halted");
                }
                catch (TimeoutException)
                {
                    SafeInvoke(CallTimedOut, new CallEventArgs("Halt", Id, i));
                    continue;
                }
            }

            CancelIfNeeeded(token);

            Log.WriteLine(Id, $"{Id}: Halting low priority nodes finished");

            lock (l)
            {
                CancelIfNeeeded(token);
                Coordinator = Id;
                State = ProcessState.Reorganization;
            }

            Log.WriteLine(Id, $"{Id}: Tell everyone I am the coordinator");

            CancelIfNeeeded(token);

            foreach (var i in Up)
            {
                CancelIfNeeeded(token);

                try
                {
                    Log.WriteLine(Id, $"{Id}: {i} Setting me as coordinator of {i}");
                    var result = await Cluster.GetProcess(i).NewCoordinator(Id);

                    if (result)
                    {
                        Log.WriteLine(Id, $"{Id}: {i} I am the coordinator of {i}");
                    }
                    else
                    {
                        Log.WriteLine(Id, $"{Id}: {i} denied me as the coordinator");
                        RestartElection();
                        return;
                    }
                }
                catch (TimeoutException)
                {
                    SafeInvoke(CallTimedOut, new CallEventArgs("NewCoordinator", Id, i));
                    RestartElection();
                    return;
                }
            }

            CancelIfNeeeded(token);

            Log.WriteLine(Id, $"{Id}: Tell everyone I am the coordinator finished");
            Log.WriteLine(Id, $"{Id}: Put all process in Ready");

            Command++;

            foreach (var i in Up)
            {
                CancelIfNeeeded(token);

                try
                {
                    Log.WriteLine(Id, $"{Id}: {i} Setting {i} as Ready to process {Command}");
                    var result = await Cluster.GetProcess(i).Ready(Id, Command);

                    if (result)
                    {
                        Log.WriteLine(Id, $"{Id}: {i} is Ready and processing {Command}");
                    }
                    else
                    {
                        Log.WriteLine(Id, $"{Id}: {i} denied my Ready to process {Command}");
                        RestartElection();
                        return;
                    }
                }
                catch (TimeoutException)
                {
                    SafeInvoke(CallTimedOut, new CallEventArgs("NewCoordinator", Id, i));
                    RestartElection();
                    return;
                }
            }

            CancelIfNeeeded(token);

            Log.WriteLine(Id, $"{Id}: Put all process in Ready finished");

            lock (l)
            {
                CancelIfNeeeded(token);
                State = ProcessState.Normal;
            }

            Log.WriteLine(Id, $"{Id}: Election finished successfully");

            CancelIfNeeeded(token);
        }

        private void RestartElection()
        {
            Log.WriteLine(Id, $"{Id}: Election Aborted");

            var random = new Random();
            var time = random.Next(2000);

            Log.WriteLine(Id, $"{Id}: Will wait for {time}ms and restart Election");

            Thread.Sleep(time);

            var electionTask = Election();
        }

        private void CancelIfNeeeded(CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                Log.WriteLine(Id, $"{Id}: Cancellation requested");
                token.ThrowIfCancellationRequested();
            }
        }

        ///PROCEDURE Recovery(i);
        /// BEGIN
        /// ((This procedure is automatically called by the operating system when a node starts up after a failure.))
        /// S(i).h = UNDEFINED;
        /// ((e.g., set it to -1))
        /// CALL Election(i);
        /// END Recovery;
        public void Recovery()
        {
            LastHalt = -1;
            var electionTask = Election();
        }

        ///PROCEDURE Check(i);
        /// BEGIN
        /// ((This procedure is called periodically by the operating system.) )
        /// IF [[S(i).S = "Normal" AND S(i).c = i]] THEN
        /// BEGIN
        ///     ((I am coordinator. Check if everyone else is normal.))
        ///     For j = 1, 2, ... , i - 1, i + 1, ...,n DO
        ///     BEGIN
        ///         CALL AreYouNormal(j, ans), ONTIMEOUT(T): NEXT ITERATION;
        ///         IF ans = "No" THEN
        ///         BEGIN
        ///             CALL Election(i);
        ///             RETURN;
        ///         END;
        ///     END;
        /// END; 
        /// END Check;
        public async Task Check()
        {
            Log.WriteLine(Id, $"{Id}: Checking...");
            if (State == ProcessState.Normal && Coordinator == Id)
            {
                for (int i = 1; i <= Cluster.ProcessCount; i++)
                {
                    try
                    {
                        Log.WriteLine(Id, $"{Id}: Checking {i}");
                        var answer = await Cluster.GetProcess(i).AreYouNormal();
                        if (answer == false)
                        {
                            Log.WriteLine(Id, $"{Id}: Checking {i} is normal");
                            var electionTask = Election();
                        }
                        else
                        {
                            Log.WriteLine(Id, $"{Id}: Checking {i} is normal");
                        }
                    }
                    catch (TimeoutException)
                    {
                        SafeInvoke(CallTimedOut, new CallEventArgs("AreYouNormal", Id, i));
                        continue;
                    }
                }
            }
            Log.WriteLine(Id, $"{Id}: Checking finished");
        }

        ///PROCEDURE Timeout(i);
        /// BEGIN
        /// ((This procedure is called automatically when node i has not heard from the coordinator in a "long" time.))
        /// IF [[S(i).S = "Normal" OR S(i).S "Reorganization']] THEN
        /// BEGIN
        ///     ((Check ifcoordinator is up.))
        ///     CALL AreYouThere(S(i).c),
        ///         ONTIMEOUT(T): CALL Election(i);
        /// END
        /// ELSE
        ///     CALL Election(i);
        /// END Timeout;
        public async Task Timeout()
        {
            if (State == ProcessState.Normal || State == ProcessState.Reorganization)
            {
                try
                {
                    await Cluster.GetProcess(Coordinator).AreYouThere();
                }
                catch (TimeoutException)
                {
                    SafeInvoke(CallTimedOut, new CallEventArgs("AreYouThere", Id, Coordinator));
                    var electionTask = Election();
                }
            }
            else
            {
                var electionTask = Election();
            }
        }

        public void WriteState()
        {
            lock (l)
            {
                Console.WriteLine($"{Id}: Coordinator {Coordinator} Last Halt {LastHalt} State {State} Command {Command}");
            }
        }
    }
}
