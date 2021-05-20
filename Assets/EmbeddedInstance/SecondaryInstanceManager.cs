#pragma warning disable IDE1006 // Naming Styles

using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;

namespace EmbeddedInstance
{

    public static class SecondaryInstanceManager
    {

        const string idParamName = "-instanceID:";
        const string layoutParamName = "-layout:";

        public static event Action onSecondInstanceStarted;
        //public static UnityInstance instance { get; private set; }
        public static bool isSecondInstance { get; } = Environment.GetCommandLineArgs().Any(a => a.StartsWith(idParamName));

        public static string id { get; private set; }
        public static string preferredLayout { get; private set; }

        [InitializeOnLoadMethod]
        static async void OnLoad()
        {

            id = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith(idParamName))?.Replace(idParamName, "");
            if (id is null)
                return;

            var layout = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith(layoutParamName))?.Replace(layoutParamName, "");
            if (layout != null)
                preferredLayout = layout;

            AssetDatabase.DisallowAutoRefresh();
            //instance = new UnityInstance(id, Directory.GetParent(Application.dataPath).FullName)
            //{

            //};

            await Task.Delay(100);
            onSecondInstanceStarted?.Invoke();

            WindowUtility.Initialize();

        }

    }

}
