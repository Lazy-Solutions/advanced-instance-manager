using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace InstanceManager.Utility
{

    /// <summary>Common paths in instance manager.</summary>
    internal static class Paths
    {

        /// <summary>The path to the project, outside of Assets folder.</summary>
        public static string project => new DirectoryInfo(Application.dataPath).Parent.FullName;

        /// <summary>The path to the instances.</summary>
        public static string instancesPath
        {

            get => EditorPrefs.GetString("InstanceManager." + nameof(instancesPath), Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

            /*Do not use setter directly, use InstanceCollection.MoveInstances(string) instead*/
            set => EditorPrefs.SetString("InstanceManager." + nameof(instancesPath), value);

        }

        /// <summary>The path to lists.json. The secondary instance list meta data.</summary>
        public static string listPath => Path.Combine(instancesPath, "lists.json");

        /// <summary>Gets the path to the specified secondary instance.</summary>
        public static string InstancePath(string listID) => Path.Combine(instancesPath, listID);

    }

}
