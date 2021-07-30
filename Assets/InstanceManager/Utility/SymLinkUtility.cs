using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace InstanceManager.Utility
{

    /// <summary>Provides functions for interacting with SymLinker.exe.</summary>
    internal static class SymLinkUtility
    {

        /// <summary>These should not be linked inside '/Library/'.</summary>
        static readonly string[] libraryBlacklist =
        {
            "-lock",
            "\\Search",
            "LastSceneManagerSetup.txt" ,
            "EditorInstance.json"
        };

        static Task RunCommand(string command) =>
            Task.Run(() =>
            {
                var p = Process.Start(new ProcessStartInfo("cmd", "/c " + command) { CreateNoWindow = true });
                p.WaitForExit();
            });

        static async void ProgressWrapper(string displayName, Task task, Action onComplete = null, string description = null)
        {

            var progress = Progress.Start(displayName, description, options: Progress.Options.Indefinite);

            try
            {
                task.Start();
                await task;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            Progress.Remove(progress);
            onComplete?.Invoke();

        }

        /// <summary>Creates a new secondary instance.</summary>
        public static void Create(string projectPath, string targetPath, Action onComplete = null) =>
            ProgressWrapper(
               displayName: "Creating instance",
               onComplete: onComplete,
               task: new Task(async () =>
               {

                   if (Directory.Exists(targetPath))
                       Directory.Delete(targetPath, true);
                   Directory.CreateDirectory(targetPath);

                   await Task.WhenAll(GenerateTasks().ToArray());

                   IEnumerable<Task> GenerateTasks()
                   {

                       //Link folders
                       yield return SymLinkRelative("Assets");
                       yield return SymLinkRelative("Packages");
                       yield return SymLinkRelative("ProjectSettings");
                       yield return SymLinkRelative("UserSettings");

                       //Link files
                       foreach (var file in Directory.GetFiles(projectPath, "*", SearchOption.TopDirectoryOnly))
                           yield return SymLinkRelative(Path.GetFileName(file));

                       //Link all items in 'Library' folder, we need to do these individually since
                       //we need to avoid lock files and files causing conflicts
                       Directory.CreateDirectory(Path.Combine(targetPath, "Library"));
                       foreach (var file in Directory.GetDirectories(Path.Combine(projectPath, "Library"), "*", SearchOption.TopDirectoryOnly))
                       {

                           if (libraryBlacklist.Any(b => file.EndsWith(b)))
                               continue;

                           var path = Path.Combine(targetPath, "Library", Path.GetFileName(file));
                           yield return SymLink(file, path);

                       }

                       Task SymLinkRelative(string relativePath) =>
                           SymLink(
                               linkPath: Path.Combine(targetPath, relativePath),
                               path: Path.Combine(projectPath, relativePath));

                       static Task SymLink(string path, string linkPath) =>
                           Task.Run(async () =>
                           {
                               if (Directory.Exists(path))
                                   await RunCommand($"mklink {(Directory.Exists(path) ? "/j" : "/h")} {linkPath} {path}");
                           });

                   }

               }));

        /// <summary>Deletes a secondary instance from Unity Hub.</summary>
        public static void DeleteHubEntry(string instancePath, Action onComplete = null) =>
            ProgressWrapper(
               displayName: "Deleting hub entry",
               onComplete: onComplete,
               task: new Task(() =>
               {
                   using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Unity Technologies\Unity Editor 5.x", writable: true);
                   foreach (var name in key.GetValueNames().Where(n => n.StartsWith("RecentlyUsedProjectPaths")))
                   {
                       var value = Encoding.ASCII.GetString((byte[])key.GetValue(name));
                       if (value.StartsWith(instancePath.Replace(@"\", @"/")))
                           key.DeleteValue(name);
                   }
               }));


        /// <summary>Deletes a secondary instance.</summary>
        public static void Delete(string path, Action onComplete = null) =>
            ProgressWrapper(
               displayName: "Removing instance",
               //Deleting with cmd, which prevents 'Directory not empty error', for Directory.Delete(path, true)
               task: new Task(() => RunCommand($"rmdir /s/q {path}")),
               onComplete);

    }

}
