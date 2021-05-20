#pragma warning disable IDE1006 // Naming Styles

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace EmbeddedInstance
{

    internal static class WindowUtility
    {

        [Serializable]
        public class WindowSettings
        {

            public static WindowSettings Current { get; } = Load();

            public Rect rect;

            static WindowSettings Load()
            {

                WindowSettings settings = null;
                if (File.Exists("windowSettings.json"))
                    settings = JsonUtility.FromJson<WindowSettings>(File.ReadAllText("windowSettings.json"));

                return settings ?? Default();

            }

            static WindowSettings Default()
            {

                var width = 200;
                var height = 200;
                //var x = Screen.safeArea.x + ((Screen.safeArea.width / 2) - (width / 2));
                //var y = Screen.safeArea.y + ((Screen.safeArea.height / 2) - (height / 2));

                var x = 0 + (2560 / 2) - (width / 2);
                var y = 0 + (1440 / 2) - (height / 2);

                return new WindowSettings() { rect = new Rect(x, y, width, height) };

            }

            public void Save()
            {
                File.WriteAllText("windowSettings.json", JsonUtility.ToJson(this));
            }

        }

        static bool isInitialized;
        public static async Task Initialize()
        {

            if (isInitialized)
                return;
            isInitialized = true;

            EditorApplication.wantsToQuit += () =>
            {
                try
                {
                    WindowSettings.Current.rect = mainWindow.position;
                    WindowSettings.Current.Save();
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    return false;
                }
                return true;
            };

            mainWindow = await GetMainWindow();

            mainWindow.maximized = false;
            mainWindow.position = WindowSettings.Current.rect;
            mainWindow.Show();

        }

        static ContainerWindow mainWindow { get; set; }

        static async Task<ContainerWindow> GetMainWindow()
        {

            ContainerWindow window = null;

            while (window == null)
            {

                var type = Type.GetType("UnityEditor.ContainerWindow, UnityEditor.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
                var showModeField = type.GetField("m_ShowMode", BindingFlags.NonPublic | BindingFlags.Instance);

                var windows = Resources.FindObjectsOfTypeAll(type);
                foreach (var win in windows)
                    if ((int)showModeField.GetValue(win) == ContainerWindow.ShowMode_MainWindow) // main window
                        window = new ContainerWindow((ScriptableObject)win);

                await Task.Delay(100);

            }

            return window;

        }

        /// <summary>
        /// <para>Wrapper for internal class:</para>
        /// <para><a href="https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/ContainerWindow.cs"></a></para>
        /// </summary>
        class ContainerWindow
        {

            public ContainerWindow(ScriptableObject source)
            {
                this.source = source;
                positionProperty = source.GetType().GetProperty("position", BindingFlags.Public | BindingFlags.Instance);
                maximizedField = source.GetType().GetField("m_Maximized", BindingFlags.NonPublic | BindingFlags.Instance);
                showModeField = source.GetType().GetField("m_ShowMode", BindingFlags.NonPublic | BindingFlags.Instance);
                showMethod = source.GetType().GetMethod("Show", BindingFlags.Public | BindingFlags.Instance);
            }

            readonly ScriptableObject source;
            readonly PropertyInfo positionProperty;
            readonly FieldInfo maximizedField;
            readonly FieldInfo showModeField;
            readonly MethodInfo showMethod;

            public const int ShowMode_MainWindow = 4;

            public Rect position
            {
                get => (Rect)positionProperty.GetValue(source);
                set => positionProperty.SetValue(source, value);
            }

            public bool maximized
            {
                get => (bool)maximizedField.GetValue(source);
                set => maximizedField.SetValue(source, value);
            }

            public int showMode
            {
                get => (int)showModeField.GetValue(source);
                set => showModeField.SetValue(source, value);
            }

            public void Show()
            {

                //Target signature (at time of writing, there are no overloads):
                //public void Show(ShowMode showMode, bool loadPosition, bool displayImmediately, bool setFocus)

                showMethod.Invoke(source, new object[] { showMode, true, true, true });

            }

        }

    }

}
