using InstanceManager.Utility;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace InstanceManager.Editor
{

    public partial class InstanceManagerWindow
    {

        public class SettingsView : View
        {

            string instancesPath;
            public override void OnEnable()
            {
                instancesPath = Paths.instancesPath.Replace(Paths.instancesPathSuffix, "").TrimEnd('/');
            }

            public override void OnGUI()
            {

                EditorGUILayout.BeginVertical(Style.margin);
                Header();
                InstancesPath();
                EditorGUILayout.EndVertical();

            }

            void Header()
            {

                EditorGUILayout.BeginHorizontal();

                if (!InstanceManager.isSecondInstance && GUILayout.Button(Content.back))
                {
                    CloseSettings();
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.LabelField("Settings", new GUIStyle(EditorStyles.label) { fontSize = 20, fixedHeight = 24, padding = new RectOffset(2, 0, -4, 0) });
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

            }

            void InstancesPath()
            {

                EditorGUILayout.BeginVertical(Style.elementMargin);

                EditorGUILayout.LabelField("Instances path:");
                EditorGUILayout.BeginHorizontal();

                instancesPath = GUILayout.TextField(instancesPath);
                GUIExt.BeginColorScope(new Color(1, 1, 1, 0.5f));
                var c = new GUIContent("/" + Paths.instancesPathSuffix);
                var size = EditorStyles.label.CalcSize(c);
                EditorGUILayout.LabelField(c, GUILayout.Width(size.x));
                GUIExt.EndColorScope();

                if (GUILayout.Button(EditorGUIUtility.IconContent("d_Folder Icon"), new GUIStyle(GUI.skin.button) { padding = new RectOffset(2, 2, 2, 2), fixedWidth = 18, fixedHeight = 18 }))
                    PickFolder();

                EditorGUILayout.EndHorizontal();

                var fullPath = Path.Combine(instancesPath, Paths.instancesPathSuffix).Replace('\\', '/');
                EditorGUILayout.BeginHorizontal();

                GUILayout.FlexibleSpace();
                GUILayout.FlexibleSpace();

                if (EditorGUILayout.LinkButton("Show in explorer"))
                    Process.Start(Paths.instancesPath);

                GUIExt.BeginEnabledScope(fullPath != Paths.instancesPath);
                if (EditorGUILayout.LinkButton("Apply"))
                {
                    Directory.CreateDirectory(fullPath);
                    InstanceManager.instances.MoveInstancesPath(fullPath, onComplete: () =>
                    {
                        window.Repaint();
                        OnEnable();
                    });
                    GUIUtility.ExitGUI();
                }
                GUIExt.EndEnabledScope();

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

            }

            void PickFolder()
            {
                var result = EditorUtility.OpenFolderPanel("Pick folder", instancesPath.Replace(Paths.instancesPathSuffix, ""), "");
                if (Directory.Exists(result))
                {
                    result = result.Replace("Instance Manager", "").Replace(InstanceManager.id, "").Replace('\\', '/').Trim('/');
                    instancesPath = result;
                }
            }

        }

    }

}
