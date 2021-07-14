using InstanceManager.Models;
using InstanceManager.Utility;
using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace InstanceManager
{

    /// <summary>The main class of Instance Manager.</summary>
    public static class InstanceManager
    {

        //TODO: Do we have any listeners for EditorApplication.quit? Unity window close button need multiple clicks to close window?
        //TODO: Check '-hubSessionId' if we can use that to remove secondary instance from hub

        //TODO: Something is wrong which causes local multiplayer to not assign correct ids to each instance

        //TODO: Check out: https://github.com/VeriorPies/ParrelSync/tree/95a062cb14e669c7834094366611765d3a9658d6

        //TODO: Allow set scene(s) to auto open
        //TODO: Scenes do not reload on sync with main project
        //TODO: Cache GUIContent and GUIStyle
        //TODO: 'Library/LastSceneManagerSetup.txt' can be used to set scene layout for instances
        //TODO: Push symlinker to a separate repo and set it up so that exe can be downloaded directly from there, present download link to user if not installed

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

            id = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith(idParamName))?.Replace(idParamName, "");
            isSecondInstance = !string.IsNullOrWhiteSpace(id) && Application.dataPath.Contains("/EmbeddedInstances/" + id);
            isPrimaryInstance = !isSecondInstance;

            instances = new InstanceCollection();
            instances.Reload();

            if (isPrimaryInstance)
                InitializePrimaryInstance();
            else
                InitializeSecondInstance();

        }


        static void InitializePrimaryInstance()
        {
            EditorApplication.wantsToQuit += () =>
            {
                foreach (var instance in instances)
                    instance?.Close();
                return true;
            };
        }

        static bool hasSetAutoRefresh;
        static async void InitializeSecondInstance()
        {

            await Task.Delay(100);
            instance = instances.Find(id);
            WindowLayoutUtility.Find(instance.preferredLayout).Apply();

            UpdateAutoSync();
            instance.autoSyncChanged += UpdateAutoSync;

            onSecondInstanceStarted?.Invoke();

            void UpdateAutoSync()
            {
                if (!instance.autoSync && !hasSetAutoRefresh)
                {
                    AssetDatabase.DisallowAutoRefresh();
                    hasSetAutoRefresh = true;
                }
                else if (hasSetAutoRefresh)
                {
                    AssetDatabase.AllowAutoRefresh();
                    hasSetAutoRefresh = false;
                }
            }

        }

    }

}
