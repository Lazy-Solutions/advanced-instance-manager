using System;
using UnityEngine;

namespace InstanceManager.Editor
{

    /// <summary>Contains a few extra gui functions.</summary>
    public static class GUIExt
    {

        static Color? prevColor;

        /// <summary>
        /// <para>Begins a color scope, this sets <see cref="GUI.color"/> and saves previous value, allowing it to be restored using <see cref="EndColorScope"/>.</para>
        /// <para>See also <see cref="EndColorScope"/></para>
        /// </summary>
        public static void BeginColorScope(Color color)
        {

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
