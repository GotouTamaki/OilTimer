using UnityEngine;

/// <summary>
/// クリア範囲のトリガー判定
/// ClearZoneとなるGameObjectにアタッチする
/// Collider の IsTrigger を On にすること
/// </summary>
public class ClearZoneTrigger : MonoBehaviour
{
    [SerializeField] private GameClearSystem gameClearSystem;

    private void OnTriggerEnter(Collider other)
    {
        gameClearSystem.OnBallEntered(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        gameClearSystem.OnBallExited(other.gameObject);
    }
}
