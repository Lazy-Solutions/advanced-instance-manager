using System.IO;
using UnityEngine;

namespace InstanceManager.Utility
{

    /// <summary>Common paths in instance manager.</summary>
    internal static class Paths
    {

        /// <summary>The path to the project, outside assets folder.</summary>
        public static string project => Directory.GetParent(Application.dataPath).FullName;

        /// <summary>The path to the secondary instances.</summary>
        public static string embeddedInstances =>
            !InstanceManager.isSecondInstance
            ? Directory.CreateDirectory(Path.Combine(project, "EmbeddedInstances")).FullName
            : Directory.GetParent(project).FullName;

        /// <summary>The path to lists.json. The secondary instance list meta data.</summary>
        public static string listPath => Path.Combine(embeddedInstances, "lists.json");

        /// <summary>The path to symlinker.exe.</summary>
        public static string symLinker => Path.Combine(embeddedInstances, "SymLinker.exe");

        /// <summary>The path to symlinker.version.</summary>
        public static string symLinkerVersion => Path.Combine(embeddedInstances, "SymLinker.version");

        /// <summary>Gets the path to the specified secondary instance.</summary>
        public static string InstancePath(string listID) => Path.Combine(embeddedInstances, listID);

    }

}
