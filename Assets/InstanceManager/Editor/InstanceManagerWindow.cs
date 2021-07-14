using InstanceManager.Models;
using InstanceManager.Utility;
using System;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace InstanceManager._Editor
{

    public class InstanceManagerWindow : EditorWindow
    {

        [MenuItem("Tools/Lazy/Instance Manager")]
        public static void Open()
        {
            var w = GetWindow<InstanceManagerWindow>();
            w.titleContent = new GUIContent("Instance Manager");
        }

        static class Style
        {

            public static GUIStyle margin { get; private set; }
            public static GUIStyle createButton { get; private set; }
            public static GUIStyle row { get; private set; }
            public static GUIStyle noItemsText { get; private set; }
            public static GUIStyle removeButton { get; private set; }

            public static void Initialize()
            {
                margin ??= new GUIStyle() { margin = new RectOffset(12, 12, 12, 12) };
                createButton ??= new GUIStyle(GUI.skin.button) { padding = new RectOffset(12, 12, 6, 6) };
                row ??= new GUIStyle(EditorStyles.toolbar) { padding = new RectOffset(12, 12, 12, 6), fixedHeight = 42 };
                noItemsText ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
                removeButton ??= new GUIStyle(GUI.skin.button) { margin = new RectOffset(12, 0, 0, 0) };
            }

        }

        string[] layouts;
        internal static InstanceManagerWindow window;
        void OnEnable()
        {

            layouts = WindowLayoutUtility.availableLayouts.Select(l => l.name).ToArray();
            window = this;

            InstanceManager.instances.Reload();

            if (InstanceManager.isSecondInstance)
                SetInstance(InstanceManager.id);

        }

        void OnDisable()
        {
            window = null;
        }

        void OnFocus()
        {
            InstanceManager.instances.Reload();
        }

        Vector2 scrollPos;
        void OnGUI()
        {

            UnfocusOnClick();

            minSize = new Vector2(450, 350);

            Style.Initialize();

            if (instance is null)
                PrimaryInstance_OnGUI();
            else
                SecondaryInstance_OnGUI();

        }


        [NonSerialized] UnityInstance instance;

        void ClearInstance() =>
            SetInstance(null);

        void SetInstance(string id)
        {
            Debug.Log("Instance set: " + id);
            instance = !string.IsNullOrEmpty(id)
            ? InstanceManager.instances.Find(id)
            : null;
        }

        void UnfocusOnClick()
        {
            if (Event.current.type == EventType.MouseDown)
            {
                GUI.FocusControl(null);
                Repaint();
            }
        }

        #region Primary instance

        void PrimaryInstance_OnGUI()
        {

            var c = GUI.color;
            GUI.color = new Color32(40, 40, 40, 255);
            GUI.DrawTexture(new Rect(0, 0, maxSize.x, maxSize.y), EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill);
            GUI.color = c;

            DrawHeader();

            DrawInstanceRow("ID:", "Status:                     ");

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            foreach (var instance in InstanceManager.instances.OfType<UnityInstance>().ToArray())
                DrawInstanceRow(instance);

            if (!InstanceManager.instances.Any())
                EditorGUILayout.LabelField("No instances found.", Style.noItemsText, GUILayout.Height(42));

            EditorGUILayout.EndScrollView();

            GUILayout.FlexibleSpace();
            DrawFooter();

        }

        void DrawInstanceRow(UnityInstance instance) =>
            DrawInstanceRow(
                id: instance.ID,
                status: instance.isRunning ? "Running" : "Not running",
                openButtonValue: instance.isRunning,
                isEnabled: !instance.isSettingUp,
                instance);

        void Remove(UnityInstance instance) =>
            InstanceManager.instances.Remove(instance, Repaint);

        void DrawInstanceRow(string id, string status, bool? openButtonValue = null, bool isEnabled = true, UnityInstance instance = null)
        {

            GUI.enabled = isEnabled;

            EditorGUILayout.BeginHorizontal(Style.row);
            EditorGUILayout.LabelField(id);
            GUILayout.Label(status, GUILayout.ExpandWidth(false));

            if (!openButtonValue.HasValue)
                GUILayout.Label("", GUILayout.ExpandWidth(false));
            else if (GUILayout.Button(instance.isRunning ? "Close" : "Open", GUILayout.ExpandWidth(false)))
                instance.ToggleOpen();

            EditorGUILayout.EndHorizontal();
            if (instance != null)
                ContextMenu_Item(instance);

            GUI.enabled = true;

        }

        void ContextMenu_Item(UnityInstance instance)
        {

            var rect = new Rect(0, GUILayoutUtility.GetLastRect().y + 73, position.width, 40);
            var pos = new Vector2(Event.current.mousePosition.x, Event.current.mousePosition.y + 73);

            if (Event.current.type == EventType.ContextClick && rect.Contains(pos))
            {

                var menu = new GenericMenu();

                if (instance.isRunning)
                    menu.AddItem(new GUIContent("Close"), false, () => instance.Close());
                else
                    menu.AddItem(new GUIContent("Open"), false, () => instance.Open());

                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Open in explorer..."), false, () => Process.Start("explorer", instance.path));

                if (instance.isRunning)
                    menu.AddDisabledItem(new GUIContent("Options..."));
                else
                    menu.AddItem(new GUIContent("Options..."), false, () => SetInstance(instance.ID));

                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Delete"), false, () => Remove(instance));
                menu.ShowAsContext();

                Repaint();

            }

        }

        void DrawHeader()
        {

        }

        void DrawFooter()
        {

            var r = GUILayoutUtility.GetRect(Screen.width, 1);

            var c = GUI.color;
            GUI.color = new Color32(56, 56, 56, 255);
            GUI.DrawTexture(new Rect(0, r.yMin, maxSize.x, maxSize.y), EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill);
            GUI.color = c;

            c = GUI.color;
            GUI.color = new Color32(35, 35, 35, (byte)(scrollPos.y == 1 ? 0 : 255));
            GUI.DrawTexture(r, EditorGUIUtility.whiteTexture);
            GUI.color = c;

            EditorGUILayout.BeginHorizontal(Style.margin);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Create new instance", Style.createButton))
            {

                InstanceManager.instances.Create(onComplete: () =>
                {
                    EditorApplication.delayCall += Repaint;
                    EditorApplication.QueuePlayerLoopUpdate();
                });

                GUIUtility.ExitGUI();

            }

            EditorGUILayout.EndHorizontal();

        }

        #endregion
        #region Secondary instance

        void SecondaryInstance_OnGUI()
        {

            EditorGUILayout.BeginVertical(new GUIStyle() { margin = new RectOffset(6, 6, 6, 6) });

            GUILayout.BeginHorizontal();

            if (!InstanceManager.isSecondInstance && GUILayout.Button("←"))
            {
                ClearInstance();
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.LabelField(" ID: " + instance.ID);

            GUILayout.FlexibleSpace();

            EditorGUI.BeginChangeCheck();

            var c = new GUIContent("Auto sync:");
            var size = GUI.skin.label.CalcSize(c);

            EditorGUILayout.LabelField(c, GUILayout.Width(size.x));
            instance.autoSync = EditorGUILayout.ToggleLeft("", instance.autoSync, GUILayout.Width(16));

            if (InstanceManager.isSecondInstance &&
                GUILayout.Button(new GUIContent("↻", "Sync with primary instance"), GUILayout.ExpandWidth(false)))
                AssetDatabase.Refresh();

            GUILayout.EndHorizontal();

            EditorGUILayout.BeginVertical(new GUIStyle() { margin = new RectOffset(0, 0, 12, 0) });
            EditorGUILayout.BeginHorizontal();

            var i = Array.IndexOf(layouts, instance.preferredLayout ?? "Default");
            if (i == -1) i = 0;
            instance.preferredLayout = layouts[EditorGUILayout.Popup("Preferred layout:", i, layouts)];

            if (InstanceManager.isSecondInstance && GUILayout.Button("apply", GUILayout.ExpandWidth(false)))
            {
                WindowLayoutUtility.Find(instance.preferredLayout).Apply();
                Open();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();

            if (EditorGUI.EndChangeCheck())
            {
                InstanceManager.instances.Update(instance);
                InstanceManager.instances.Save();
            }

        }

        #endregion

    }

}
