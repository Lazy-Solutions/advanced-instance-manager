using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            "EditorInstance.json",
            "ArtifactDB",
            "SourceAssetDB",
            "\\Bee"
        };

        /// <summary>Creates a new secondary instance.</summary>
        public static Task Create(string projectPath, string targetPath, Action onComplete = null, bool hideProgress = false) =>
           ProgressUtility.RunTask(
               displayName: "Creating instance",
               onComplete: (t) => onComplete?.Invoke(),
               hideProgress: hideProgress,
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
                       foreach (var file in Directory.GetFileSystemEntries(Path.Combine(projectPath, "Library"), "*", SearchOption.TopDirectoryOnly))
                       {

                           if (libraryBlacklist.Any(b => file.EndsWith(b)))
                               continue;

                           var path = Path.Combine(targetPath, "Library", Path.GetFileName(file));
                           yield return SymLink(file, path);

                       }

                       yield return Copy(Path.Combine(projectPath, "Library", "ArtifactDB"), Path.Combine(targetPath, "Library", "ArtifactDB"));
                       yield return Copy(Path.Combine(projectPath, "Library", "SourceAssetDB"), Path.Combine(targetPath, "Library", "SourceAssetDB"));

                       Task Copy(string path, string destination) =>
                           Task.Run(async () =>
                           {
#if UNITY_EDITOR_WIN
                            await CommandUtility.RunCommandWindows($"copy {path.ToWindowsPath().WithQuotes()} {destination.ToWindowsPath().WithQuotes()}");
#elif UNITY_EDITOR_LINUX
                            await CommandUtility.RunCommandWindows($"cp {path.WithQuotes()} {destination.WithQuotes()}");
#elif UNITY_EDITOR_OSX
                           Debug.LogWarning("OSX not yet supported.");
#endif
                           });

                       Task SymLinkRelative(string relativePath) =>
                           SymLink(
                               linkPath: Path.Combine(targetPath, relativePath),
                               path: Path.Combine(projectPath, relativePath));

                       Task SymLink(string path, string linkPath) =>
                           Task.Run(async () =>
                           {
#if UNITY_EDITOR_WIN
                               await CommandUtility.RunCommandWindows($"mklink {(Directory.Exists(path) ? "/j" : "/h")} {linkPath.ToWindowsPath().WithQuotes()} {path.ToWindowsPath().WithQuotes()}");
#elif UNITY_EDITOR_LINUX
                               await CommandUtility.RunCommandWindows($"ln -s {path.WithQuotes()} {linkPath.WithQuotes()}");
#elif UNITY_EDITOR_OSX
                               Debug.LogWarning("OSX not yet supported.");
#endif
                           });

                   }

               }));

        /// <summary>Deletes a secondary instance from Unity Hub.</summary>
        public static Task DeleteHubEntry(string instancePath, Action onComplete = null, bool hideProgress = false) =>
            ProgressUtility.RunTask(
               displayName: "Deleting hub entry",
               onComplete: (t) => onComplete?.Invoke(),
               hideProgress: hideProgress,
               task: new Task(() =>
               {
                   using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Unity Technologies\Unity Editor 5.x", writable: true))
                       foreach (var name in key.GetValueNames().Where(n => n.StartsWith("RecentlyUsedProjectPaths")))
                       {
                           var value = Encoding.ASCII.GetString((byte[])key.GetValue(name));
                           if (value.StartsWith(instancePath.ToCrossPlatformPath()))
                               key.DeleteValue(name);
                       }
               }));

        /// <summary>Deletes a secondary instance.</summary>
        public static Task Delete(string path, Action onComplete = null, bool hideProgress = false) =>
            ProgressUtility.RunTask(
               displayName: "Removing instance",
               onComplete: (t) => onComplete?.Invoke(),
               hideProgress: hideProgress,
               //Deleting with cmd, which prevents 'Directory not empty error', for Directory.Delete(path, recursive: true)
               task: new Task(async () =>
               {
#if UNITY_EDITOR_WIN
                await CommandUtility.RunCommandWindows($"rmdir /s/q {path.ToWindowsPath().WithQuotes()}");
#elif UNITY_EDITOR_LINUX
                await CommandUtility.RunCommandWindows($"rm -r {path.WithQuotes()}");
#elif UNITY_EDITOR_OSX
                Debug.LogWarning("OSX not yet supported.");
#endif
               }));

    }

}
