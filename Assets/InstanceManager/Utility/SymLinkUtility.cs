#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace InstanceManager.Utility
{

    /// <summary>Provides functions for interacting with SymLinker.exe.</summary>
    internal static class SymLinkUtility
    {

        const string DeleteHubEntryParam = "-delHubEntry";
        const string DeleteParam = "-delete";

        static string symLinkerPath => Path.Combine(Paths.project, "EmbeddedInstances", "SymLinker.exe");

        static readonly Dictionary<int, string> SymLinkerErrorCodes = new Dictionary<int, string>()
        {
            { -1, "Unknown error."},
            { 1,  "Process is not elevated." },
            { 2,  "Source folder does not exist." },
            { 3,  "Target folder is not empty." },
            { 4,  "Source and target folders are the same."},
        };

        /// <summary>Checks if available. Logs to console if not.</summary>
        public static bool CheckAvailable()
        {
            if (!isAvailable)
                Debug.LogError(@"Could not find SymLinker tool, please reinstall Instance Manager, or follow instructions at <a>https://github.com/zumwani/unity-instance-manager/wiki/symlinker</a>.");
            return isAvailable;
        }

        /// <summary>Gets if sym linker is available.</summary>
        public static bool isAvailable =>
            File.Exists(symLinkerPath);

        /// <summary>Creates a new secondary instance.</summary>
        public static void Create(string progressString, string projectPath, string targetPath, Action onComplete = null) =>
            Invoke(progressString, projectPath, targetPath, onComplete);

        /// <summary>Deletes a secondary instance from Unity Hub.</summary>
        public static void DeleteHubEntry(string progressString, string instancePath, Action onComplete = null) =>
            Invoke(progressString, DeleteHubEntryParam, instancePath, onComplete);

        /// <summary>Deletes a secondary instance.</summary>
        public static void Delete(string progressString, string path, Action onComplete = null) =>
            Invoke(progressString, DeleteParam, path, onComplete);

        static void Invoke(string progressString, string p1, string p2, Action onComplete = null) =>
            EditorApplication.delayCall += () =>
            {

                if (!CheckAvailable())
                    return;

                var progress = Progress.Start(progressString, options: Progress.Options.Indefinite);

                void OnComplete()
                {
                    onComplete?.Invoke();
                    Progress.Remove(progress);
                }

                try
                {

                    var p = Process.Start(new ProcessStartInfo(symLinkerPath, p1 + " " + p2)
                    {
                        UseShellExecute = true,
                        Verb = "runas",
                        WindowStyle = ProcessWindowStyle.Hidden
                    });

                    p.EnableRaisingEvents = true;
                    p.Exited += OnExit;

                    void OnExit(object sender, EventArgs e)
                    {

                        //Run on main thread
                        EditorApplication.delayCall += () =>
                        {

                            if (p.ExitCode != 0)
                            {
                                if (SymLinkerErrorCodes.TryGetValue(p.ExitCode, out var message))
                                    Debug.LogError(message);
                                else
                                    Debug.LogError(new Win32Exception(p.ExitCode));
                            }

                            p.Exited -= OnExit;
                            OnComplete();

                        };
                        EditorApplication.QueuePlayerLoopUpdate();

                    }

                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    OnComplete();
                }

            };

    }

}
#endif
