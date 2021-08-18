using System.Diagnostics;
using System.Threading.Tasks;

namespace InstanceManager.Utility
{

    /// <summary>An utility class for running commands in the system terminal.</summary>
    public static class CommandUtility
    {

        /// <summary>Runs the command in the system terminal.</summary>
        public static Task<int> RunCommand(string command, bool closeWindowWhenDone = true) =>
            Task.Run(() =>
            {

                var p = Process.Start(new ProcessStartInfo("cmd", (closeWindowWhenDone ? "/c" : "/k ") + command)
                {
                    UseShellExecute = true,
                    CreateNoWindow = closeWindowWhenDone,
                    WindowStyle = closeWindowWhenDone ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Normal
                });

                p.WaitForExit();
                return p.ExitCode;

            });

    }

}
