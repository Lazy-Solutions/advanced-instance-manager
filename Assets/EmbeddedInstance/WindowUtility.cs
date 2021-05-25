#pragma warning disable IDE1006 // Naming Styles

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
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

            //EditorApplication.wantsToQuit += () =>
            //{
            //    try
            //    {
            //        WindowSettings.Current.rect = mainWindow.position;
            //        WindowSettings.Current.Save();
            //    }
            //    catch (Exception e)
            //    {
            //        Debug.LogError(e);
            //        return false;
            //    }
            //    return true;
            //};



            //mainWindow = await GetMainWindow();
            var handle = await GetHandle();

            //ShowWindow(handle, ShowWindowCommands.Normal);
            //MoveWindow(handle, 200, 200, 1000, 600, false);

            //mainWindow.title.value = "sak";
            //Debug.Log(mainWindow.title.isAvailable);
            //mainWindow.m_Maximized.value = false;
            //mainWindow.m_PixelRect.value = new Rect(0, 200, 1000, 600);
            //mainWindow.SaveGeometry.Invoke();
            //mainWindow.Internal_Show.Invoke((m_PixelRect: new Rect(0, 0, 1000, 600), mainWindow.m_ShowMode, mainWindow.m_MinSize, mainWindow.m_MaxSize));
            //mainWindow.Show.Invoke((showMode: ContainerWindow.ShowMode.MainWindow, loadPosition: true, displayImmediately: true, setFocus: true));
            //Debug.Log(mainWindow.m_PixelRect.value);

            //await Task.Delay(500);
            //mainWindow.Show.Invoke((showMode: ContainerWindow.ShowMode.MainWindow, loadPosition: true, displayImmediately: true, setFocus: true));
            //await Task.Delay(500);
            //mainWindow.Show.Invoke((showMode: ContainerWindow.ShowMode.MainWindow, loadPosition: true, displayImmediately: true, setFocus: true));

            //if (GetWindowRect(handle, out var rect))
            //    Debug.Log(rect);

        }

        public static async Task<IntPtr> GetHandle()
        {

            var handle = IntPtr.Zero;
            while (handle == IntPtr.Zero)
            {

                handle = GetForegroundWindow();
                var sb = new StringBuilder(256);
                GetClassName(handle, sb, sb.Capacity);
                Debug.Log(sb.ToString());
                if (sb.ToString() == "UnityContainerWndClass")
                    return handle;

                handle = IntPtr.Zero;
                await Task.Delay(10);

            }

            return IntPtr.Zero;

        }

        [StructLayout(LayoutKind.Sequential)]
        struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
            public override string ToString() =>
                string.Join(", ", Left, Top, Right, Bottom);
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, ShowWindowCommands nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        enum ShowWindowCommands
        {
            /// <summary>
            /// Hides the window and activates another window.
            /// </summary>
            Hide = 0,
            /// <summary>
            /// Activates and displays a window. If the window is minimized or
            /// maximized, the system restores it to its original size and position.
            /// An application should specify this flag when displaying the window
            /// for the first time.
            /// </summary>
            Normal = 1,
            /// <summary>
            /// Activates the window and displays it as a minimized window.
            /// </summary>
            ShowMinimized = 2,
            /// <summary>
            /// Maximizes the specified window.
            /// </summary>
            Maximize = 3, // is this the right value?
            /// <summary>
            /// Activates the window and displays it as a maximized window.
            /// </summary>      
            ShowMaximized = 3,
            /// <summary>
            /// Displays a window in its most recent size and position. This value
            /// is similar to <see cref="Win32.ShowWindowCommand.Normal"/>, except
            /// the window is not activated.
            /// </summary>
            ShowNoActivate = 4,
            /// <summary>
            /// Activates the window and displays it in its current size and position.
            /// </summary>
            Show = 5,
            /// <summary>
            /// Minimizes the specified window and activates the next top-level
            /// window in the Z order.
            /// </summary>
            Minimize = 6,
            /// <summary>
            /// Displays the window as a minimized window. This value is similar to
            /// <see cref="Win32.ShowWindowCommand.ShowMinimized"/>, except the
            /// window is not activated.
            /// </summary>
            ShowMinNoActive = 7,
            /// <summary>
            /// Displays the window in its current size and position. This value is
            /// similar to <see cref="Win32.ShowWindowCommand.Show"/>, except the
            /// window is not activated.
            /// </summary>
            ShowNA = 8,
            /// <summary>
            /// Activates and displays the window. If the window is minimized or
            /// maximized, the system restores it to its original size and position.
            /// An application should specify this flag when restoring a minimized window.
            /// </summary>
            Restore = 9,
            /// <summary>
            /// Sets the show state based on the SW_* value specified in the
            /// STARTUPINFO structure passed to the CreateProcess function by the
            /// program that started the application.
            /// </summary>
            ShowDefault = 10,
            /// <summary>
            ///  <b>Windows 2000/XP:</b> Minimizes a window, even if the thread
            /// that owns the window is not responding. This flag should only be
            /// used when minimizing windows from a different thread.
            /// </summary>
            ForceMinimize = 11
        }

        static ContainerWindow mainWindow { get; set; }

        static async Task<ContainerWindow> GetMainWindow()
        {

            //We don't have a way of knowing when unity is done during startup, so lets just loop until we do
            while (true)
            {

                var type = Type.GetType("UnityEditor.ContainerWindow, UnityEditor.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", throwOnError: false);
                if (type is null)
                {
                    Debug.LogError("The type 'UnityEditor.ContainerWindow' could not be found! This means your Unity version is not yet supported.");
                    return null;
                }

                var windows = Resources.FindObjectsOfTypeAll(type).OfType<ScriptableObject>();
                foreach (var win in windows)
                    if (ContainerWindow.TryCreate(win, out var window) && window.IsMainWindow.Invoke())
                        return window;

                await Task.Delay(10);

            }

        }

        /// <summary>
        /// <para>Wrapper for internal class:</para>
        /// <para><a href="https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/ContainerWindow.cs"></a></para>
        /// </summary>
        public class ContainerWindow : ReflectedClass
        {

            public ContainerWindow(ScriptableObject source) : base(source)
            { }

            public static bool TryCreate(ScriptableObject source, out ContainerWindow window)
            {
                window = new ContainerWindow(source);
                return window.isValid;
            }

            public bool isValid => m_ShowMode?.isAvailable ?? false;

            public ReflectedProperty<object> m_WindowPtr { get; private set; }
            public ReflectedProperty<Vector2> m_MinSize { get; private set; }
            public ReflectedProperty<Vector2> m_MaxSize { get; private set; }
            public ReflectedProperty<Rect> position { get; private set; }
            public ReflectedProperty<Rect> m_PixelRect { get; private set; }
            public ReflectedProperty<string> title { get; private set; }
            public ReflectedProperty<bool> m_Maximized { get; private set; }
            public ReflectedProperty<ShowMode> m_ShowMode { get; private set; }

            public ReflectedFunction<bool> IsMainWindow { get; private set; }

            public ReflectedMethod<(ShowMode showMode, bool loadPosition, bool displayImmediately, bool setFocus)> Show { get; set; }
            public ReflectedMethod SaveGeometry { get; private set; }
            public ReflectedMethod<(Rect m_PixelRect, ShowMode m_ShowMode, Vector2 m_MinSize, Vector2 m_MaxSize)> Internal_Show { get; private set; }

            IntPtr? m_handle;
            public IntPtr handle => m_handle.HasValue
                ? m_handle.Value
                : (m_handle = GetHandle()).Value;

            public IntPtr GetHandle()
            {

                Show.Invoke((m_ShowMode, loadPosition: false, displayImmediately: true, setFocus: true));

                var handle = IntPtr.Zero;
                while (handle == IntPtr.Zero)
                {

                    handle = GetForegroundWindow();
                    var sb = new StringBuilder(256);
                    GetClassName(handle, sb, sb.Capacity);
                    Debug.Log(sb.ToString());
                    if (sb.ToString() == "UnityContainerWndClass")
                        return handle;

                    handle = IntPtr.Zero;

                }

                return IntPtr.Zero;

            }

            public enum ShowMode
            {
                NormalWindow = 0,
                PopupMenu = 1,
                Utility = 2,
                NoShadow = 3,
                MainWindow = 4,
                AuxWindow = 5,
                Tooltip = 6,
                ModalUtility = 7
            }

        }

        public abstract class ReflectedClass
        {

            public const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.InvokeMethod |
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.GetField | BindingFlags.SetField |
                BindingFlags.GetProperty | BindingFlags.SetProperty;

            public object source { get; }

            public ReflectedClass(object source)
            {

                this.source = source;

                var properties = GetType().
                    GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.SetProperty).
                    Where(p => typeof(IReflectionHelper).IsAssignableFrom(p.PropertyType)).
                    ToArray();

                foreach (var reflectedMember in properties)
                {
                    var instance = Activator.CreateInstance(reflectedMember.PropertyType, new object[] { this, reflectedMember.Name });
                    reflectedMember.SetValue(this, instance);
                }

            }

            public interface IReflectionHelper
            { }

            public class ReflectedMember<Value> : IReflectionHelper
            {

                public ReflectedMember(ContainerWindow wrapper, string name)
                {

                    this.wrapper = wrapper;
                    this.name = name;

                    if (wrapper?.source == null)
                    {
                        Debug.LogError("Member '" + name + "' does not exist in ContainerWindow! This means that your Unity version is not yet supported.");
                        return;
                    }

                    //Debug.Log(string.Join(", ", wrapper.source.GetType().GetMembers(flags).Select(m => m.Name)));

                    member = wrapper.source.GetType().GetMember(
                        name,
                        type: MemberTypes.Field | MemberTypes.Property | MemberTypes.Method,
                        bindingAttr: flags).
                                     FirstOrDefault();

                    if (member is null)
                    {
                        Debug.LogError("Member '" + name + "' does not exist in ContainerWindow! This means that your Unity version is not yet supported.");
                        return;
                    }

                }

                public string name { get; private set; }
                public ContainerWindow wrapper { get; private set; }
                public MemberInfo member { get; private set; }

                public bool isAvailable => member != null;

            }

            public class ReflectedProperty<Value> : ReflectedMember<Value>
            {

                public ReflectedProperty(ContainerWindow wrapper, string name) : base(wrapper, name)
                { }

                public Value value
                {
                    get
                    {
                        if (member is FieldInfo field)
                            return (Value)field.GetValue(wrapper.source);
                        else if (member is PropertyInfo property)
                            return (Value)property.GetValue(wrapper.source);
                        return default;
                    }
                    set
                    {
                        (member as FieldInfo)?.SetValue(wrapper.source, value);
                        (member as PropertyInfo)?.SetValue(wrapper.source, value);
                    }
                }

                public static implicit operator Value(ReflectedProperty<Value> prop) =>
                    prop.value;

            }

            public class ReflectedFunction<ReturnValue> : ReflectedMember<object>
            {

                public ReflectedFunction(ContainerWindow wrapper, string name) : base(wrapper, name)
                { }

                public ReturnValue Invoke() =>
                  InvokeInternal(member, wrapper);

                public static ReturnValue InvokeInternal(MemberInfo member, ContainerWindow wrapper, ITuple param = default)
                {

                    var parameters = new List<object>();
                    for (int i = 0; i < param?.Length; i++)
                    {
                        if (param[i].GetType().BaseType == typeof(Enum))
                            parameters.Add((int)param[i]);
                        else
                            parameters.Add(param[i]);
                    }

                    return (member as MethodInfo).Invoke(wrapper.source, parameters.ToArray()) is ReturnValue returnValue
                        ? returnValue
                        : default;

                }

            }

            public class ReflectedMethod : ReflectedMember<object>
            {

                public ReflectedMethod(ContainerWindow wrapper, string name) : base(wrapper, name)
                { }

                public void Invoke() =>
                    ReflectedFunction<object>.InvokeInternal(member, wrapper);

            }

            public class ReflectedMethod<TParam> : ReflectedMember<object> where TParam : ITuple
            {

                public ReflectedMethod(ContainerWindow wrapper, string name) : base(wrapper, name)
                { }

                public void Invoke(TParam parameters) =>
                    ReflectedFunction<object>.InvokeInternal(member, wrapper, parameters);

            }

            public class ReflectedFunction<ReturnValue, TParam> : ReflectedMember<object> where TParam : ITuple
            {

                public ReflectedFunction(ContainerWindow wrapper, string name) : base(wrapper, name)
                { }

                public ReturnValue Invoke(TParam parameters) =>
                  ReflectedFunction<ReturnValue>.InvokeInternal(member, wrapper, parameters);

            }

        }

    }

}
