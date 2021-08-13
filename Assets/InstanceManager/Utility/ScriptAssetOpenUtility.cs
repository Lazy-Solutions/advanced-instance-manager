using System.Diagnostics;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace InstanceManager.Utility
{

    internal static class ScriptAssetOpenUtility
    {

        [OnOpenAsset(0)]
        public static bool OnOpen(int instanceID, int line)
        {

            if (InstanceManager.isSecondaryInstance && EditorUtility.InstanceIDToObject(instanceID) is MonoScript script)
            {

                Process.Start("devenv",
                    "/edit " + Application.dataPath.Replace("/Assets", "") + "/" + AssetDatabase.GetAssetPath(script) + " " +
                    "/command " + $"GoToLn {line}".WithQuotes());

                return true;

            }

            return false;

        }

    }

}
