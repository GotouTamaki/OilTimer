using UnityEngine;
using UnityEditor;
using System.IO;

public class CreateHLSLFile
{
    [MenuItem("Assets/Create/Shader/HLSL File", false, 100)]
    public static void CreateNewHLSLFile()
    {
        //現在プロジェクトウィンドウで開いているパスを取得
        var selectedAsset = Selection.activeObject;
        string selectedPath = AssetDatabase.GetAssetPath(selectedAsset);

        //もし選択していない場合はAssets直下に作成
        if (selectedAsset == null || string.IsNullOrEmpty(selectedPath))
        {
            selectedPath = "Assets";
        }

        //現在プロジェクトウィンドウで開いているパスにNewHLSL.hlslという名前を付けファイルを作成
        string fullPath = Path.Combine(selectedPath, "NewHLSL.hlsl");
        fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);

        //ファイルが存在しない場合作成
        File.WriteAllText(fullPath, string.Empty);
        AssetDatabase.Refresh();
    }
}