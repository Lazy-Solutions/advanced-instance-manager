using System.Diagnostics;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;

namespace InstanceManager.Utility
{

    /// <summary>An utility class for running commands in the system terminal.</summary>
    public static class CommandUtility
    {

        /// <summary>Runs a command in the system terminal, chosen depending on which platform we're currently running on. Error is logged in console.</summary>
        public static Task<int> RunCommand(string windows = null, string linux = null, string osx = null, bool closeWindowWhenDone = true)
        {
#if UNITY_EDITOR_WIN
            return RunCommandWindows(windows, closeWindowWhenDone);
#elif UNITY_EDITOR_LINUX
            return RunCommandLinux(linux, closeWindowWhenDone);
#elif UNITY_EDITOR_OSX
            return RunCommandOSX(osx, closeWindowWhenDone);
#endif
        }

        /// <summary>Runs the command in the windows system terminal. Error is logged in console.</summary>
        public static Task<int> RunCommandWindows(string command, bool closeWindowWhenDone = true) =>
            Task.Run(() =>
            {

                using (var p = Process.Start(new ProcessStartInfo("cmd", (closeWindowWhenDone ? "/c " : "/k ") + command)
                {
                    UseShellExecute = true,
                    RedirectStandardError = true,
                    CreateNoWindow = closeWindowWhenDone,
                    WindowStyle = closeWindowWhenDone ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Normal
                }))
                {

                    p.WaitForExit();
                    if (!p.StandardError.EndOfStream)
                        Debug.LogError(p.StandardError.ReadToEnd());

                    return p.ExitCode;

                }

            });

        /// <summary>Runs the command in the linux system terminal. Error is logged in console.</summary>
        public static Task<int> RunCommandLinux(string command, bool closeWindowWhenDone = true) =>
            Task.Run(() =>
            {

                using (var p = Process.Start(new ProcessStartInfo("/bin/bash", "-c \"" + command.Replace("\"", "\\\"") + "\"")
                {
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    CreateNoWindow = closeWindowWhenDone,
                    WindowStyle = closeWindowWhenDone ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Normal
                }))
                {

                    p.WaitForExit();
                    if (!p.StandardError.EndOfStream)
                        Debug.LogError(p.StandardError.ReadToEnd());

                    return p.ExitCode;

                }

            });

        /// <summary>Runs the command in the osx system terminal. Error is logged in console.</summary>
        public static Task<int> RunCommandOSX(string command, bool closeWindowWhenDone = true) =>
            Task.Run(() =>
            {
                Debug.LogWarning("OSX not yet supported.");
                return 0;
            });

    }

}
