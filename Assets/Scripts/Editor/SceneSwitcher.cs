using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;

public class SceneSwitcher : EditorWindow
{
    [MenuItem("Tools/SceneSwitcher")]
    public static void ShowWindow()
    {
        GetWindow<SceneSwitcher>("SceneSwitcher");
    }

    private void OnGUI()
    {
        GUILayout.Label("ビルドに含まれるシーン", EditorStyles.boldLabel);
        DrawScenes(inBuild: true);

        GUILayout.Space(10);
        GUILayout.Label("ビルドに含まれないシーン", EditorStyles.boldLabel);
        DrawScenes(inBuild: false);
    }

    private void DrawScenes(bool inBuild)
    {
        var allSceneGuids = AssetDatabase.FindAssets("t:Scene");
        var allScenePaths = allSceneGuids
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .Where(path => path.StartsWith("Assets/"))
            .ToList();

        var buildScenePaths = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToHashSet();

        foreach (string path in allScenePaths)
        {
            bool isInBuild = buildScenePaths.Contains(path);
            if (isInBuild != inBuild) continue;

            string sceneName = Path.GetFileNameWithoutExtension(path);

            if (GUILayout.Button(sceneName))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(path);
                }
            }
        }
    }
}
