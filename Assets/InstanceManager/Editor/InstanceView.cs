using InstanceManager.Utility;
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace InstanceManager.Editor
{

    public partial class InstanceManagerWindow
    {

        public class InstanceView : View
        {

            string[] layouts;
            (string path, SceneAsset asset, int index)[] addedScenes;
            (string path, SceneAsset asset, int index)[] scenes;
            public override Vector2? minSize => new Vector2(450, 82);

            Color scenesSeparator = new Color32(100, 100, 100, 32);

            public override void OnEnable()
            {
                layouts = WindowLayoutUtility.availableLayouts.Select(l => l.name).ToArray();
            }

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

                EditorGUILayout.BeginVertical(Style.secondaryInstanceMargin);
                Header();
                Layout();
                Scenes();
                AutoPlayMode();
                EditorGUILayout.EndVertical();

                if (EditorGUI.EndChangeCheck())
                    instance.Save();

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

                if (scenes == null)
                    RefreshScenes();

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

                if (addedScenes?.Any() ?? false)
                {

                    foreach (var scene in addedScenes)
                        DrawScene(scene, canReorder: true);

                    if (scenes.Any())
                    {
                        var r = GUILayoutUtility.GetLastRect();
                        GUIExt.BeginColorScope(scenesSeparator);
                        GUI.Label(new Rect(r.x, r.yMax + 8, r.width, 2), Content.emptyString, Style.scenesSeparator);
                        GUIExt.EndColorScope();
                        EditorGUILayout.Space();
                    }

                }

                if (scenes != null)
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

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.FlexibleSpace();
                if (InstanceManager.isSecondInstance && addedScenes.Any() && GUILayout.Button(Content.openScenes))
                {
                    var setup = addedScenes.Select(s => new SceneSetup() { path = s.path, isLoaded = true }).ToArray();
                    setup[0].isActive = true;
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
                }

                EditorGUILayout.EndHorizontal();

            }

            void DrawScene((string path, SceneAsset asset, int index) scene, bool canReorder = false)
            {

                EditorGUILayout.BeginHorizontal(Style.secondaryInstanceMargin);

                var value = instance.scenes?.Contains(scene.path) ?? false;
                var newValue = EditorGUILayout.Toggle(value, GUILayout.Width(16), GUILayout.Height(22));
                if (value != newValue)
                {
                    instance.SetScene(scene.path, enabled: newValue);
                    RefreshScenes();
                }

                GUIExt.BeginEnabledScope(false);
                EditorGUILayout.ObjectField(scene.asset, typeof(SceneAsset), allowSceneObjects: false, GUILayout.Height(22));
                GUIExt.EndEnabledScope();

                if (canReorder)
                {

                    GUIExt.BeginEnabledScope(scene.index > 0);
                    if (GUILayout.Button(Content.up, Style.moveSceneButton, GUILayout.ExpandWidth(false)))
                    {
                        instance.SetScene(scene.path, index: scene.index - 1);
                        RefreshScenes();
                    }
                    GUIExt.EndEnabledScope();

                    GUIExt.BeginEnabledScope(scene.index < instance.scenes?.Length - 1);
                    if (GUILayout.Button(Content.down, Style.moveSceneButton, GUILayout.ExpandWidth(false)))
                    {
                        instance.SetScene(scene.path, index: scene.index + 1);
                        RefreshScenes();
                    }
                    GUIExt.EndEnabledScope();

                }

                EditorGUILayout.EndHorizontal();

            }

        }

    }

}
