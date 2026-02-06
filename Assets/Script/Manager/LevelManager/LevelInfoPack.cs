using System.Collections;
using UnityEngine;

public class LevelInfoPack : MonoBehaviour
{
    public int LevelIndex;//关卡索引
    public Transform PlayerBornPos;//玩家出生点
    public LevelState CurrentLevelState = LevelState.Playing;//当前关卡状态

    // 缓存玩家预制体
    public GameObject PlayerPrefab;
    private GameObject _playerInstance;

    private void Start()
    {
        // 场景加载后自动注册关卡
        RegisterLevel();
    }

    /// <summary>
    /// 在管理器中注册当前关卡
    /// </summary>
    public void RegisterLevel()
    {
        LevelManager.Instance.CurrentLevelInfoPack = this;

        // 2. 加载该关卡的历史进度（P1核心：读档）
        LevelManager.Instance.LoadLevelProgress(LevelIndex);

        // 3. 生成玩家（示例：根据出生点）
        SpawnPlayer();

        // 4. 初始化关卡状态
        CurrentLevelState = LevelState.Playing;
        Debug.Log($"关卡 {LevelIndex} 已注册到管理器！");
    }

    /// <summary>
    /// 生成玩家（示例逻辑）
    /// </summary>
    private void SpawnPlayer()
    {
        if (PlayerPrefab == null || PlayerBornPos == null)
        {
            Debug.LogError("玩家预制体或出生点未配置！");
            return;
        }
        // 销毁已有玩家
        if (_playerInstance != null)
        {
            Destroy(_playerInstance);
        }
        // 生成新玩家
        _playerInstance = Instantiate(PlayerPrefab, PlayerBornPos.position, PlayerBornPos.rotation);
    }

    /// <summary>
    /// 设置当前关卡胜利（P1核心：状态更新+回调+存档）
    /// </summary>
    public void SetLevelWin()
    {
        if (CurrentLevelState == LevelState.Win) return; // 避免重复触发

        // 1. 更新状态
        CurrentLevelState = LevelState.Win;
        // 2. 触发管理器的胜利回调（供UI/音效监听）
        LevelManager.Instance.OnLevelWin?.Invoke();
        // 3. 自动存档
        LevelManager.Instance.SaveLevelProgress(LevelIndex);
        // 4. 进入结算状态
        StartCoroutine(SettlementCoroutine(true));
    }

    /// <summary>
    /// 设置当前关卡失败（P1核心）
    /// </summary>
    public void SetLevelLose()
    {
        if (CurrentLevelState == LevelState.Lose) return;

        CurrentLevelState = LevelState.Lose;
        LevelManager.Instance.OnLevelLose?.Invoke();
        StartCoroutine(SettlementCoroutine(false));
    }

    /// <summary>
    /// 关卡结算协程（胜利/失败后处理）
    /// </summary>
    private IEnumerator SettlementCoroutine(bool isWin)
    {
        CurrentLevelState = LevelState.Settlement;
        Debug.Log($"关卡 {LevelIndex} 进入结算状态，胜利：{isWin}");

        // 模拟结算等待（如显示结算UI 3秒）
        yield return new WaitForSeconds(3f);

        if (isWin)
        {
            // 胜利：解锁下一关+返回选关界面
            LevelManager.Instance.SaveGlobalProgress();
            ExitLevel();
        }
        else
        {
            // 失败：重置当前关卡
            ResetLevel();
        }
    }

    /// <summary>
    /// 重置当前关卡（P1核心：重新加载场景+恢复初始状态）
    /// </summary>
    public void ResetLevel()
    {
        if (!LevelManager.Instance.IsLevelOperable()) return;

        Debug.Log($"重置关卡 {LevelIndex}！");
        // 1. 销毁场景内动态生成的对象（玩家、敌人、道具）
        if (_playerInstance != null)
        {
            Destroy(_playerInstance);
        }
        // 2. 重新生成玩家+恢复出生点
        SpawnPlayer();
        // 3. 重置关卡状态
        CurrentLevelState = LevelState.Playing;
    }

    /// <summary>
    /// 退出当前关卡（调用管理器的卸载逻辑）
    /// </summary>
    public void ExitLevel()
    {
        Debug.Log($"退出关卡 {LevelIndex}！");
        LevelManager.Instance.ExitLevelScene();
        // 可选：加载选关界面
        // SceneManager.LoadScene("LevelSelect");
    }

}
//关卡状态
public enum LevelState
{ 
    Playing,//游玩中
    Pause,//当前关卡暂停
    Win,//玩家胜利
    Lose,//玩家失败
    Settlement,//结算中
}

