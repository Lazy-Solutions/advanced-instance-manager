using InstanceManager.Models;
using InstanceManager.Utility;
using System;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace InstanceManager.Editor
{

    public partial class InstanceManagerWindow : EditorWindow
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

            public static GUIStyle secondaryInstanceMargin { get; private set; }
            public static GUIStyle elementMargin { get; private set; }

            public static GUIStyle searchBox { get; private set; }
            public static GUIStyle scenesList { get; private set; }
            public static GUIStyle scenesSeparator { get; private set; }

            public static GUIStyle moveSceneButton { get; private set; }

            public static void Initialize()
            {

                margin ??= new GUIStyle() { margin = new RectOffset(12, 12, 12, 12) };
                createButton ??= new GUIStyle(GUI.skin.button) { padding = new RectOffset(12, 12, 6, 6) };
                row ??= new GUIStyle(EditorStyles.toolbar) { padding = new RectOffset(12, 12, 12, 6), fixedHeight = 42 };
                noItemsText ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
                removeButton ??= new GUIStyle(GUI.skin.button) { margin = new RectOffset(12, 0, 0, 0) };

                secondaryInstanceMargin ??= new GUIStyle() { margin = new RectOffset(6, 6, 6, 6) };
                elementMargin ??= new GUIStyle() { margin = new RectOffset(0, 0, 12, 0) };

                searchBox ??= new GUIStyle(EditorStyles.textField) { margin = new RectOffset(0, 6, 0, 0) };
                scenesList ??= new GUIStyle(GUI.skin.box) { margin = new RectOffset(6, 6, 8, 6) };
                scenesSeparator ??= new GUIStyle(GUI.skin.box);
                scenesSeparator.normal.background = EditorGUIUtility.whiteTexture;

                moveSceneButton ??= new GUIStyle(GUI.skin.button) { fontSize = 18, fixedWidth = 21, fixedHeight = 21, padding = new RectOffset(2, 0, 0, 0) };

            }

        }

        static class Content
        {

            public static GUIContent noInstances { get; private set; }
            public static GUIContent status { get; private set; }
            public static GUIContent running { get; private set; }
            public static GUIContent notRunning { get; private set; }

            public static GUIContent emptyString { get; private set; }
            public static GUIContent close { get; private set; }
            public static GUIContent open { get; private set; }

            public static GUIContent showInExplorer { get; private set; }
            public static GUIContent options { get; private set; }
            public static GUIContent delete { get; private set; }

            public static GUIContent symLinkerUpdate { get; private set; }
            public static GUIContent symLinkerNotInstalled { get; private set; }
            public static GUIContent update { get; private set; }
            public static GUIContent install { get; private set; }
            public static GUIContent github { get; private set; }
            public static GUIContent createNewInstance { get; private set; }

            public static GUIContent back { get; private set; }
            public static GUIContent reload { get; private set; }
            public static GUIContent autoSync { get; private set; }
            public static GUIContent apply { get; private set; }
            public static GUIContent autoPlayMode { get; private set; }

            public static GUIContent scenesToOpen { get; private set; }
            public static GUIContent search { get; private set; }

            public static GUIContent up { get; private set; }
            public static GUIContent down { get; private set; }

            public static void Initialize()
            {

                status ??= new GUIContent("Status:                     ");
                noInstances ??= new GUIContent("No instances found.");
                running ??= new GUIContent("Running");
                notRunning ??= new GUIContent("Not running");

                emptyString ??= new GUIContent(string.Empty);
                open ??= new GUIContent("Open");
                close ??= new GUIContent("Close");

                showInExplorer ??= new GUIContent("Show in explorer...");
                options ??= new GUIContent("Options...");
                delete ??= new GUIContent("Delete");

                symLinkerUpdate ??= new GUIContent("SymLinker.exe has an update available.");
                symLinkerNotInstalled ??= new GUIContent("SymLinker.exe is not installed.");
                update ??= new GUIContent("Update");
                install ??= new GUIContent("Install");
                github ??= new GUIContent("Github");
                createNewInstance ??= new GUIContent("Create new instance");

                back ??= new GUIContent("←");
                reload ??= new GUIContent("↻", "Sync with primary instance");
                autoSync ??= new GUIContent("Auto sync:");
                apply ??= new GUIContent("apply");
                autoPlayMode ??= new GUIContent("Automatically enter / exit play mode: ");

                scenesToOpen ??= new GUIContent("Scenes to open:");
                search ??= new GUIContent(" Search:");

                up ??= new GUIContent("▴");
                down ??= new GUIContent("▾");

            }

        }

        internal static InstanceManagerWindow window;

        void OnFocus()
        {
            InstanceManager.instances.Reload();
            editor.OnFocus();
        }

        void OnEnable()
        {

            window = this;

            InstanceManager.instances.Reload();

            if (InstanceManager.isSecondInstance)
                SetInstance(InstanceManager.id);
            SetInstance(InstanceManager.instances.First().id);

            primary.OnEnable();
            secondary.OnEnable();

        }

        void OnDisable()
        {
            primary.OnDisable();
            secondary.OnDisable();
            window = null;
        }

        static UnityInstance instance;

        InstanceEditor editor => instance is null ? primary : secondary;
        static InstanceEditor primary { get; } = new PrimaryInstance();
        static InstanceEditor secondary { get; } = new SecondaryInstance();

        void OnGUI()
        {

            if (GUIExt.UnfocusOnClick())
                Repaint();

            Style.Initialize();
            Content.Initialize();

            if (editor.minSize.HasValue)
                minSize = editor.minSize.Value;

            BeginCheckResize();
            editor.OnGUI();
            EndCheckResize();

        }

        #region Set instance

        static void ClearInstance() =>
            SetInstance(null);

        static void SetInstance(string id)
        {

            var prevEditor = window.editor;

            instance = !string.IsNullOrEmpty(id)
            ? InstanceManager.instances.Find(id)
            : null;

            if (prevEditor != window.editor)
            {
                prevEditor.OnDisable();
                window.editor.OnEnable();
            }

        }

        #endregion
        #region Check resize

        public bool hasResized { get; private set; }

        Rect prevPos;
        protected void BeginCheckResize()
        {
            hasResized =
                prevPos.width != position.width ||
                prevPos.height != position.height;
        }

        protected void EndCheckResize() =>
            prevPos = position;

        #endregion

        public class InstanceEditor
        {
            public virtual void OnGUI() { }
            public virtual void OnFocus() { }
            public virtual void OnEnable() { }
            public virtual void OnDisable() { }
            public virtual Vector2? minSize { get; }
        }

        public class PrimaryInstance : InstanceEditor
        {

            public override Vector2? minSize => new Vector2(450, 350);

            Color background = new Color32(56, 56, 56, 255);
            Color listBackground = new Color32(40, 40, 40, 255);
            Color line1 = new Color32(35, 35, 35, 0);
            Color line2 = new Color32(35, 35, 35, 255);

            bool isScrollbarInitialized;
            Vector2 scrollPos;
            Vector2 maxScroll;

            float normalizedYScroll =>
                isScrollbarInitialized && maxScroll.y != 0
                ? scrollPos.y / maxScroll.y
                : 0;

            void BeginScrollbar()
            {

                if (window.hasResized)
                    isScrollbarInitialized = false;

                //Workaround for not having a way to get normalized scroll pos
                //Just set scroll pos to float.MaxValue and let unity clamp to max and save value,
                //then reset
                if (!isScrollbarInitialized && Event.current.type == EventType.Repaint)
                {
                    Debug.Log("reset scroll");
                    var scroll = scrollPos;
                    scrollPos = new Vector2(0, float.MaxValue);
                    scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
                    maxScroll = scrollPos;
                    scrollPos = scroll;
                    isScrollbarInitialized = true;
                }
                else
                    scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            }

            public override void OnFocus()
            {
                UpdateSymLinker();
            }

            public override void OnGUI()
            {

                var c = GUI.color;
                GUI.color = listBackground;
                GUI.DrawTexture(new Rect(0, 0, window.position.width, window.position.height), EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill);
                GUI.color = c;

                DrawInstanceRow("ID:", Content.status);

                BeginScrollbar();

                foreach (var instance in InstanceManager.instances.OfType<UnityInstance>().ToArray())
                    DrawInstanceRow(instance);

                if (!InstanceManager.instances.Any())
                    EditorGUILayout.LabelField(Content.noInstances, Style.noItemsText, GUILayout.Height(42));

                EditorGUILayout.EndScrollView();

                GUILayout.FlexibleSpace();
                DrawFooter();

            }

            void DrawInstanceRow(UnityInstance instance) =>
                DrawInstanceRow(
                    id: instance.id,
                    status: instance.isRunning ? Content.running : Content.notRunning,
                    openButtonValue: instance.isRunning,
                    isEnabled: !instance.isSettingUp,
                    instance);

            void Remove(UnityInstance instance) =>
                 InstanceManager.instances.Remove(instance, window.Repaint);

            void DrawInstanceRow(string id, GUIContent status, bool? openButtonValue = null, bool isEnabled = true, UnityInstance instance = null)
            {

                GUI.enabled = isEnabled;

                EditorGUILayout.BeginHorizontal(Style.row);
                EditorGUILayout.LabelField(id);
                GUILayout.Label(status, GUILayout.ExpandWidth(false));

                if (!openButtonValue.HasValue)
                    GUILayout.Label(Content.emptyString, GUILayout.ExpandWidth(false));
                else if (GUILayout.Button(instance.isRunning ? Content.close : Content.open, GUILayout.ExpandWidth(false)))
                    instance.ToggleOpen();

                EditorGUILayout.EndHorizontal();
                if (instance != null)
                    ContextMenu_Item(instance);

                GUI.enabled = true;

            }

            void ContextMenu_Item(UnityInstance instance)
            {

                var rect = new Rect(0, GUILayoutUtility.GetLastRect().y + 73, window.position.width, 40);
                var pos = new Vector2(Event.current.mousePosition.x, Event.current.mousePosition.y + 73);

                if (Event.current.type == EventType.ContextClick && rect.Contains(pos))
                {

                    var menu = new GenericMenu();

                    if (instance.isRunning)
                        menu.AddItem(Content.close, false, () => instance.Close());
                    else
                        menu.AddItem(Content.open, false, () => instance.Open());

                    menu.AddSeparator(string.Empty);
                    menu.AddItem(Content.showInExplorer, false, () => Process.Start("explorer", instance.path));

                    if (instance.isRunning)
                        menu.AddDisabledItem(Content.options);
                    else
                        menu.AddItem(Content.options, false, () => SetInstance(instance.id));

                    menu.AddSeparator(string.Empty);
                    menu.AddItem(Content.delete, false, () => Remove(instance));
                    menu.ShowAsContext();

                    window.Repaint();

                }

            }

            bool symLinkerHasAnUpdate;
            bool symLinkerInstalled;
            async void UpdateSymLinker()
            {
                symLinkerHasAnUpdate = false;
                symLinkerInstalled = SymLinkUtility.isAvailable;
                if (window) window.Repaint();
                symLinkerHasAnUpdate = await SymLinkUtility.HasUpdate();
                if (window) window.Repaint();
            }

            void DrawFooter()
            {

                if (symLinkerHasAnUpdate || !symLinkerInstalled)
                {

                    EditorGUILayout.BeginHorizontal();
                    var symLinkerUpdate = symLinkerInstalled ? Content.symLinkerUpdate : Content.symLinkerNotInstalled;
                    var size = EditorStyles.label.CalcSize(symLinkerUpdate);
                    EditorGUILayout.LabelField(symLinkerUpdate, GUILayout.Width(size.x));
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button(Content.github, GUILayout.ExpandWidth(false)))
                        Process.Start("https://github.com/Lazy-Solutions/InstanceManager.SymLinker");

                    if (GUILayout.Button(symLinkerInstalled ? Content.update : Content.install, GUILayout.ExpandWidth(false)))
                        SymLinkUtility.Update(onDone: UpdateSymLinker);

                    EditorGUILayout.EndHorizontal();

                }

                var r = GUILayoutUtility.GetRect(Screen.width, 1);

                GUIExt.BeginColorScope(background);
                GUI.DrawTexture(new Rect(0, r.yMin, window.maxSize.x, window.maxSize.y), EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill);
                GUIExt.EndColorScope();

                GUIExt.BeginColorScope(normalizedYScroll == 1 ? line1 : line2);
                GUI.DrawTexture(r, EditorGUIUtility.whiteTexture);
                GUIExt.EndColorScope();

                EditorGUILayout.BeginHorizontal(Style.margin);
                GUILayout.FlexibleSpace();

                GUI.enabled = symLinkerInstalled;

                if (GUILayout.Button(Content.createNewInstance, Style.createButton))
                {

                    InstanceManager.instances.Create(onComplete: () =>
                    {
                        EditorApplication.delayCall += window.Repaint;
                        EditorApplication.QueuePlayerLoopUpdate();
                    });

                    GUIUtility.ExitGUI();

                }

                GUI.enabled = true;

                EditorGUILayout.EndHorizontal();

            }

        }

        public class SecondaryInstance : InstanceEditor
        {

            string[] layouts;
            (string path, SceneAsset asset, int index)[] addedScenes;
            (string path, SceneAsset asset, int index)[] scenes;
            public override Vector2? minSize => new Vector2(450, 82);

            Color scenesSeparator = new Color32(100, 100, 100, 32);

            public override void OnFocus()
            {
                layouts = WindowLayoutUtility.availableLayouts.Select(l => l.name).ToArray();
                RefreshScenes();
            }

            void RefreshScenes()
            {

                var allScenes = AssetDatabase.FindAssets("t:" + nameof(SceneAsset)).
                    Select(id => AssetDatabase.GUIDToAssetPath(id)).
                    Select(path => (
                        path,
                        asset: AssetDatabase.LoadAssetAtPath<SceneAsset>(path),
                        index: Array.IndexOf(instance.scenes, path)));

                addedScenes = allScenes.
                    Where(s => instance.scenes?.Contains(s.path) ?? false).
                    OrderBy(s => s.index).
                    ToArray();
                scenes = allScenes.Except(addedScenes).ToArray();

            }

            public override void OnGUI()
            {

                Header();

                EditorGUILayout.BeginVertical(Style.secondaryInstanceMargin);
                Layout();
                Scenes();
                AutoPlayMode();
                EditorGUILayout.EndVertical();

                if (EditorGUI.EndChangeCheck())
                    InstanceManager.instances.Update(instance);

            }

            void Header()
            {

                GUILayout.BeginHorizontal();

                if (!InstanceManager.isSecondInstance && GUILayout.Button(Content.back))
                {
                    ClearInstance();
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.LabelField(" ID: " + instance.id);

                GUILayout.FlexibleSpace();

                EditorGUI.BeginChangeCheck();

                var c = new GUIContent(Content.autoSync);
                var size = GUI.skin.label.CalcSize(c);

                EditorGUILayout.LabelField(c, GUILayout.Width(size.x));
                instance.autoSync = EditorGUILayout.ToggleLeft(Content.emptyString, instance.autoSync, GUILayout.Width(16));

                if (InstanceManager.isSecondInstance &&
                    GUILayout.Button(Content.reload, GUILayout.ExpandWidth(false)))
                {
                    InstanceManager.SyncWithPrimaryInstance();
                    Open();
                }

                GUILayout.EndHorizontal();

            }

            void Layout()
            {

                EditorGUILayout.BeginHorizontal(Style.elementMargin);

                var i = Array.IndexOf(layouts, instance.preferredLayout ?? "Default");
                if (i == -1) i = 0;
                instance.preferredLayout = layouts[EditorGUILayout.Popup("Preferred layout:", i, layouts)];

                if (InstanceManager.isSecondInstance && GUILayout.Button(Content.apply, GUILayout.ExpandWidth(false)))
                {
                    WindowLayoutUtility.Find(instance.preferredLayout).Apply();
                    Open();
                }

                EditorGUILayout.EndHorizontal();

            }

            void AutoPlayMode()
            {

                EditorGUILayout.BeginHorizontal();

                var contentSize = EditorStyles.label.CalcSize(Content.autoPlayMode);
                EditorGUILayout.LabelField(Content.autoPlayMode, GUILayout.Width(contentSize.x));
                instance.enterPlayModeAutomatically = EditorGUILayout.Toggle(instance.enterPlayModeAutomatically);

                EditorGUILayout.EndHorizontal();

            }

            Vector2 scroll;
            string q;
            void Scenes()
            {

                EditorGUILayout.BeginVertical(Style.elementMargin);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(Content.scenesToOpen);
                GUILayout.FlexibleSpace();
                GUILayout.FlexibleSpace();
                q = EditorGUILayout.TextField(q, Style.searchBox);

                if (string.IsNullOrEmpty(q))
                {

                    var r = GUILayoutUtility.GetLastRect();
                    GUIExt.BeginColorScope(Color.gray);
                    GUI.Label(r, Content.search);
                    GUIExt.EndColorScope();

                }

                EditorGUILayout.EndHorizontal();

                scroll = EditorGUILayout.BeginScrollView(scroll, Style.scenesList, GUILayout.MaxHeight(float.MaxValue), GUILayout.MinWidth(window.position.width - 24));

                if (addedScenes.Any())
                {

                    foreach (var scene in addedScenes)
                        DrawScene(scene);

                    if (scenes.Any())
                    {
                        var r = GUILayoutUtility.GetLastRect();
                        GUIExt.BeginColorScope(scenesSeparator);
                        GUI.Label(new Rect(r.x, r.yMax + 8, r.width, 2), Content.emptyString, Style.scenesSeparator);
                        GUIExt.EndColorScope();
                        EditorGUILayout.Space();
                    }

                }

                foreach (var scene in scenes)
                {

                    if (string.IsNullOrWhiteSpace(scene.path))
                        continue;

                    if (!string.IsNullOrWhiteSpace(q) && !scene.path.ToLower().Contains(q.ToLower()))
                        continue;

                    DrawScene(scene);

                }

                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();

            }

            void DrawScene((string path, SceneAsset asset, int index) scene)
            {

                EditorGUILayout.BeginHorizontal(Style.secondaryInstanceMargin);

                var value = instance.scenes?.Contains(scene.path) ?? false;
                var newValue = EditorGUILayout.Toggle(value, GUILayout.Width(16), GUILayout.Height(22));
                if (value != newValue)
                {
                    instance.SetScene(scene.path, enabled: newValue);
                    RefreshScenes();
                }

                GUI.enabled = false;
                EditorGUILayout.ObjectField(scene.asset, typeof(SceneAsset), allowSceneObjects: false, GUILayout.Height(22));
                GUI.enabled = true;

                GUI.enabled = scene.index > 0;
                if (GUILayout.Button(Content.up, Style.moveSceneButton, GUILayout.ExpandWidth(false)))
                {
                    instance.SetScene(scene.path, index: scene.index - 1);
                    RefreshScenes();
                }

                GUI.enabled = scene.index < instance.scenes.Length - 1;
                if (GUILayout.Button(Content.down, Style.moveSceneButton, GUILayout.ExpandWidth(false)))
                {
                    instance.SetScene(scene.path, index: scene.index + 1);
                    RefreshScenes();
                }

                GUI.enabled = true;

                EditorGUILayout.EndHorizontal();

            }

        }

    }

}
