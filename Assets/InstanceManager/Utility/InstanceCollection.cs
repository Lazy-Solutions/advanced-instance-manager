using InstanceManager.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            if (!SymLinkUtility.CheckAvailable())
                return null;

            var id = IDUtility.Generate(validate: id => !Directory.Exists(Paths.InstancePath(id)));
            var path = Paths.InstancePath(id);
            var instance = new UnityInstance(id, path)
            {
                isSettingUp = true
            };

            SymLinkUtility.Create("Creating new instance", Paths.project.WithQuotes(), path.WithQuotes(),
                 onComplete: () =>
                 {
                     instance.isSettingUp = false;
                     onComplete?.Invoke();
                 });

            list.Add(instance);
            Save();

            return instance;

        }

        /// <summary>Removes a secondary instance.</summary>
        public void Remove(UnityInstance instance, Action onComplete = null)
        {
            instance.Close();
            instance.isSettingUp = true;
            SymLinkUtility.Delete(progressString: "Deleting instance", instance.path,
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

    }

}
