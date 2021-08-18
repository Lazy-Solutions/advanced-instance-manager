using InstanceManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace InstanceManager.Utility
{

    /// <summary>Provides utility functions for sending 'events' to secondary instances.</summary>
    public static class CrossProcessEventUtility
    {

        #region Watcher

        /// <summary>Initializes the event listener, for a secondary instance.</summary>
        internal static void Initialize()
        {

            if (InstanceManager.isPrimaryInstance)
                return;

            var instancePath = InstanceManager.instance.lockPath;
            var file = new FileInfo(instancePath);

            DateTime lastUpdate = DateTime.Now;
            EditorApplication.update -= Update;
            EditorApplication.update += Update;

            void Update()
            {

                if (DateTime.Now - lastUpdate < TimeSpan.FromSeconds(0.5))
                    return;
                lastUpdate = DateTime.Now;

                file.Refresh();
                if (file.Length > 0)
                    OnEvent();

            }

        }

        static void OnEvent()
        {

            if (!File.Exists(InstanceManager.instance.lockPath) || new FileInfo(InstanceManager.instance.lockPath).Length == 0)
                return;

            var str = File.ReadAllText(InstanceManager.instance.lockPath);
            File.WriteAllText(InstanceManager.instance.lockPath, null);

            var names = str.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var name in names.Distinct().ToArray())
                RaiseEvent(name);

        }

        #endregion

        /// <summary>Sends an event to all open secondary instances.</summary>
        public static void Send(string name)
        {
            foreach (var instance in InstanceManager.instances.ToArray())
                Send(instance, name);
        }

        /// <summary>Sends an event to the specified secondary instance.</summary>
        public static void Send(UnityInstance instance, string name) =>
            Send(instance.lockPath, name);

        static void Send(string path, string name)
        {

            if (InstanceManager.isSecondaryInstance || !File.Exists(path))
                return;

            //Listeners can be added to primary instance
            RaiseEvent(name);
            using (var writer = File.AppendText(path))
                writer.WriteLine(name);

        }

        static readonly Dictionary<string, List<Action>> listeners = new Dictionary<string, List<Action>>();

        /// <summary>Adds a listener to the specified event.</summary>
        public static void On(string name, Action action)
        {
            if (!listeners.ContainsKey(name))
                listeners.Add(name, new List<Action>());
            listeners[name].Add(action);
        }

        static void RaiseEvent(string name)
        {

            if (listeners.TryGetValue(name, out var list))
                foreach (var callback in list)
                    callback?.Invoke();

        }

    }

}
