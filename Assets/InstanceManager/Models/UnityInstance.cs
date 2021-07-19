using InstanceManager._Editor;
using InstanceManager.Utility;
using System;
using System.Diagnostics;
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

        public UnityInstance(string id, string path)
        {
            m_ID = id;
            m_path = path;
        }

        [SerializeField] private string m_ID;
        [SerializeField] private string m_path;
        [SerializeField] private int m_processID;
        [SerializeField] private string m_preferredLayout = "Default";
        [SerializeField] private bool m_autoSync = true;
        [SerializeField] private bool m_enterPlayModeAutomatically = true;

        public static event Action autoSyncChanged;

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

        /// <summary>Gets whatever this instance is running.</summary>
        public bool isRunning
        {
            get
            {
                InstanceProcess?.Refresh();
                return !(InstanceProcess?.HasExited ?? true);
            }
        }

        /// <summary>Gets the id of this instance.</summary>
        public string ID => m_ID;

        /// <summary>Gets the path of this instance.</summary>
        public string path => m_path;

        /// <summary>Gets if the instance is currently being set up.</summary>
        public bool isSettingUp { get; internal set; }

        /// <summary>Gets the process of this instance, if it is running.</summary>
        public Process InstanceProcess { get; private set; }

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

        /// <summary>Open if not running, othewise close.</summary>
        public void ToggleOpen()
        {
            if (isRunning)
                Close();
            else
                Open();
        }

        /// <summary>Open instance.</summary>
        public void Open()
        {

            InstanceProcess = Process.Start(new ProcessStartInfo(
                fileName: EditorApplication.applicationPath,
                arguments:
                    InstanceManager.idParamName + ID +
                    " -projectPath " + path.WithQuotes()));

            InstanceProcess.EnableRaisingEvents = true;
            InstanceProcess.Exited += InstanceProcess_Exited;
            InstanceManager.instances.Save();

        }

        void InstanceProcess_Exited(object sender, EventArgs e)
        {
            //Debug.Log("exited");
            Close();
        }

        public void Close()
        {

            if (InstanceProcess != null)
            {
                InstanceProcess.Exited -= InstanceProcess_Exited;
                if (!InstanceProcess.HasExited)
                    InstanceProcess?.Kill();
                InstanceProcess = null;
            }

            SymLinkUtility.DeleteHubEntry("Deleting unity hub entry", path);

            InstanceManager.instances.Save();
            if (InstanceManagerWindow.window)
            {
                InstanceManagerWindow.window.Repaint();
                EditorApplication.QueuePlayerLoopUpdate();
            }

        }

    }

}
