using InstanceManager.Models;
using InstanceManager.Utility;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace InstanceManager
{

    /// <summary>The main class of Instance Manager.</summary>
    public static class InstanceManager
    {

        //TODO: Something is wrong which causes local multiplayer to not assign correct ids to each instance
        //TODO: Check out: https://github.com/VeriorPies/ParrelSync/tree/95a062cb14e669c7834094366611765d3a9658d6

        //TODO: Cannot get current window layout anymore
        //TODO: Fix auto sync
        //TODO: Open on right click menu is disabled

        //TODO: After release:
        //TODO: Add multi-platform support
        //TODO: Fix cross process events only raising event once after registered

        /// <summary>The secondary instances that have been to this project.</summary>
        public static IEnumerable<UnityInstance> instances => InstanceUtility.Enumerate();

        /// <summary>The current instance. <see langword="null"/> if primary.</summary>
        public static UnityInstance instance => InstanceUtility.LocalInstance();

        /// <summary>Occurs during startup if current instance is secondary.</summary>
        public static event Action onSecondInstanceStarted;

        /// <summary>Gets if the current instance is the primary instance.</summary>
        public static bool isPrimaryInstance => instance == null;

        /// <summary>Gets if the current instance is a secondary instance.</summary>
        public static bool isSecondInstance => instance != null;

        /// <summary>Gets the id of the current instance. <see langword="null"/> if primary instance.</summary>
        public static string id => instance?.id;

        #region Events

        //Events are currently only raised once after registering, pause and unpause as such useless, but lets keep them for the future
        ///// <summary>Occurs when host is paused.</summary>
        //public static CrossProcessEvent OnHostPause { get; } = new CrossProcessEvent(nameof(OnHostPause));
        ///// <summary>Occurs when host is unpaused.</summary>
        //public static CrossProcessEvent OnHostUnpause { get; } = new CrossProcessEvent(nameof(OnHostUnpause));

        /// <summary>Occurs when host enters play mode.</summary>
        public static CrossProcessEvent OnHostEnterPlayMode { get; } = new CrossProcessEvent(nameof(OnHostEnterPlayMode));

        /// <summary>Occurs when host exits play mode.</summary>
        public static CrossProcessEvent OnHostExitPlayMode { get; } = new CrossProcessEvent(nameof(OnHostExitPlayMode));

        /// <summary>Occurs when host assets change.</summary>
        public static CrossProcessEvent OnAssetsChange { get; } = new CrossProcessEvent(nameof(OnAssetsChange));

        static CrossProcessEvent quitRequest;

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
                ////OnHostPause.InitializeClient();
                ////OnHostUnpause.InitializeClient();
                OnAssetsChange.InitializeClient();

                OnHostEnterPlayMode.AddHandler(() => { if (instance.enterPlayModeAutomatically) EditorApplication.EnterPlaymode(); });
                OnHostExitPlayMode.AddHandler(() => { if (instance.enterPlayModeAutomatically) EditorApplication.ExitPlaymode(); });
                //OnHostPause.AddHandler(() => { if (instance.enterPlayModeAutomatically) EditorApplication.isPaused = true; });
                //OnHostUnpause.AddHandler(() => { if (instance.enterPlayModeAutomatically) EditorApplication.isPaused = false; });
                OnAssetsChange.AddHandler(() => { if (instance.autoSync) SyncWithPrimaryInstance(); });

                quitRequest = new CrossProcessEvent($"QuitRequest ({id})");
                quitRequest.InitializeClient();
                quitRequest.AddHandler(() => EditorApplication.Exit(0));

            }

        }

        #endregion

        [InitializeOnLoadMethod]
        static void OnLoad()
        {

            Debug.Log(id);

            SetupCrossProcessEvents(isPrimaryInstance);

            if (isPrimaryInstance)
                InitializePrimaryInstance();
            else
                InitializeSecondInstance();

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

            await Task.Delay(100);
            WindowLayoutUtility.Find(instance.preferredLayout).Apply();

            //AssetDatabase.DisallowAutoRefresh();
            onSecondInstanceStarted?.Invoke();

        }

        /// <summary>Syncs this instance with the primary instance, does nothing if current instance is primary.</summary>
        public static void SyncWithPrimaryInstance()
        {

            if (isPrimaryInstance)
                return;

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            var setup = EditorSceneManager.GetSceneManagerSetup();
            EditorSceneManager.RestoreSceneManagerSetup(setup);

            CompilationPipeline.RequestScriptCompilation();

        }

    }

}
