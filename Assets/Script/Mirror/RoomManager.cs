using Mirror;
using UnityEngine;

/// <summary>
/// 房间管理器
/// </summary>
public class RoomManager : NetworkBehaviour
{
    public static RoomManager Instance { get; private set; }

    [Header("同步数据")]
    [SyncVar(hook = nameof(OnGameTimeChanged))]
    public float CurrentGameTime;

    [SyncVar]
    public int BlueTeamScore;

    [SyncVar]
    public int RedTeamScore;

    [SyncVar]
    public GameState CurrentState;

    public enum GameState
    {
        Waiting,
        Playing,
        Finished
    }

    void Awake()
    {
        Instance = this;
    }

    #region 服务器逻辑 (Server Only)

    public override void OnStartServer()
    {
        base.OnStartServer();
        // 服务器初始化
        CurrentGameTime = 300; // 5分钟
        BlueTeamScore = 0;
        RedTeamScore = 0;
        CurrentState = GameState.Waiting;

        // 开始游戏倒计时协程
        InvokeRepeating(nameof(ServerUpdateTimer), 1, 1);
    }

    [Server]
    void ServerUpdateTimer()
    {
        if (CurrentState != GameState.Playing) 
            return;

        CurrentGameTime--;
        if (CurrentGameTime <= 0)
        {
            CurrentGameTime = 0;
            ServerEndGame();
        }
    }

    // 服务器：开始游戏
    [Server]
    public void ServerStartGame()
    {
        CurrentState = GameState.Playing;
        // 通知所有客户端播放开场特效
        RpcOnGameStarted();
    }

    // 服务器：结束游戏
    [Server]
    public void ServerEndGame()
    {
        CurrentState = GameState.Finished;
        // 通知所有客户端显示结算UI
        RpcOnGameEnded(RedTeamScore > BlueTeamScore ? "红方" : "蓝方");
    }

    // 服务器：加分
    [Server]
    public void ServerAddScore(bool isRedTeam, int points)
    {
        if (isRedTeam)
            RedTeamScore += points;
        else
            BlueTeamScore += points;


    }

    #endregion

    #region 客户端回调 

    [ClientRpc]
    void RpcOnGameStarted()
    {
        Debug.Log("游戏正式开始！");
        // 这里可以播放UI动画、音效等
    }

    [ClientRpc]
    void RpcOnGameEnded(string winner)
    {
        Debug.Log($"游戏结束！获胜者: {winner}");
        // 这里可以显示结算面板
    }

    void OnGameTimeChanged(float oldValue, float newValue)
    {
        //调用统一的时间更新
    }

    #endregion
}