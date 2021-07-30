using System.Diagnostics;
using System.Threading.Tasks;

namespace InstanceManager.Utility
{

    public static class CommandUtility
    {

        public static Task RunCommand(string command) =>
            Task.Run(() =>
            {
                var p = Process.Start(new ProcessStartInfo("cmd", "/c " + command) { WindowStyle = ProcessWindowStyle.Hidden });
                p.WaitForExit();
            });

    }

}
