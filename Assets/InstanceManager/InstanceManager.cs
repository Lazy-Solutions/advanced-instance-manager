using InstanceManager.Models;
using InstanceManager.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace InstanceManager
{

    /// <summary>The main class of Instance Manager.</summary>
    public static class InstanceManager
    {

        //TODO: Something is wrong which causes local multiplayer to not assign correct ids to each instance
        //TODO: Check out: https://github.com/VeriorPies/ParrelSync/tree/95a062cb14e669c7834094366611765d3a9658d6

        //TODO: Scenes do not reload on sync with main project
        //TODO: Cache GUIContent and GUIStyle
        //TODO: Allow set scene(s) to auto open
        //TODO: 'Library/LastSceneManagerSetup.txt' can be used to set scene layout for instances
        //TODO: Add event for secondary instances when main instance enters play mode, and add option for automatically entering play mode
        //TODO: Add cross-process event for scripts reloading, reload asset database in instances then

        internal const string idParamName = "-instanceID:";

        /// <summary>The secondary instances that have been to this project.</summary>
        public static InstanceCollection instances { get; private set; }

        /// <summary>The current instance. <see langword="null"/> if primary.</summary>
        public static UnityInstance instance { get; private set; }

        /// <summary>Occurs during startup if current instance is secondary.</summary>
        public static event Action onSecondInstanceStarted;

        /// <summary>Gets if the current instance is the primary instance.</summary>
        public static bool isPrimaryInstance { get; private set; }

        /// <summary>Gets if the current instance is a secondary instance.</summary>
        public static bool isSecondInstance { get; private set; }

        /// <summary>Gets the id of the current instance. <see langword="null"/> if primary.</summary>
        public static string id { get; private set; }

        [InitializeOnLoadMethod]
        static void OnLoad()
        {

            id = Directory.GetParent(Application.dataPath).Name;
            var isEmbedded = Application.dataPath.Contains("/EmbeddedInstances/" + id);

            isSecondInstance = isEmbedded;
            isPrimaryInstance = !isSecondInstance;

            instances = new InstanceCollection();
            instances.Reload();
            instance = instances.Find(id);

            SetupCrossProcessEvents(isPrimaryInstance);

            if (isEmbedded && instance is null)
            {
                Debug.LogError("This instance is embedded, but no associated instance metadata could be found, Instance Manager initialization has been stopped.");
                return;
            }

            if (isPrimaryInstance)
                InitializePrimaryInstance();
            else
                InitializeSecondInstance();

        }

        public class CrossProcessEvent
        {

            public CrossProcessEvent(string name) =>
                this.name = name;

            public bool isInitialized => waitHandle != null;
            public bool isHost { get; private set; }
            public string name { get; }

            EventWaitHandle waitHandle;
            CancellationTokenSource clientWaitToken;

            readonly List<Action> actions = new List<Action>();

            public void AddHandler(Action action) => actions.Add(action);
            public void RemoveHandler(Action action) => actions.Remove(action);

            public void RaiseEvent()
            {

                if (!isHost)
                    throw new Exception("Cross-process events can only be raised on host.");

                Debug.Log($"{name}: Raising event.");
                waitHandle.Set();

                //Call callbacks on host, not primary use case, but why not? its extra code either way (we'd need a check in AddHandler otherwise)
                CallCallbacks();

            }

            void CallCallbacks()
            {
                Debug.Log($"{name} occured (background thread)");
                EditorApplication.update += NextFrame;
                EditorApplication.QueuePlayerLoopUpdate();
                void NextFrame()
                {
                    EditorApplication.update -= NextFrame;
                    Debug.Log($"{name} occured");
                    foreach (var callback in actions)
                        callback?.Invoke();
                }
            }

            public void InitializeHost()
            {

                if (isInitialized)
                    return;
                isHost = true;

                // create a rule that allows anybody in the "Users" group to synchronise with us
                var users = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
                var rule = new EventWaitHandleAccessRule(users, EventWaitHandleRights.Synchronize | EventWaitHandleRights.Modify, AccessControlType.Allow);
                var security = new EventWaitHandleSecurity();
                security.AddAccessRule(rule);

                waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, @"Global\InstanceManager." + name, out var created, security);
                waitHandle.Reset();

                Debug.Log($"{name}: Registered event as host ({nameof(created)}:{created}).");

            }

            public async void InitializeClient()
            {

                if (isInitialized)
                    return;
                isHost = false;

                await Task.Delay(1000);

                waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, @"Global\InstanceManager." + name, out var created);
                if (created)
                    throw new Exception("Cannot subscribe to a cross-process event if no host has registered it.");

                clientWaitToken?.Cancel();
                clientWaitToken = new CancellationTokenSource();

                _ = Task.Factory.StartNew(WaitUntilSignalled, TaskCreationOptions.LongRunning);
                Debug.Log($"{name}: Registered event as client.");

            }

            void WaitUntilSignalled()
            {
                while (true)
                {

                    if (waitHandle.WaitOne(500))
                        CallCallbacks();
                    if (waitHandle is null || clientWaitToken.IsCancellationRequested)
                        return;

                }
            }

        }

        public static CrossProcessEvent OnHostEnterPlayMode { get; } = new CrossProcessEvent(nameof(OnHostEnterPlayMode));
        public static CrossProcessEvent OnHostExitPlayMode { get; } = new CrossProcessEvent(nameof(OnHostExitPlayMode));
        //public static CrossProcessEvent OnHostPause { get; } = new CrossProcessEvent(nameof(OnHostPause));
        //public static CrossProcessEvent OnHostUnpause { get; } = new CrossProcessEvent(nameof(OnHostUnpause));
        public static CrossProcessEvent OnAssetsChange { get; } = new CrossProcessEvent(nameof(OnAssetsChange));

        class AssetsChangedCallback : AssetPostprocessor
        {

            static void OnPostprocessAllAssets(string[] _, string[] _1, string[] _2, string[] _3)
            {
                if (isPrimaryInstance)
                    OnAssetsChange.RaiseEvent();
            }

        }

        static void SetupCrossProcessEvents(bool isPrimary)
        {

            if (isPrimary)
            {

                OnHostEnterPlayMode.InitializeHost();
                OnHostExitPlayMode.InitializeHost();
                //OnHostPause.InitializeHost();
                //OnHostUnpause.InitializeHost();
                OnAssetsChange.InitializeHost();

                EditorApplication.playModeStateChanged += (state) =>
                {
                    if (state == PlayModeStateChange.ExitingEditMode)
                        OnHostEnterPlayMode.RaiseEvent();
                    else if (state == PlayModeStateChange.ExitingPlayMode)
                        OnHostExitPlayMode.RaiseEvent();
                };


                //EditorApplication.pauseStateChanged += (state) =>
                //{
                //    if (state == PauseState.Paused)
                //        OnHostPause.RaiseEvent();
                //    else if (state == PauseState.Unpaused)
                //        OnHostUnpause.RaiseEvent();
                //};

            }
            else
            {

                OnHostEnterPlayMode.InitializeClient();
                OnHostExitPlayMode.InitializeClient();
                //OnHostPause.InitializeClient();
                //OnHostUnpause.InitializeClient();
                OnAssetsChange.InitializeClient();

                OnHostEnterPlayMode.AddHandler(() => { if (instance.enterPlayModeAutomatically) EditorApplication.EnterPlaymode(); });
                OnHostExitPlayMode.AddHandler(() => { if (instance.enterPlayModeAutomatically) EditorApplication.ExitPlaymode(); });
                //OnHostPause.AddHandler(() => { if (instance.enterPlayModeAutomatically) EditorApplication.isPaused = true; });
                //OnHostUnpause.AddHandler(() => { if (instance.enterPlayModeAutomatically) EditorApplication.isPaused = false; });
                OnAssetsChange.AddHandler(() => { if (instance.autoSync) AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate); });

            }

            EditorApplication.QueuePlayerLoopUpdate();
            EditorApplication.Step();

        }

        static void InitializePrimaryInstance()
        {

            OnAssetsChange.RaiseEvent();

            EditorApplication.wantsToQuit += () =>
            {
                foreach (var instance in instances)
                    instance?.Close();
                return true;
            };

        }

        static async void InitializeSecondInstance()
        {

            Debug.Log(id);

            await Task.Delay(100);
            WindowLayoutUtility.Find(instance.preferredLayout).Apply();

            AssetDatabase.DisallowAutoRefresh();
            onSecondInstanceStarted?.Invoke();

        }

    }

}
