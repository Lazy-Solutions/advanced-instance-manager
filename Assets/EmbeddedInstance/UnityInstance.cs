#pragma warning disable IDE1006 // Naming Styles

using System;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace EmbeddedInstance
{

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
        [SerializeField] private int m_windowHandle;
        [SerializeField] private int m_processID;

        public string ID => m_ID;
        public string path => m_path;
        public IntPtr windowHandle => (IntPtr)m_windowHandle;
        public bool isSettingUp { get; internal set; }

        public Process InstanceProcess { get; private set; }

        public UnityInstancePipe pipe { get; internal set; }

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

        public bool isRunning
        {
            get
            {
                InstanceProcess?.Refresh();
                return !(InstanceProcess?.HasExited ?? true);
            }
        }

        /// <summary>Toggle open. Closes if already open, otherwise opens in default mode, as defined in settings.</summary>
        public void ToggleOpen()
        {
            if (isRunning)
                Close();
            else
                Open();
        }

        /// <summary>Open default mode, as defined in settings.</summary>
        public void Open()
        {
            //TODO: After settings added, add support for switching open mode here
            OpenAsEditor();
        }

        /// <summary>Open as editor, like a regular unity window.</summary>
        public void OpenAsEditor() =>
            Run(gameView: false);

        /// <summary>Open as game view, displaying only game window.</summary>
        public void OpenAsGameView() =>
            Run(gameView: true);

        void Run(bool gameView)
        {

            InstanceProcess = Process.Start(EditorApplication.applicationPath, "-projectPath " + path + " -instanceID:" + ID + (gameView ? " -gameView" : ""));
            InstanceProcess.EnableRaisingEvents = true;
            InstanceProcess.Exited += InstanceProcess_Exited;
            //pipe = UnityInstancePipe.Server(ID, Close);
            InstanceManager.Save();

        }

        private void InstanceProcess_Exited(object sender, EventArgs e)
        {
            Debug.Log("exited");
            InstanceProcess.Exited -= InstanceProcess_Exited;
            Close();
        }

        public void Close()
        {

            if (!InstanceProcess?.HasExited ?? false)
                InstanceProcess?.Kill();
            InstanceProcess = null;

            m_windowHandle = 0;

            pipe?.Dispose();
            pipe = null;

            RemoveUnityHubEntry();

            InstanceManager.Save();
            if (InstanceManagerWindow.window)
            {
                InstanceManagerWindow.window.Repaint();
                EditorApplication.QueuePlayerLoopUpdate();
            }

        }

        void RemoveUnityHubEntry()
        {
            InstanceManager.SymLink("Deleting unity hub entry", "-delHubEntry", path);
        }

    }

}
