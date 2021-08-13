using InstanceManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace InstanceManager.Utility
{

    [ExecuteAlways]
    public static class CrossProcessEventUtility
    {

        #region Watcher

        static CancellationTokenSource token;

        internal static void Initialize(string instancePath)
        {

            if (InstanceManager.isPrimaryInstance)
                return;

            var syncContext = SynchronizationContext.Current;
            token?.Cancel();
            token = new CancellationTokenSource();

            EditorApplication.playModeStateChanged -= OnPlaymodeChanged;
            EditorApplication.playModeStateChanged += OnPlaymodeChanged;

            void OnPlaymodeChanged(PlayModeStateChange state)
            {
                if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.ExitingPlayMode)
                    token?.Cancel();
            }

            Task.Factory.StartNew(
                state: token.Token,
                cancellationToken: token.Token,
                creationOptions: TaskCreationOptions.LongRunning,
                scheduler: TaskScheduler.Default,
                action: async (t) =>
                {

                    var token = (CancellationToken)t;

                    while (!token.IsCancellationRequested)
                    {

                        try
                        {

                            var file = new FileInfo(instancePath);

                            if (file.Exists)
                            {

                                while (file.Length == 0)
                                {
                                    await Task.Delay(1000);

                                    if (token.IsCancellationRequested)
                                        return;
                                    file.Refresh();
                                }

                                syncContext.Post(_ => OnEvent(), null);

                            }

                            await Task.Delay(1000);

                        }
                        catch (Exception ex)
                        {
                            Debug.LogError(ex);
                        }

                    }

                    syncContext.Post(_ => Initialize(instancePath), null);

                });

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

        public static void Send(string name)
        {
            foreach (var instance in InstanceManager.instances.ToArray())
                Send(instance, name);
        }

        public static void Send(UnityInstance instance, string name) =>
            Send(instance.lockPath, name);

        static void Send(string path, string name)
        {

            if (InstanceManager.isSecondaryInstance)
                return;

            //Listeners can be added to primary instance
            RaiseEvent(name);
            var writer = File.AppendText(path);
            writer.WriteLine(name);
            writer.Flush();
            writer.Close();
            writer.Dispose();

        }

        static readonly Dictionary<string, List<Action>> listeners = new Dictionary<string, List<Action>>();
        public static void On(string name, Action action)
        {
            if (!listeners.ContainsKey(name))
                listeners.Add(name, new List<Action>());
            listeners[name].Add(action);
        }

        static void RaiseEvent(string name)
        {
            Debug.Log("Event raised: " + name);
            if (listeners.TryGetValue(name, out var list))
                foreach (var callback in list)
                    callback?.Invoke();
            EditorApplication.QueuePlayerLoopUpdate();
        }

    }

}
