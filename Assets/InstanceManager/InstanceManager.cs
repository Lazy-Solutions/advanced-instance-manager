using InstanceManager.Models;
using InstanceManager.Utility;
using System;
using System.IO;
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

        //TODO: 'Library/LastSceneManagerSetup.txt' can be used to set scene layout for instances
        //TODO: Remove instance when 'No' is chosen at UAC prompt before running symlinker, and also if symlinker is killed (from task manager)
        //TODO: Add menu button that opens instance context menu
        //TODO: Cannot get current window layout anymore
        //TODO: Check if we can't create symlinks without admin? ParrelSync seems to be able to do that?

        //TODO: After release:
        //TODO: Add multi-platform support

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

        public static CrossProcessEvent OnHostEnterPlayMode { get; } = new CrossProcessEvent(nameof(OnHostEnterPlayMode));
        public static CrossProcessEvent OnHostExitPlayMode { get; } = new CrossProcessEvent(nameof(OnHostExitPlayMode));
        public static CrossProcessEvent OnHostPause { get; } = new CrossProcessEvent(nameof(OnHostPause));
        public static CrossProcessEvent OnHostUnpause { get; } = new CrossProcessEvent(nameof(OnHostUnpause));
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
                OnHostPause.InitializeHost();
                OnHostUnpause.InitializeHost();
                OnAssetsChange.InitializeHost();

                EditorApplication.playModeStateChanged += (state) =>
                {
                    if (state == PlayModeStateChange.ExitingEditMode)
                        OnHostEnterPlayMode.RaiseEvent();
                    else if (state == PlayModeStateChange.ExitingPlayMode)
                        OnHostExitPlayMode.RaiseEvent();
                };

                EditorApplication.pauseStateChanged += (state) =>
                {
                    if (state == PauseState.Paused)
                        OnHostPause.RaiseEvent();
                    else if (state == PauseState.Unpaused)
                        OnHostUnpause.RaiseEvent();
                };

            }
            else
            {

                OnHostEnterPlayMode.InitializeClient();
                OnHostExitPlayMode.InitializeClient();
                OnHostPause.InitializeClient();
                OnHostUnpause.InitializeClient();
                OnAssetsChange.InitializeClient();

                OnHostEnterPlayMode.AddHandler(() => { if (instance.enterPlayModeAutomatically) EditorApplication.EnterPlaymode(); });
                OnHostExitPlayMode.AddHandler(() => { if (instance.enterPlayModeAutomatically) EditorApplication.ExitPlaymode(); });
                OnHostPause.AddHandler(() => { if (instance.enterPlayModeAutomatically) EditorApplication.isPaused = true; });
                OnHostUnpause.AddHandler(() => { if (instance.enterPlayModeAutomatically) EditorApplication.isPaused = false; });
                OnAssetsChange.AddHandler(() => { if (instance.autoSync) SyncWithPrimaryInstance(); });

                quitRequest = new CrossProcessEvent($"QuitRequest ({id})");
                quitRequest.InitializeClient();
                quitRequest.AddHandler(() => EditorApplication.Exit(0));

            }

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

            //AssetDatabase.DisallowAutoRefresh();
            onSecondInstanceStarted?.Invoke();

        }


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
