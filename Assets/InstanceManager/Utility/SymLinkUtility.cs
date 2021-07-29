#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace InstanceManager.Utility
{

    /// <summary>Provides functions for interacting with SymLinker.exe.</summary>
    internal static class SymLinkUtility
    {

        const string DeleteHubEntryParam = "-delHubEntry";
        const string DeleteParam = "-delete";

        static readonly Dictionary<int, string> SymLinkerErrorCodes = new Dictionary<int, string>()
        {
            { -1, "Unknown error."},
            { 1,  "Process is not elevated." },
            { 2,  "Source folder does not exist, or is locked." },
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
            File.Exists(Paths.symLinker);

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

                //var command = "/C mklink /J " + string.Format("\"{0}\" \"{1}\"", p2, p1);
                //Process.Start("cmd", command);

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

                    var p = Process.Start(new ProcessStartInfo(Paths.symLinker, p1 + " " + p2)
                    {
                        UseShellExecute = true,
                        //Verb = "runas",
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

        public static async Task<bool> HasUpdate() =>
         !isAvailable || (await GetLatestVersion() > GetInstalledVersion());

        static Uri SymLinkerExe => new Uri("https://raw.githubusercontent.com/Lazy-Solutions/InstanceManager.SymLinker/main/publish/SymLinker.exe");
        static Uri SymLinkerVersion => new Uri("https://raw.githubusercontent.com/Lazy-Solutions/InstanceManager.SymLinker/main/publish/SymLinker.version");

        public static async Task<int?> GetLatestVersion()
        {

            using (var client = new WebClient())
            {
                var str = await client.DownloadStringTaskAsync(SymLinkerVersion);
                if (int.TryParse(str, out var i))
                    return i;
            }

            return null;

        }

        public static int GetInstalledVersion()
        {

            if (File.Exists(Paths.symLinkerVersion))
            {
                var str = File.ReadAllText(Paths.symLinkerVersion);
                if (int.TryParse(str, out var i))
                    return i;
            }

            return 0;

        }

        public static async void Update(Action onDone = null)
        {

            EditorApplication.update += OnUpdate;

            await Task.WhenAll(
                            Download(SymLinkerExe, Paths.symLinker),
                            Download(SymLinkerVersion, Paths.symLinkerVersion));

            await Task.Delay(750);

            EditorApplication.update -= OnUpdate;
            EditorUtility.ClearProgressBar();
            onDone?.Invoke();

            async Task Download(Uri uri, string file)
            {

                if (File.Exists(file))
                    File.Delete(file);

                using (var client = new WebClient())
                    await client.DownloadFileTaskAsync(uri, file);

            }

            void OnUpdate()
            {
                EditorUtility.DisplayProgressBar("Updating SymLinker.exe", "Updating...", 0);
                EditorApplication.QueuePlayerLoopUpdate();
            }

        }

    }

}
#endif
