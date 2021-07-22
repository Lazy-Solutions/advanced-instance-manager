using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace InstanceManager.Utility
{

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

            waitHandle.Dispose();
            waitHandle = null;
            InitializeHost();

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

        void CreateWaitEvent()
        {

            // create a rule that allows anybody in the "Users" group to synchronise with us
            var users = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
            var rule = new EventWaitHandleAccessRule(users, EventWaitHandleRights.Synchronize | EventWaitHandleRights.Modify, AccessControlType.Allow);
            var security = new EventWaitHandleSecurity();
            security.AddAccessRule(rule);

            waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, @"Global\InstanceManager." + name, out _, security);
            if (isHost)
                waitHandle.Reset();

            Debug.Log($"{name}: Registered event as {(isHost ? "host" : "client")}.");

        }

        public void InitializeHost()
        {

            if (isInitialized)
                return;
            isHost = true;

            CreateWaitEvent();

        }

        public void InitializeClient()
        {

            if (isInitialized)
                return;
            isHost = false;

            CreateWaitEvent();
            clientWaitToken?.Cancel();
            clientWaitToken = new CancellationTokenSource();

            _ = Task.Factory.StartNew(() => WaitUntilSignalled(clientWaitToken.Token), TaskCreationOptions.LongRunning);

        }

        void WaitUntilSignalled(CancellationToken token)
        {
            while (true)
            {

                if (token.IsCancellationRequested)
                    return;

                if (waitHandle.WaitOne(500))
                {

                    CallCallbacks();

                    waitHandle.Dispose();
                    waitHandle = null;
                    InitializeClient();
                    return;

                }

            }
        }

    }

}
