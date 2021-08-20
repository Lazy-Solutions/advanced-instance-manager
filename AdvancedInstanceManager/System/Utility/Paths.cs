using System.IO;
using UnityEngine;

namespace InstanceManager.Utility
{

    /// <summary>Common paths in instance manager.</summary>
    internal static class Paths
    {

        /// <summary>The path to the project, outside of Assets folder.</summary>
        public static string project { get; } = new DirectoryInfo(Application.dataPath).Parent.FullName.ToCrossPlatformPath();

        /// <summary>The path to the project, outside of Assets folder.</summary>
        public static string aboveProject { get; } = new DirectoryInfo(project).Parent.FullName.ToCrossPlatformPath();

        /// <summary>Gets the path to the specified secondary instance.</summary>
        public static string InstancePath(string id) => $"{aboveProject}/{Application.productName}{InstanceSeparatorChar}{id}";

        public const char InstanceSeparatorChar = '﹕';

    }

}
