using System.Linq;
using UnityEditor.SceneManagement;

namespace InstanceManager
{
    internal static class SceneUtility
    {

        public static void OpenScenes(params string[] paths)
        {

            paths = paths.Where(path => !string.IsNullOrWhiteSpace(path)).ToArray();

            if (paths is null || paths.Length == 0)
                return;

            var setup = paths.Select(path => new SceneSetup() { path = path, isLoaded = true }).ToArray();
            setup[0].isActive = true;
            EditorSceneManager.RestoreSceneManagerSetup(setup);

        }

        public static void ReloadScenes() =>
            EditorSceneManager.RestoreSceneManagerSetup(EditorSceneManager.GetSceneManagerSetup());

    }

}
