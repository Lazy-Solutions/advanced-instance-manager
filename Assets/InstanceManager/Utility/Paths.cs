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
        public static string project { get; } = new DirectoryInfo(Application.dataPath).Parent.FullName;

        /// <summary>Gets 'Instance Manager\{id}'.</summary>
        public static string instancesPathSuffix => Path.Combine("Instance Manager", InstanceManager.id).Replace("\\", "/");

        /// <summary>Gets the default <see cref="instancesPath"/>.</summary>
        public static string defaultInstancesPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), instancesPathSuffix).Replace("\\", "/");

        /// <summary>The path to the instances.</summary>
        public static string instancesPath
        {
            [Obsolete] //Do not use setter directly, use InstanceCollection.MoveInstances(string) instead
            set => EditorPrefs.SetString("InstanceManager." + nameof(instancesPath), value);
            get => EditorPrefs.GetString("InstanceManager." + nameof(instancesPath), defaultInstancesPath).Replace("\\", "/").TrimEnd('/');
        }

        /// <summary>The path to lists.json. The secondary instance list meta data.</summary>
        public static string listPath => Path.Combine(instancesPath, "lists.json");

        /// <summary>Gets the path to the specified secondary instance.</summary>
        public static string InstancePath(string listID) => instancesPath + "/" + listID;

    }

}
