using Mirror;
using UnityEngine;

public class CustomNetworkManager : NetworkManager
{
    // 自定义事件：供Main类监听
    public static event System.Action OnServerStartedEvent;//接入服务器
    public static event System.Action OnServerStoppedEvent;//停止服务器
    public static event System.Action OnClientConnectedSuccess;//链接成功


    /// <summary>
    /// 服务端启动时触发
    /// </summary>
    public override void OnStartServer()
    {
        base.OnStartServer();
        OnServerStartedEvent?.Invoke();
        Debug.Log("[CustomNetworkManager] 服务端启动，触发重生管理器生成事件");
    }

    /// <summary>
    /// 服务端停止时触发
    /// </summary>
    public override void OnStopServer()
    {
        base.OnStopServer();
        OnServerStoppedEvent?.Invoke();
        Debug.Log("[CustomNetworkManager] 服务端停止，触发重生管理器销毁事件");
    }

    /// <summary>
    /// 重写玩家生成（仅服务端执行）
    /// </summary>
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Transform spawnPoint = GetStartPosition(); // 先取Mirror默认出生点

        // 仅当重生管理器已生成时，才使用其出生点
        if (PlayerRespawnManager.Instance != null)
        {
            Transform managerSpawnPoint = PlayerRespawnManager.Instance.GetRandomSpawnPoint();
            if (managerSpawnPoint != null)
            {
                spawnPoint = managerSpawnPoint;
            }
        }
        if (playerPrefab == null)
        {
            Debug.LogError("[CustomNetworkManager] 严重错误：Player Prefab未赋值！请在Inspector面板中选择玩家预制体");
            conn.Disconnect();
            return;
        }

        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        Quaternion spawnRot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        // 生成玩家
        GameObject player = Instantiate(playerPrefab, spawnPos, spawnRot);
        NetworkServer.AddPlayerForConnection(conn, player);

        Debug.Log($"[CustomNetworkManager] 玩家{conn.connectionId}生成成功，出生点：{spawnPos}");

        // 仅保留服务端的全局消息播报（延迟执行）
        CountDownManager.Instance.CreateTimer(false, 500, () =>
        {
            string playerName = player.GetComponent<Player>().GetDisplayName(); // 用兜底方法
            PlayerRespawnManager.Instance.SendGlobalMessage("玩家：" + playerName + "加入进房间", 1);
        });

        PlayerRespawnManager.Instance.UpdatePlayerCount();//更新一下人数
    }


    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {

        // 尝试获取玩家名字用于广播
        string exitPlayerName = "未知玩家";
        if (conn.identity != null && conn.identity.TryGetComponent<Player>(out var player))
        {
            exitPlayerName = player.GetDisplayName();
        }

        if (PlayerRespawnManager.Instance != null)
        {
            // 这里需要你在 PlayerRespawnManager 里确实写了这个方法
            PlayerRespawnManager.Instance.HandlePlayerDisconnected(conn);

            // 发送退出公告
            PlayerRespawnManager.Instance.SendGlobalMessage("玩家：" + exitPlayerName + "离开了房间", 1);
        }

        base.OnServerDisconnect(conn);

        // 这里可以再更新一次人数，确保万无一失
        PlayerRespawnManager.Instance?.UpdatePlayerCount();

        Debug.Log($"[CustomNetworkManager] 玩家 {exitPlayerName} 断开连接");
    }

    /// <summary>
    /// 客户端连接服务器成功时触发
    /// </summary>
    public override void OnClientConnect()
    {
        base.OnClientConnect();
        // 触发客户端连接成功事件，通知Main类执行UI逻辑
        OnClientConnectedSuccess?.Invoke();
        Debug.Log("[CustomNetworkManager] 客户端连接服务器成功，触发UI初始化事件");
        //自动分配队伍(根据当前的人数比)
        
    }
}