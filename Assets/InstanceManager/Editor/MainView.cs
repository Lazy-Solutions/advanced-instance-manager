using InstanceManager.Models;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace InstanceManager.Editor
{

    public partial class InstanceManagerWindow
    {

        public class MainView : View
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

            public override void OnGUI()
            {

                GUIExt.BeginColorScope(listBackground);
                GUI.DrawTexture(new Rect(0, 0, position.width, position.height), EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill);
                GUIExt.EndColorScope();

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

                GUIExt.BeginEnabledScope(isEnabled);

                EditorGUILayout.BeginHorizontal(Style.row);
                EditorGUILayout.LabelField(id);
                GUILayout.Label(status, GUILayout.ExpandWidth(false));

                if (!openButtonValue.HasValue)
                    GUILayout.Label(Content.emptyString, GUILayout.ExpandWidth(false));
                else if (GUILayout.Button(instance.isRunning ? Content.close : Content.open, GUILayout.ExpandWidth(false)))
                    instance.ToggleOpen();

                menuButtonPressed = openButtonValue.HasValue && GUILayout.Button(Content.menu, Style.menu, GUILayout.ExpandWidth(false));

                EditorGUILayout.EndHorizontal();
                if (instance != null)
                    ContextMenu_Item(instance);

                GUIExt.EndEnabledScope();

            }

            bool menuButtonPressed;
            void ContextMenu_Item(UnityInstance instance)
            {

                var rect = new Rect(0, GUILayoutUtility.GetLastRect().y + 73, window.position.width, 40);
                var pos = new Vector2(Event.current.mousePosition.x, Event.current.mousePosition.y + 73);

                if (menuButtonPressed || (Event.current.type == EventType.ContextClick && rect.Contains(pos)))
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

                    if (instance.isRunning)
                        menu.AddDisabledItem(Content.delete);
                    else
                        menu.AddItem(Content.delete, false, () => Remove(instance));

                    menu.ShowAsContext();

                    window.Repaint();

                }

            }

            void DrawFooter()
            {

                var r = GUILayoutUtility.GetRect(Screen.width, 1);

                GUIExt.BeginColorScope(background);
                GUI.DrawTexture(new Rect(0, r.yMin, window.maxSize.x, window.maxSize.y), EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill);
                GUIExt.EndColorScope();

                GUIExt.BeginColorScope(normalizedYScroll == 1 ? line1 : line2);
                GUI.DrawTexture(r, EditorGUIUtility.whiteTexture);
                GUIExt.EndColorScope();

                EditorGUILayout.BeginHorizontal(Style.margin);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(Content.settings, GUILayout.Height(27)))
                    OpenSettings();

                if (GUILayout.Button(Content.createNewInstance, Style.createButton))
                {

                    InstanceManager.instances.Create(onComplete: () =>
                    {
                        EditorApplication.delayCall += window.Repaint;
                        EditorApplication.QueuePlayerLoopUpdate();
                    });

                    GUIUtility.ExitGUI();

                }

                EditorGUILayout.EndHorizontal();

            }

        }

    }

}
