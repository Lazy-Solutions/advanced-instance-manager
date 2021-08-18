using InstanceManager.Models;
using InstanceManager.Utility;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace InstanceManager
{

    /// <summary>The main class of Instance Manager.</summary>
    public static class InstanceManager
    {

        //TODO: Something is wrong which causes local multiplayer to not assign correct ids to each instance
        //TODO: Check out: https://github.com/VeriorPies/ParrelSync/tree/95a062cb14e669c7834094366611765d3a9658d6

        //TODO: Clean up code
        //TODO: Fix fonts
        //TODO: Add multi-platform support

        /// <summary>The secondary instances that have been to this project.</summary>
        public static IEnumerable<UnityInstance> instances => InstanceUtility.Enumerate();

        /// <summary>The current instance. <see langword="null"/> if primary.</summary>
        public static UnityInstance instance { get; } = InstanceUtility.LocalInstance();

        /// <summary>Occurs during startup if current instance is secondary.</summary>
        public static event Action onSecondInstanceStarted;

        /// <summary>Gets if the current instance is the primary instance.</summary>
        public static bool isPrimaryInstance => instance == null;

        /// <summary>Gets if the current instance is a secondary instance.</summary>
        public static bool isSecondaryInstance => instance != null;

        static string m_ID;
        /// <summary>Gets the id of the current instance.</summary>
        public static string id =>
            isPrimaryInstance
            ? m_ID
            : instance?.id;

        #region Events

        public static event Action OnPrimaryPause;
        public static event Action OnPrimaryUnpause;
        public static event Action OnPrimaryEnterPlayMode;
        public static event Action OnPrimaryExitPlayMode;
        public static event Action OnPrimaryAssetsChanged;

        class AssetsChangedCallback : AssetPostprocessor
        {

            static void OnPostprocessAllAssets(string[] _, string[] _1, string[] _2, string[] _3)
            {
                if (isPrimaryInstance)
                    CrossProcessEventUtility.Send(nameof(OnPrimaryAssetsChanged));
            }

        }

        static void SetupCrossProcessEvents(bool isPrimary)
        {

            if (isPrimary)
            {

                EditorApplication.playModeStateChanged += (state) =>
                {

                    if (state == PlayModeStateChange.ExitingEditMode)
                        CrossProcessEventUtility.Send(nameof(OnPrimaryEnterPlayMode));
                    else if (state == PlayModeStateChange.ExitingPlayMode)
                        CrossProcessEventUtility.Send(nameof(OnPrimaryExitPlayMode));
                };

                EditorApplication.pauseStateChanged += (state) =>
                {
                    if (state == PauseState.Paused)
                        CrossProcessEventUtility.Send(nameof(OnPrimaryPause));
                    else if (state == PauseState.Unpaused)
                        CrossProcessEventUtility.Send(nameof(OnPrimaryUnpause));
                };

            }
            else
            {

                CrossProcessEventUtility.Initialize();

                CrossProcessEventUtility.On(nameof(OnPrimaryEnterPlayMode), () => OnPrimaryEnterPlayMode?.Invoke());
                CrossProcessEventUtility.On(nameof(OnPrimaryExitPlayMode), () => OnPrimaryExitPlayMode?.Invoke());
                CrossProcessEventUtility.On(nameof(OnPrimaryPause), () => OnPrimaryPause?.Invoke());
                CrossProcessEventUtility.On(nameof(OnPrimaryUnpause), () => OnPrimaryUnpause?.Invoke());
                CrossProcessEventUtility.On(nameof(OnPrimaryAssetsChanged), () => OnPrimaryAssetsChanged?.Invoke());

                OnPrimaryEnterPlayMode += () => { if (instance.enterPlayModeAutomatically) EditorApplication.EnterPlaymode(); };
                OnPrimaryExitPlayMode += () => { if (instance.enterPlayModeAutomatically) EditorApplication.ExitPlaymode(); };
                OnPrimaryPause += () => { if (instance.enterPlayModeAutomatically) EditorApplication.isPaused = true; };
                OnPrimaryUnpause += () => { if (instance.enterPlayModeAutomatically) EditorApplication.isPaused = false; };
                OnPrimaryAssetsChanged += () => { if (instance.autoSync) SyncWithPrimaryInstance(); };

                CrossProcessEventUtility.On("Quit", () => EditorApplication.Exit(0));

            }

        }

        #endregion

        [InitializeOnLoadMethod]
        static void OnLoad()
        {

#if UNITY_EDITOR_WIN
            WindowUtility.Initialize();
#endif

            if (isPrimaryInstance)
                InitializePrimaryInstance();
            else
                InitializeSecondInstance();

            SetupCrossProcessEvents(isPrimaryInstance);

        }

        static void InitializePrimaryInstance()
        {

            m_ID = PlayerPrefs.GetString("InstanceManager.PrimaryID", null);
            if (string.IsNullOrWhiteSpace(m_ID))
                PlayerPrefs.SetString("InstanceManager.PrimaryID", m_ID = IDUtility.Generate());

            EditorApplication.wantsToQuit += () =>
            {
                foreach (var instance in instances)
                    instance?.Close();
                return true;
            };

        }

        static async void InitializeSecondInstance()
        {

            if (!InstanceUtility.IsLocked())
            {
                await Task.Delay(100);
                WindowLayoutUtility.Find(instance.preferredLayout).Apply();
                InstanceUtility.SetLocked(true);
            }

            EditorApplication.quitting += () =>
            {
                InstanceUtility.SetLocked(false);
            };
#if UNITY_EDITOR_WIN
            EditorApplication.playModeStateChanged += (state) =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode)
                    WindowUtility.StopTaskbarFromFlashing();
            };
#endif
            onSecondInstanceStarted?.Invoke();

        }

        /// <summary>Sync this instance with the primary instance, does nothing if current instance is primary.</summary>
        public static void SyncWithPrimaryInstance()
        {

            if (isPrimaryInstance || Application.isPlaying)
                return;

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            SceneUtility.ReloadScenes();
            CompilationPipeline.RequestScriptCompilation();

        }

    }

}
