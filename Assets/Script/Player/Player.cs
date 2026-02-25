using Mirror;
using UnityEngine;

public class Player : Base_Entity
{
    public static Player LocalPlayer { get; private set; }

    [Header("自己的身体")]
    public GameObject MyBody; // 只缩放这个物体

    [Header("核心组件")]
    public playerStats myStats;
    public MyPlayerInput myInputSystem;

    [Header("枪械挂载")]
    public Transform playerHandPos;
    public playerHandControl MyHandControl;
    [SyncVar(hook = nameof(OnGunChanged))]
    public BaseGun currentGun;

    [Header("当前玩家触碰到的枪械")]
    public BaseGun CurrentTouchGun;

    [Header("准备状态")]
    [SyncVar]
    public bool IsPrepara = false;

    public void ChangePreparaState(bool State)
    {
        if (!isLocalPlayer) return;
        CmdChangePreparaState(State);
    }

    [Command]
    public void CmdChangePreparaState(bool State)
    {
        IsPrepara = State;

        if (PlayerRespawnManager.Instance != null)
        {
            PlayerRespawnManager.Instance.ServerHandlePlayerPrepareChange(connectionToClient, State);
        }
    }

    [SyncVar]
    public string PlayerName;

    [SyncVar]
    public Team CurrentTeam;//当前队伍


    [Command]
    public void ChangeTeam()//改变队伍
    {
        string teaName = null;
        if (CurrentTeam == Team.Red)
        {
            CurrentTeam = Team.Blue;
            teaName = "蓝队";
        }
        else
        {
            CurrentTeam = Team.Red;
            teaName = "红队";
        }
        //全局播报
        PlayerRespawnManager.Instance.SendGlobalMessage(PlayerName + "加入" + teaName, 1f);//进行一下播报

    }

    #region 缩放动画配置
    private float currentYScale; // 仅作用于MyBody的Y轴缩放
    private float targetYStretch;
    public float mainLerpSpeed = 2f;
    public float maxLerpSpeed = 15f;
    private Vector2 baseBodyScale; // 改为记录MyBody的初始缩放
    #endregion

    #region Mirror生命周期
    public override void OnStartServer()
    {
        base.OnStartServer();
        MyRigdboby = GetComponent<Rigidbody2D>();
        if (MyRigdboby == null)
        {
            Debug.LogError($"[服务器] 玩家{gameObject.name}缺少Rigidbody2D组件！", this);
            return;
        }

        MyRigdboby.bodyType = RigidbodyType2D.Dynamic;
        MyRigdboby.drag = 0.3f;
        MyRigdboby.gravityScale = 1f;
        Debug.Log($"[服务器] 初始化玩家{gameObject.name}刚体为Dynamic");

        if (string.IsNullOrEmpty(PlayerName))
        {
            PlayerName = $"玩家{connectionToClient.connectionId}";
            Debug.Log($"[服务器] 玩家{connectionToClient.connectionId}名称兜底：{PlayerName}");
        }
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        LocalPlayer = this;
        Debug.Log($"[本地客户端] 初始化本地玩家：{gameObject.name}");

        string localName = Main.PlayerName; // 你的本地名称
        if (string.IsNullOrEmpty(localName))
        {
            localName = $"玩家{Random.Range(1000, 9999)}"; // 本地兜底
        }
        CmdSyncPlayerName(localName); // 同步到服务端

        myStats = GetComponent<playerStats>();
        myInputSystem = GetComponent<MyPlayerInput>();

        UImanager.Instance.ShowPanel<PlayerPanel>();//显示UI
        Debug.Log("显示玩家UI");
        //开始摄像机跟随
        MyCameraControl.Instance.SetCameraMode_FollowPlayerMode(this.gameObject, true);

        if (myInputSystem != null)
        {
            myInputSystem.Initialize(this, myStats);
            Debug.Log("Player：本地玩家输入系统初始化完成！");
        }
        MyHandControl = playerHandPos.GetComponent<playerHandControl>();
        MyHandControl.ownerPlayer = LocalPlayer;

        if (PlayerRespawnManager.Instance.IsGameStart)
        {
            //重生后获取当前战备
            PlayerAndGameInfoManger.Instance.EquipCurrentSlot();
        }
    }

    public override void OnStopLocalPlayer()
    {
        base.OnStopLocalPlayer();
        if (LocalPlayer == this)
            LocalPlayer = null;
    }
    #endregion

    #region 名称同步Command（核心新增）
    /// <summary>
    /// 客户端请求服务端同步玩家名称
    /// </summary>
    [Command(requiresAuthority = false)]
    private void CmdSyncPlayerName(string newName)
    {
        // 服务端校验名称合法性
        if (string.IsNullOrEmpty(newName))
        {
            newName = $"玩家{connectionToClient.connectionId}";
        }
        // 服务端修改SyncVar，自动同步到所有客户端
        PlayerName = newName;
        Debug.Log($"[服务器] 同步玩家{connectionToClient.connectionId}名称：{newName}");
    }

    private void Update()
    {
        PlayerMoveStretchAnima();//应用缩放动画
    }
    #endregion

    #region 初始化
    public override void Awake()
    {
        base.Awake();
        MyRigdboby = GetComponent<Rigidbody2D>();

        if (MyBody != null)
        {
            baseBodyScale = MyBody.transform.localScale;
            currentYScale = baseBodyScale.y; // 初始Y轴缩放
        }
        else
        {
            Debug.LogError($"[Player] {gameObject.name} 的MyBody未赋值！", this);
            baseBodyScale = Vector2.one;
            currentYScale = 1f;
        }

        transform.localScale = Vector3.one;
    }
    #endregion

    #region 缩放动画
    public void PlayerMoveStretchAnima()
    {
        float currentXVel = MyRigdboby.velocity.x;
        float currentYVel = MyRigdboby.velocity.y;

        bool isGroundMove = Mathf.Abs(currentYVel) < 0.1f && Mathf.Abs(currentXVel) > 0.1f;
        bool isJumpStretch = Mathf.Abs(currentYVel) >= 0.1f;

        if (isGroundMove)
        {
            float bumpyOffset = Mathf.Sin(Time.time * myStats.MoveBumpySpeed) * myStats.MoveBumpyRange;
            targetYStretch = baseBodyScale.y + bumpyOffset;
            currentYScale = Mathf.Lerp(currentYScale, targetYStretch, 5f * Time.deltaTime);
        }
        else if (isJumpStretch)
        {
            float ySpeedRatio = Mathf.Abs(currentYVel) / myStats.MaxYSpeed;
            targetYStretch = baseBodyScale.y + ySpeedRatio * myStats.MaxYStretch;

            float scaleDelta = Mathf.Abs(targetYStretch - currentYScale);
            float dynamicLerpSpeed = Mathf.Lerp(mainLerpSpeed, maxLerpSpeed, ySpeedRatio + scaleDelta * 2f);
            currentYScale = Mathf.Lerp(currentYScale, targetYStretch, dynamicLerpSpeed * Time.deltaTime);
            currentYScale = Mathf.Max(currentYScale, baseBodyScale.y * 0.8f);
        }
        else
        {
            targetYStretch = baseBodyScale.y;
            currentYScale = Mathf.Lerp(currentYScale, targetYStretch, 2f * Time.deltaTime);
        }

        Vector3 bodyScale = MyBody.transform.localScale;
        bodyScale.x = Mathf.Sign(bodyScale.x) * Mathf.Abs(baseBodyScale.x);
        bodyScale.y = currentYScale; // 动态修改Y轴缩放
        MyBody.transform.localScale = bodyScale;
    }
    #endregion

    #region 枪械管理
    private int ViewTaskID = -1;//当前视野提升任务ID
    private float ChangeSpeed_View = 4;//视野变化速度
    private void OnGunChanged(BaseGun oldGun, BaseGun newGun)
    {
        if (!isClient || playerHandPos == null)
            return;

        if (oldGun != null)
        {
            oldGun.transform.SetParent(null); // 仅解挂载
            EventCenter.Instance.TriggerEvent(E_EventType.E_playerLoseGun, this);
        }

        if (newGun != null)
        {
            // 客户端：强制枪械X缩放为负
            Vector3 gunScale = newGun.transform.localScale;
            gunScale.x = -Mathf.Abs(gunScale.x);
            newGun.transform.localScale = gunScale;

            newGun.transform.SetParent(playerHandPos); // 仅挂载
            gunScale.x = -Mathf.Abs(gunScale.x);
            newGun.transform.localScale = gunScale;
            newGun.transform.localPosition = Vector3.zero;
            newGun.transform.localRotation = Quaternion.identity;
            EventCenter.Instance.TriggerEvent(E_EventType.E_playerGetGun, this);
        }

        if (!isLocalPlayer)
            return;

        // 对视野进行设置
        if (newGun != null)
        {
            UImanager.Instance.ShowPanel<PlayerPanel>().ShowGunBackGround();//设置UI
            Debug.Log($"[本地客户端] 玩家{gameObject.name}持有新枪械，调整视野");
            if (ViewTaskID != -1)
            {
                MyCameraControl.Instance.ResetZoomTask(ViewTaskID);
                ViewTaskID = -1;
            }
            float zoomPercent = 1 + newGun.gunInfo.ViewRange;//负数会自动缩放，正数会放大
            ViewTaskID = MyCameraControl.Instance.AddZoomTask_ByPercent_TemporaryManual(zoomPercent, ChangeSpeed_View);
            Debug.Log($"[本地客户端] 枪械缩放任务ID：{ViewTaskID}，百分比：{zoomPercent}");
        }
        else
        {
            UImanager.Instance.ShowPanel<PlayerPanel>().HideGunBackGround();//关闭UI显示
            //没有枪械了，视野恢复默认
            if (ViewTaskID != -1)
            {
                MyCameraControl.Instance.ResetZoomTask(ViewTaskID);
                ViewTaskID = -1;
            }
        }
    }

    public void PickUpSceneGun()
    {
        if (!isLocalPlayer)
            return;
        if (CurrentTouchGun == null)
            return;

        CurrentTouchGun.transform.SetParent(playerHandPos);
        CurrentTouchGun.transform.localPosition = Vector3.zero;
        CurrentTouchGun.transform.localRotation = Quaternion.identity;

        NetworkIdentity gunNetId = CurrentTouchGun.GetComponent<NetworkIdentity>();
        if (gunNetId == null)
            return;

        LocalPlayer.CmdPickUpSceneGun(gunNetId.netId);
        CurrentTouchGun = null;
    }

    [Command]
    private void CmdPickUpSceneGun(uint gunNetId)
    {
        if (!isServer)
            return;
        if (!NetworkServer.spawned.TryGetValue(gunNetId, out NetworkIdentity gunNetIdentity))
            return;
        //把当前的权限转移给这个玩家
        gunNetIdentity.RemoveClientAuthority();
        gunNetIdentity.AssignClientAuthority(connectionToClient);

        GameObject gunObj = gunNetIdentity.gameObject;
        BaseGun targetGun = gunObj.GetComponent<BaseGun>();
        if (targetGun == null)
        {
            NetworkServer.Destroy(gunObj);
            return;
        }

        if (!gunNetIdentity.isOwned)
        {
            gunNetIdentity.AssignClientAuthority(connectionToClient);
        }

        ServerHandlePickUpGun(gunObj);
    }

    public void SpawnAndPickGun(string gunName)
    {
        if (!isLocalPlayer)
            return;
        if (MilitaryManager.Instance == null)
            return;

        LocalPlayer.CmdSpawnAndPickGun(gunName);
        CountDownManager.Instance.CreateTimer(false, 1000, () => { currentGun.TriggerReload(); });
    }

    [Command]
    private void CmdSpawnAndPickGun(string gunName)
    {
        if (!isServer || MilitaryManager.Instance == null) return;

        GameObject gunPrefab = MilitaryManager.Instance.GetGun(gunName);
        if (gunPrefab == null) return;

        GameObject gunObj = Instantiate(gunPrefab);
        NetworkServer.Spawn(gunObj, connectionToClient);

        ServerHandlePickUpGun(gunObj);
    }

    private void ServerHandlePickUpGun(GameObject gunObj)
    {
        if (!isServer || gunObj == null || playerHandPos == null)
            return;

        BaseGun newGun = gunObj.GetComponent<BaseGun>();
        if (newGun == null)
        {
            NetworkServer.Destroy(gunObj);
            return;
        }

        if (currentGun != null)
        {
            ServerHandleDropGun(currentGun.gameObject);
        }

        // 服务器：强制枪械X缩放为负
        Vector3 gunScale = gunObj.transform.localScale;
        gunScale.x = -Mathf.Abs(gunScale.x); // 核心：强制X轴为负
        gunObj.transform.localScale = gunScale;

        // 再处理父对象、位置、旋转
        gunObj.transform.SetParent(playerHandPos);
        gunObj.transform.localPosition = Vector3.zero;
        gunObj.transform.localRotation = Quaternion.identity;

        newGun.isInPlayerHand = true;
        currentGun = newGun;
        newGun.ownerPlayer = this;

        newGun.SafeServerOnGunPicked();
    }

    public void DropCurrentGun()
    {
        if (!isLocalPlayer || currentGun == null)
            return;
        LocalPlayer.CmdDropCurrentGun();

    }

    [Command]
    private void CmdDropCurrentGun()
    {
        if (!isServer || currentGun == null) return;
        ServerHandleDropGun(currentGun.gameObject);
        currentGun = null;
    }

    // 原private改为public，添加[Server]特性限定仅服务器执行
    [Server]
    public void ServerHandleDropGun(GameObject gunObj)
    {
        if (!isServer || gunObj == null)
            return;

        NetworkIdentity gunNetId = gunObj.GetComponent<NetworkIdentity>();
        if (gunNetId != null && gunNetId.connectionToClient != null)
        {
            gunNetId.RemoveClientAuthority();
        }

        gunObj.transform.SetParent(null);

        BaseGun gun = gunObj.GetComponent<BaseGun>();
        if (gun != null)
        {
            gun.isInPlayerHand = false;
        }

        RpcResetGunTransform(gunNetId?.netId ?? 0, gunObj.transform.position, gunObj.transform.eulerAngles.z);
        gunObj.GetComponent<BaseGun>().SafeServerOnGunDropped();
        gunNetId.RemoveClientAuthority();//移除权限
    }


    [ClientRpc]
    private void RpcResetGunTransform(uint gunNetId, Vector3 worldPos, float rotZ)
    {
        if (!NetworkClient.spawned.TryGetValue(gunNetId, out NetworkIdentity gunNetIdentity))
            return;

        GameObject gunObj = gunNetIdentity.gameObject;
        // 直接设置位置，无插值
        gunObj.transform.position = worldPos;
        gunObj.transform.rotation = Quaternion.Euler(0, 0, rotZ);
    }
    #endregion

    #region 辅助方法
    public override void DestroyMe(float time = 0)
    {
        if (isServer)
        {
            Invoke(nameof(ServerDestroy), time);
        }
    }

    private void ServerDestroy()
    {
        NetworkServer.Destroy(gameObject);
    }
    #endregion

    public string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(PlayerName))
        {
            return PlayerName;
        }
        if (isServer)
        {
            return $"玩家{connectionToClient?.connectionId ?? -1}";
        }
        else
        {
            return Main.PlayerName ?? $"玩家{Random.Range(1000, 9999)}";
        }
    }

    public void RequestRefreshTeamUI()
    {
        if (!isLocalPlayer) return;
        CmdTellServerToRefreshTeam();
    }

    [Command]
    private void CmdTellServerToRefreshTeam()
    {
        // 服务器逻辑：直接让管理器刷新
        if (PlayerRespawnManager.Instance != null)
        {
            PlayerRespawnManager.Instance.ServerUpdateTeamInfo();
        }
    }

    #region 调用游戏开始

    [Command]
    public void CmdRequestStartGame()
    {
        if (PlayerRespawnManager.Instance != null)
        {
            var playerRespawnManager = PlayerRespawnManager.Instance;
            playerRespawnManager.NoticePlayerGameStart();
            //初始化游戏数据
            playerRespawnManager.InitGameData();//初始化游戏数据记录队伍
        }
    }

    #endregion
}
