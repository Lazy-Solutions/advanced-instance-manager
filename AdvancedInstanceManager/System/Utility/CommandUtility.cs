using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;

namespace InstanceManager.Utility
{

    /// <summary>An utility class for running commands in the system terminal.</summary>
    public static class CommandUtility
    {

        /// <summary>Runs a command in the system terminal, chosen depending on which platform we're currently running on. Error is logged in console.</summary>
        public static Task<int> RunCommand(string windows = null, string linux = null, string osx = null)
        {
#if UNITY_EDITOR_WIN
            return RunCommandWindows(windows);
#elif UNITY_EDITOR_LINUX || UNITY_EDITOR_OSX
            return RunCommandLinux(linux);
#endif
        }

        /// <summary>Runs the command in the windows system terminal. Error is logged in console.</summary>
        public static Task<int> RunCommandWindows(string command) =>
            Task.Run(() =>
            {

                using (var p = Process.Start(new ProcessStartInfo("cmd", "/c " + command)
                {
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }))
                {

                    p.WaitForExit();
                    if (!p.StandardError.EndOfStream)
                        Debug.LogError($"Command '{command}' failed:" + Environment.NewLine + p.StandardError.ReadToEnd());

                    return p.ExitCode;

                }

            });

        /// <summary>Runs the command in the linux system terminal. Error is logged in console. This method can be used OSX.</summary>
        public static Task<int> RunCommandLinux(string command) =>
            Task.Run(() =>
            {

                using (var p = Process.Start(new ProcessStartInfo("/bin/bash", "-c \"" + command.Replace("\"", "\"\"") + "\"")
                {
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }))
                {

                    p.WaitForExit();
                    if (!p.StandardError.EndOfStream)
                        Debug.LogError($"Command '{command}' failed:" + Environment.NewLine + p.StandardError.ReadToEnd());

                    return p.ExitCode;

                }

            });

    }

}
