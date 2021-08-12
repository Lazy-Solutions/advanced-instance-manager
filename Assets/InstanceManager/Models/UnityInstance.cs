using InstanceManager.Editor;
using InstanceManager.Utility;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace InstanceManager.Models
{

    /// <summary>Represents a secondary unity instance.</summary>
    [Serializable]
    public class UnityInstance : ISerializationCallbackReceiver
    {

        public UnityInstance()
        { }

        public UnityInstance(string id)
        {
            m_ID = id;
            needsRepair = InstanceUtility.NeedsRepair(this);
        }

        #region Properties

        [SerializeField] private string m_ID;
        [SerializeField] private int m_processID;
        [SerializeField] private string m_preferredLayout = "Default";
        [SerializeField] private bool m_autoSync = true;
        [SerializeField] private bool m_enterPlayModeAutomatically = true;
        [SerializeField] private string[] m_scenes;

        /// <summary>The paths to this instance file.</summary>
        internal string filePath => Paths.InstancePath(id) + "/" + InstanceUtility.instanceFileName;

        /// <summary>Gets if this process needs repairing.</summary>
        public bool needsRepair { get; }

        /// <summary>Gets or sets the window layout.</summary>
        public string preferredLayout
        {
            get => m_preferredLayout;
            set => m_preferredLayout = value;
        }

        /// <summary>Gets or sets whatever this instance should auto sync asset changes.</summary>
        public bool autoSync
        {
            get => m_autoSync;
            set
            {
                var prevValue = m_autoSync;
                m_autoSync = value;
                if (prevValue != value)
                    autoSyncChanged?.Invoke();
            }
        }

        /// <summary>Gets or sets whatever this instance should enter / exit play mode automatically when primary instance does.</summary>
        public bool enterPlayModeAutomatically
        {
            get => m_enterPlayModeAutomatically;
            set => m_enterPlayModeAutomatically = value;
        }

        /// <summary>Gets the scenes this instance should open when starting.</summary>
        public string[] scenes
        {
            get => m_scenes;
            set => m_scenes = value;
        }

        /// <summary>Saves the instance settings to disk.</summary>
        public void Save() =>
            InstanceUtility.Save(this);

        /// <summary>Removes the instance from disk.</summary>
        public void Remove(Action onComplete = null) =>
            InstanceUtility.Remove(this, onComplete);

        /// <summary>Gets whatever this instance is running.</summary>
        public bool isRunning
        {
            get
            {
                InstanceProcess?.Refresh();
                return InstanceProcess != null && !InstanceProcess.HasExited;
            }
        }

        /// <summary>Gets the id of this instance.</summary>
        public string id => m_ID;

        /// <summary>Gets the path of this instance.</summary>
        public string path => Paths.InstancePath(id);

        /// <summary>Gets if the instance is currently being set up.</summary>
        public bool isSettingUp => InstanceUtility.IsInstanceBeingSetUp(this);

        /// <summary>Gets the process of this instance, if it is running.</summary>
        public Process InstanceProcess { get; private set; }

        #endregion
        #region ISerializationCallbackReceiver

        public void OnBeforeSerialize()
        {
            if (InstanceProcess != null)
            {
                m_processID = InstanceProcess.Id;
                InstanceProcess.Exited -= InstanceProcess_Exited;
            }
            else
                m_processID = 0;
        }

        public void OnAfterDeserialize()
        {

            try
            {
                if (m_processID > 0)
                {
                    InstanceProcess = Process.GetProcessById(m_processID);
                    InstanceProcess.Exited += InstanceProcess_Exited;
                }
            }
            catch
            { }

            m_processID = -1;

        }

        #endregion

        internal static event Action autoSyncChanged;

        /// <summary>Refreshes this <see cref="UnityInstance"/>.</summary>
        public void Refresh() =>
            InstanceUtility.Refresh(this);

        /// <summary>Set property of scene.</summary>
        /// <param name="enabled">Set whatever this scene is enabled or not.</param>
        /// <param name="index">Set the index of this scene.</param>
        public void SetScene(string path, bool? enabled = null, int? index = null)
        {

            m_scenes ??= Array.Empty<string>();
            if (enabled.HasValue)
            {
                if (enabled.Value && !m_scenes.Contains(path))
                    ArrayUtility.Add(ref m_scenes, path);
                else if (m_scenes.Contains(path))
                    ArrayUtility.Remove(ref m_scenes, path);
            }

            if (index.HasValue && m_scenes.Contains(path))
            {
                index = Mathf.Clamp(index.Value, 0, m_scenes.Length - 1);
                ArrayUtility.Remove(ref m_scenes, path);
                ArrayUtility.Insert(ref m_scenes, index.Value, path);
            }

        }

        /// <summary>Open if not running, othewise close.</summary>
        public void ToggleOpen()
        {
            if (isRunning)
                Close();
            else
                Open();
        }

        CrossProcessEvent quitRequest;
        /// <summary>Open instance.</summary>
        public void Open()
        {

            if (InstanceManager.isSecondInstance || isRunning || needsRepair)
                return;

            SetupScenes();
            quitRequest = new CrossProcessEvent($"QuitRequest ({id})");
            quitRequest.InitializeHost();
            InstanceProcess = Process.Start(new ProcessStartInfo(
                fileName: EditorApplication.applicationPath,
                arguments: "-projectPath " + path.WithQuotes()));

            Save();
            InstanceProcess.EnableRaisingEvents = true;
            InstanceProcess.Exited += InstanceProcess_Exited;

        }

        void SetupScenes()
        {

            var root = "sceneSetups:";
            bool isFirstScene = true;


            string GetSceneString(string scenePath)
            {

                var str =
                    "- path: " + scenePath + Environment.NewLine +
                    "  isLoaded: 1" + Environment.NewLine +
                    "  isActive: " + (isFirstScene ? "1" : "0") + Environment.NewLine +
                    "  isSubScene: 0";

                isFirstScene = false;
                return str;

            }

            var yaml = root + Environment.NewLine + string.Join(Environment.NewLine, scenes?.Select(GetSceneString) ?? Array.Empty<string>());
            File.WriteAllText(Path.Combine(path, "Library", "LastSceneManagerSetup.txt"), yaml);

        }

        void InstanceProcess_Exited(object sender, EventArgs e) =>
            OnClosed();

        /// <summary>Closes this instance.</summary>
        public void Close() =>
            Close(null);

        /// <summary>Closes this instance.</summary>
        public void Close(Action onClosed = null)
        {

            if (InstanceManager.isSecondInstance || !isRunning || InstanceProcess is null)
                return;

            //Lets copy variable, since if we use property when killing process after 5
            //seconds we'll end up killing new instance process, if one started
            var process = InstanceProcess;

            process.Exited -= InstanceProcess_Exited;
            if (!process.HasExited)
            {

                //Send quit request since unity won't save settings unless EditorApplication.Exit() is called.
                //Process.Close() does nothing and Process.CloseMainWindow() closes, but does not save
                quitRequest = new CrossProcessEvent($"QuitRequest ({id})");
                quitRequest.InitializeHost();

                process.Exited += Exited;
                quitRequest.RaiseEvent();

                //In the off chance that the event was not registered in the secondary instance, lets kill process after 5 seconds
                _ = Task.Run(async () =>
                {
                    await Task.Delay(5000);
                    process?.Kill();
                });

                void Exited(object sender, EventArgs e)
                {
                    process.Exited -= Exited;
                    OnClosed();
                }

            }
            else
                OnClosed();

            void OnClosed()
            {

                if (InstanceProcess == process)
                    InstanceProcess = null;

                this.OnClosed();

                EditorApplication.delayCall += () =>
                onClosed?.Invoke();

            }

        }

        void OnClosed()
        {

            SymLinkUtility.DeleteHubEntry(path);

            Save();
            if (InstanceManagerWindow.window)
            {
                InstanceManagerWindow.window.Repaint();
                EditorApplication.QueuePlayerLoopUpdate();
            }

        }

    }

}
