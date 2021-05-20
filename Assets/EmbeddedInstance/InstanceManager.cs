#pragma warning disable IDE1006 // Naming Styles

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace EmbeddedInstance
{

    public static class InstanceManager
    {

        static readonly Dictionary<int, string> SymLinkerErrorCodes = new Dictionary<int, string>()
        {
            { -1, "Unknown error."},
            { 1,  "Process is not elevated." },
            { 2,  "Source folder does not exist." },
            { 3,  "Target folder is not empty." },
            { 4,  "Source and target folders are the same."},
        };

        [Serializable]
        public class InstanceCollection : IReadOnlyList<UnityInstance>
        {

            [SerializeField] List<UnityInstance> list = new List<UnityInstance>();

            public UnityInstance this[int index] =>
                ((IReadOnlyList<UnityInstance>)list)[index];

            public int Count =>
                ((IReadOnlyCollection<UnityInstance>)list).Count;

            public IEnumerator<UnityInstance> GetEnumerator() =>
                ((IEnumerable<UnityInstance>)list).GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() =>
                ((IEnumerable)list).GetEnumerator();

            internal UnityInstance Add(UnityInstance instance)
            {
                list.Add(instance);
                Save();
                return instance;
            }

            internal void Remove(UnityInstance instance)
            {
                list.RemoveAll(i => i.ID == instance.ID);
                Save();
            }

        }

        static string projectPath => Directory.GetParent(Application.dataPath).FullName;

        static string path => Directory.CreateDirectory(Path.Combine(projectPath, "EmbeddedInstances")).FullName;
        static string symLinkerPath => Path.Combine(path, "SymLinker.exe");
        static string listPath => Path.Combine(path, "lists.json");
        static string InstancePath(string listID) => Path.Combine(path, listID);

        public static InstanceCollection instances { get; private set; }

        [InitializeOnLoadMethod]
        public static void Reload()
        {
            instances = Load() ?? new InstanceCollection();
            EditorApplication.wantsToQuit += EditorApplication_wantsToQuit;
        }

        private static bool EditorApplication_wantsToQuit()
        {
            foreach (var instance in instances)
                instance?.Close();
            return true;
        }

        public static void Save()
        {

            if (instances is null)
                return;

            var json = JsonUtility.ToJson(instances);
            File.WriteAllText(listPath, json);

        }

        static InstanceCollection Load()
        {

            if (!File.Exists(listPath))
                return null;

            var json = File.ReadAllText(listPath);
            var instances = JsonUtility.FromJson<InstanceCollection>(json);
            return instances;

        }

        public static UnityInstance Create(Action onComplete = null)
        {

            if (!File.Exists(symLinkerPath))
            {
                Debug.LogError(@"Could not find SymLinker tool, please reinstall Instance Manager, or follow instructions at <a>https://github.com/zumwani/unity-instance-manager/wiki/symlinker</a>.");
                return null;
            }

            var id = GenerateID(validate: id => !Directory.Exists(InstancePath(id)));
            var path = InstancePath(id);
            var instance = new UnityInstance(id, path)
            {
                isSettingUp = true
            };

            SymLink("Creating new instance", projectPath, path,
                onComplete: () =>
                {
                    instance.isSettingUp = false;
                    onComplete?.Invoke();
                });

            return instances.Add(instance);

        }

        static string GenerateID(Func<string, bool> validate = null)
        {

            string id = null;
            while (id is null || !(validate?.Invoke(id) ?? true))
            {
                var ticks = new DateTime(2016, 1, 1).Ticks;
                var ans = DateTime.Now.Ticks - ticks;
                id = ans.ToString("x");
            }

            return id;

        }

        public static void Delete(UnityInstance instance, Action onComplete = null)
        {
            instance.Close();
            instance.isSettingUp = true;
            SymLink(progressString: "Deleting instance", "-delete", instance.path,
                onComplete: () =>
                {
                    instances.Remove(instance);
                    onComplete?.Invoke();
                });
        }

        internal static void SymLink(string progressString, string p1, string p2, Action onComplete = null) =>
            EditorApplication.delayCall += () =>
            {

                var progress = Progress.Start(progressString, options: Progress.Options.Indefinite);

                void OnComplete()
                {
                    onComplete?.Invoke();
                    Progress.Remove(progress);
                }

                try
                {

                    var p = Process.Start(new ProcessStartInfo(symLinkerPath, p1 + " " + p2)
                    {
                        UseShellExecute = true,
                        Verb = "runas",
                        WindowStyle = ProcessWindowStyle.Hidden
                    });

                    p.EnableRaisingEvents = true;
                    p.Exited += OnExit;

                    void OnExit(object sender, EventArgs e)
                    {

                        //Run on main thread
                        EditorApplication.delayCall += () =>
                        {

                            if (p.ExitCode != 0)
                            {
                                if (SymLinkerErrorCodes.TryGetValue(p.ExitCode, out var message))
                                    Debug.LogError(message);
                                else
                                    Debug.LogError(new Win32Exception(p.ExitCode));
                            }

                            p.Exited -= OnExit;
                            OnComplete();

                        };
                        EditorApplication.QueuePlayerLoopUpdate();

                    }

                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    OnComplete();
                }

            };

    }

}
