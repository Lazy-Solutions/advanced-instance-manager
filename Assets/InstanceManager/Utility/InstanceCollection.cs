using InstanceManager.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace InstanceManager.Utility
{

    /// <summary>
    /// <para>A collection of instances.</para>
    /// <para>Usage: <see cref="InstanceManager.instances"/>.</para>
    /// </summary>
    [Serializable]
    public class InstanceCollection : IReadOnlyList<UnityInstance>
    {

        internal InstanceCollection()
        { }

        [SerializeField] List<UnityInstance> list = new List<UnityInstance>();

        public UnityInstance this[int index] =>
            ((IReadOnlyList<UnityInstance>)list)[index];

        public int Count =>
            ((IReadOnlyCollection<UnityInstance>)list).Count;

        public IEnumerator<UnityInstance> GetEnumerator() =>
            ((IEnumerable<UnityInstance>)list).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            ((IEnumerable)list).GetEnumerator();

        /// <summary>Finds the secondary instance with the specified id.</summary>
        public UnityInstance Find(string id) =>
            list.FirstOrDefault(i => i.id == id);

        /// <summary>Saves the instance meta data to disk.</summary>
        public void Save()
        {
            var json = JsonUtility.ToJson(this);
            Directory.GetParent(Paths.listPath).Create();
            File.WriteAllText(Paths.listPath, json);
        }

        static InstanceCollection Load()
        {

            if (!File.Exists(Paths.listPath))
                return null;

            var json = File.ReadAllText(Paths.listPath);
            var instances = JsonUtility.FromJson<InstanceCollection>(json);
            return instances;

        }

        /// <summary>Reloads instance meta data from disk.</summary>
        public void Reload()
        {
            if (list is null)
                list = new List<UnityInstance>();
            list.Clear();
            if (Load() is InstanceCollection collection)
                list.AddRange(collection);
        }

        /// <summary>Create a new secondary instance.</summary>
        public UnityInstance Create(Action onComplete = null)
        {

            var id = IDUtility.Generate(validate: id => !Directory.Exists(Paths.InstancePath(id)));
            var path = Paths.InstancePath(id);
            var instance = new UnityInstance(id)
            {
                isSettingUp = true
            };

            SymLinkUtility.Create(Paths.project, path,
                 onComplete: () =>
                 {
                     instance.isSettingUp = false;
                     onComplete?.Invoke();
                 });

            list.Add(instance);
            Save();

            return instance;

        }

        /// <summary>Removes a secondary instance. Instance has to be closed.</summary>
        public void Remove(UnityInstance instance, Action onComplete = null)
        {

            if (instance.isRunning)
                throw new Exception("Cannot remove instance while running!");

            instance.isSettingUp = true;

            SymLinkUtility.Delete(instance.path,
                onComplete: () =>
                {
                    list.Remove(instance);
                    Save();
                    onComplete?.Invoke();
                });

        }

        /// <summary>Updates the instance properties. This makes sure that the correct c# instance of the object is up-to-date, with the specified object.</summary>
        public void Update(UnityInstance instance)
        {
            var inList = Find(instance.id);
            inList.preferredLayout = instance.preferredLayout;
            inList.autoSync = instance.autoSync;
            inList.enterPlayModeAutomatically = instance.enterPlayModeAutomatically;
            inList.scenes = instance.scenes;
            Save();
        }

        /// <summary>Gets if the instances are currently being moved. Instance Manager window and a lot of APIs will be unavailable when true.</summary>
        public bool isMovingInstances { get; private set; }

        /// <summary>Moves the instances to a new path. Folder must exist before calling this method and all instances must be closed.</summary>
        public Task MoveInstancesPath(string newPath, Action onComplete = null) =>
            MoveInstancesPath(Paths.instancesPath, newPath, onComplete);

#pragma warning disable CS0612 // Type or member is obsolete
        Task MoveInstancesPath(string currentPath, string newPath, Action onComplete = null) =>
            ProgressUtility.RunTask(
                displayName: "Moving instances",
                canRun: !isMovingInstances && Directory.Exists(newPath) && currentPath != newPath,
                onComplete: (t) => { if (!t.IsFaulted) Paths.instancesPath = newPath; isMovingInstances = false; Save(); onComplete?.Invoke(); },
                task: new Task(async () =>
               {

                   if (!Directory.Exists(currentPath))
                       return; //Nothing to move, but we need to set Paths.instancesPath (which is set above in onComplete:)

                   if (InstanceManager.instances.Any(i => i.isRunning))
                       throw new Exception("All instances must be closed before moving instances path!");

                   isMovingInstances = true;

                   await CommandUtility.RunCommand("rmdir /s/q " + newPath.ToWindowsPath().WithQuotes());
                   await CommandUtility.RunCommand($"move {currentPath.ToWindowsPath().WithQuotes()} {newPath.ToWindowsPath().WithQuotes()}");

                   var deletePath = currentPath.Replace(Paths.instancesPathSuffix, "");
                   if (!newPath.Contains(deletePath) && CanDelete(deletePath))
                       await CommandUtility.RunCommand("rmdir /s/q " + currentPath.Replace(Paths.instancesPathSuffix, "").ToWindowsPath().WithQuotes());
                   else
                       await CommandUtility.RunCommand("rmdir /s/q " + currentPath.Replace(InstanceManager.id, "").ToWindowsPath().WithQuotes());

                   isMovingInstances = false;

               }));
#pragma warning restore CS0612 // Type or member is obsolete

        static bool CanDelete(string folder)
        {
            try
            {
                return Directory.Exists(folder) &&
                    !Directory.GetFileSystemEntries(folder, "*", SearchOption.AllDirectories).Select(f => Path.GetFileName(f)).
                    Except(new[] { "Instance Manager", InstanceManager.id }).
                    Any();
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }


    }

}
