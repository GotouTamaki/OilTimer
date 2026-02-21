using UnityEngine;

/// <summary>
/// シーン間でステージ情報を引き継ぐ静的クラス
/// StageSelectMenuからステージインデックスを受け取り、インゲームで使用する
/// </summary>
public static class StageDataHolder
{
    public static int CurrentStageIndex = 0;
    public static string[] StageSceneNames = new string[0];
}
