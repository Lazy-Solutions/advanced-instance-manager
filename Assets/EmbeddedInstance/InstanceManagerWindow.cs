#pragma warning disable IDE1006 // Naming Styles

using System;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EmbeddedInstance
{

    //TODO: Redirect output from process
    //TODO: Allow set scene(s) to auto open
    //TODO: Scenes do not reload on sync with main project
    //TODO: Autosync by default and automatically sync when main project is changed, without needing unity window to be focused

    public class InstanceManagerWindow : EditorWindow
    {

        [MenuItem("Tools/Instance Manager")]
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

        internal static InstanceManagerWindow window;
        private void OnEnable()
        {
            window = this;
            if (!SecondaryInstanceManager.isSecondInstance)
                InstanceManager.Reload();
        }

        private void OnDisable()
        {
            window = null;
        }

        private void OnFocus()
        {
        }

        Vector2 scrollPos;
        private void OnGUI()
        {

            minSize = new Vector2(450, 350);

            Style.Initialize();

            if (!SecondaryInstanceManager.isSecondInstance)
                PrimaryInstance_OnGUI();
            else
                SecondaryInstance_OnGUI();

        }

        void PrimaryInstance_OnGUI()
        {

            var c = GUI.color;
            GUI.color = new Color32(40, 40, 40, 255);
            GUI.DrawTexture(new Rect(0, 0, maxSize.x, maxSize.y), EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill);
            GUI.color = c;

            DrawHeader();

            DrawInstanceRow("ID:", "Status:                               ");

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            foreach (var instance in InstanceManager.instances.OfType<UnityInstance>().ToArray())
                DrawInstanceRow(instance);

            if (!InstanceManager.instances.Any())
                EditorGUILayout.LabelField("No instances found.", Style.noItemsText, GUILayout.Height(42));

            EditorGUILayout.EndScrollView();

            GUILayout.FlexibleSpace();
            DrawFooter();

        }

        void SecondaryInstance_OnGUI()
        {
            EditorGUILayout.LabelField("ID: " + SecondaryInstanceManager.id);
            EditorGUILayout.LabelField("Preferred layout: ", SecondaryInstanceManager.preferredLayout);
            EditorGUILayout.LinkButton("Set current as preferred");
            if (EditorGUILayout.LinkButton("Sync with main project"))
                AssetDatabase.Refresh();
            EditorGUILayout.ToggleLeft("Auto sync", false);
        }

        void DrawInstanceRow(UnityInstance instance) =>
            DrawInstanceRow(
                id: instance.ID,
                status: instance.isRunning ? "Running" : "Not running",
                openButtonValue: instance.isRunning,
                removeButton: () => Remove(instance),
                isEnabled: !instance.isSettingUp,
                instance);

        void Remove(UnityInstance instance)
        {
            InstanceManager.Delete(instance, Repaint);
            GUIUtility.ExitGUI();
        }

        void DrawInstanceRow(string id, string status, bool? openButtonValue = null, Action removeButton = null, bool isEnabled = true, UnityInstance instance = null)
        {

            GUI.enabled = isEnabled;

            EditorGUILayout.BeginHorizontal(Style.row);
            EditorGUILayout.LabelField(id);
            GUILayout.Label(status, GUILayout.ExpandWidth(false));

            if (!openButtonValue.HasValue)
                GUILayout.Label("", GUILayout.ExpandWidth(false));
            else if (GUILayout.Button(instance.isRunning ? "Close" : "Open", GUILayout.ExpandWidth(false)))
                instance.ToggleOpen();

            if (removeButton != null && GUILayout.Button("x", Style.removeButton, GUILayout.ExpandWidth(false)))
                removeButton.Invoke();

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
                menu.AddItem(new GUIContent("Open in explorer"), false, () => Process.Start("explorer", instance.path));
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

                InstanceManager.Create(onComplete: () =>
                {
                    EditorApplication.delayCall += Repaint;
                    EditorApplication.QueuePlayerLoopUpdate();
                });

                GUIUtility.ExitGUI();

            }

            EditorGUILayout.EndHorizontal();

        }

    }

}
