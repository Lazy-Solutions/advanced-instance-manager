using InstanceManager.Models;
using InstanceManager.Utility;
using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;

namespace InstanceManager
{

    /// <summary>The main class of Instance Manager.</summary>
    public static class InstanceManager
    {

        //TODO: Do we have any listeners for EditorApplication.quit? Unity window close button need multiple clicks to close window?
        //TODO: Add prompt in window to install symlinker if it does not exist
        //TODO: Can we get current window layout? if so, we should prevent layout apply if same

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
            isSecondInstance = !string.IsNullOrWhiteSpace(id);
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

        static async void InitializeSecondInstance()
        {

            //AssetDatabase.DisallowAutoRefresh();

            await Task.Delay(100);
            instance = instances.Find(id);
            WindowLayoutUtility.Find(instance.preferredLayout).Apply();

            onSecondInstanceStarted?.Invoke();

        }

    }

}
