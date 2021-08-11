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

            Color grayedOutText = new Color(1, 1, 1, 0.5f);

            void Header()
            {

                EditorGUILayout.BeginHorizontal();

                if (!InstanceManager.isSecondInstance && GUILayout.Button(Content.back))
                {
                    CloseSettings();
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.LabelField(Content.settingsText, Style.header);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

            }

            void InstancesPath()
            {

                EditorGUILayout.BeginVertical(Style.elementMargin);

                EditorGUILayout.LabelField(Content.instancesPath);
                EditorGUILayout.BeginHorizontal();

                instancesPath = GUILayout.TextField(instancesPath);
                GUIExt.BeginColorScope(grayedOutText);
                var c = new GUIContent("/" + Paths.instancesPathSuffix);
                var size = EditorStyles.label.CalcSize(c);
                EditorGUILayout.LabelField(c, GUILayout.Width(size.x));
                GUIExt.EndColorScope();

                if (GUILayout.Button(Content.folder, Style.folder))
                    PickFolder();

                EditorGUILayout.EndHorizontal();

                var fullPath = Path.Combine(instancesPath, Paths.instancesPathSuffix).ToCrossPlatformPath();
                EditorGUILayout.BeginHorizontal();

                GUILayout.FlexibleSpace();
                GUILayout.FlexibleSpace();

                if (EditorGUILayout.LinkButton(Content.showInExplorer))
                    Process.Start(Paths.instancesPath);

                GUIExt.BeginEnabledScope(fullPath != Paths.instancesPath);
                if (EditorGUILayout.LinkButton(Content.Apply))
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
                    result = result.Replace("Instance Manager".WithQuotes("\\"), "").Replace(InstanceManager.id.WithQuotes("\\"), "").ToCrossPlatformPath().Trim('/');
                    instancesPath = result;
                }
            }

        }

    }

}
