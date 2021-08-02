using System;
using UnityEditor;
using UnityEngine;

namespace InstanceManager.Editor
{

    /// <summary>Contains a few extra gui functions.</summary>
    public static class GUIExt
    {

        static void Update()
        {
            prevEnabled = null;
            prevColor = null;
        }

        #region ColorScope

        static Color? prevColor;

        /// <summary>
        /// <para>Begins a color scope, this sets <see cref="GUI.color"/> and saves previous value, allowing it to be restored using <see cref="EndColorScope"/>.</para>
        /// <para>See also <see cref="EndColorScope"/></para>
        /// </summary>
        public static void BeginColorScope(Color color)
        {

            EditorApplication.update -= Update;
            EditorApplication.update += Update;

            if (prevColor.HasValue)
                throw new Exception($"Cannot use {nameof(BeginColorScope)} before ending existing scope with {nameof(EndColorScope)}.");

            prevColor = GUI.color;
            GUI.color = color;

        }

        /// <summary>Ends the color scope, that was started with <see cref="BeginColorScope(Color)"/>.</summary>
        public static void EndColorScope()
        {
            if (prevColor.HasValue)
                GUI.color = prevColor.Value;
            prevColor = null;
        }

        #endregion
        #region EnabledScope

        static bool? prevEnabled;

        /// <summary>
        /// <para>Begins an enabled scope, this sets <see cref="GUI.enabled"/> and saves previous value, allowing it to be restored using <see cref="EndEnabledScope"/>.</para>
        /// <para>See also <see cref="EndColorScope"/></para>
        /// </summary>
        public static void BeginEnabledScope(bool enabled, bool overrideWhenAlreadyFalse = false)
        {

            EditorApplication.update -= Update;
            EditorApplication.update += Update;

            if (!GUI.enabled && !overrideWhenAlreadyFalse)
                return;

            if (prevEnabled.HasValue)
                throw new Exception($"Cannot use {nameof(BeginEnabledScope)} before ending existing scope with {nameof(EndEnabledScope)}.");

            prevEnabled = GUI.enabled;
            GUI.enabled = enabled;

        }

        /// <summary>Ends the enabled scope, that was started with <see cref="BeginEnabledScope(bool)"/>.</summary>
        public static void EndEnabledScope()
        {
            if (prevEnabled.HasValue)
                GUI.enabled = prevEnabled.Value;
            prevEnabled = null;
        }

        #endregion

        /// <summary>
        /// <para>Unfocuses elements when blank area of <see cref="UnityEditor.EditorWindow"/> clicked.</para>
        /// <para>Returns true if element was unfocused, you may want to <see cref="UnityEditor.EditorWindow.Repaint"/> then.</para>
        /// </summary>
        public static bool UnfocusOnClick()
        {
            if (Event.current.type == EventType.MouseDown)
            {
                GUI.FocusControl(null);
                return true;
            }
            return false;
        }

    }

}
