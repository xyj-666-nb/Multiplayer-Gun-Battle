using DG.Tweening;
using Mirror;
using UnityEngine;
using UnityEngine.Playables;
using System.Text;

public class Player : Base_Entity
{
    public static Player LocalPlayer { get; private set; }

    [Header("自己的身体")]
    public GameObject MyBody; // 只缩放这个物体

    [Header("核心组件")]
    public playerStats myStats;
    public MyPlayerInput myInputSystem;
    public PlayerSortingLayerControl mySortingLayerControl;

    [Header("枪械挂载")]
    public Transform playerHandPos;
    public playerHandControl MyHandControl;
    [SyncVar(hook = nameof(OnGunChanged))]
    public BaseGun currentGun;

    [Header("是否进入房屋")]
    [SyncVar(hook = nameof(OnChangeEnterRoomState))]
    public bool IsEnterRoom = false;//是否进入房屋

    [Header("当前玩家触碰到的枪械")]
    public BaseGun CurrentTouchGun;

    [Header("当前玩家穿戴的护甲")]
    [SyncVar(hook = (nameof(OnChangeArmorState)))]
    public ArmorType CurrentArmorType = ArmorType.Empty_handed;

    [Header("护甲控制")]
    public SpriteRenderer ArmorSprite;//护甲图片
    public SpriteRenderer HelmetSprite;//头盔图片
    [Header("护甲动画")]
    public PlayableDirector TimeLine_Helmet;//头盔动画

    [Header("准备状态")]
    [SyncVar]
    public bool IsPrepara = false;

    [SyncVar]
    public string PlayerName;

    [SyncVar(hook = nameof(OnChangeTeam))]
    public Team CurrentTeam;//当前队伍

    #region 缓存变量（核心优化：缓存复用+GC减少）
    // 单例缓存：避免反复Instance调用（高频函数优化）
    private UImanager _uiManager;
    private MyCameraControl _myCameraControl;
    private MilitaryManager _militaryManager;
    private PlayerRespawnManager _playerRespawnManager;
    private CountDownManager _countDownManager;
    private GlobalPictureFlipManager _globalPictureFlipManager;

    // UI面板缓存：避免反复GetPanel（缓存复用）
    private PlayerPanel _playerPanel;
    private PlayerPreparaPanel _playerPreparaPanel;
    private GameScorePanel _gameScorePanel;

    // 缩放动画缓存：减少临时变量（高频函数优化+GC减少）
    private float _cachedTime;
    private float _cachedXVel;
    private float _cachedYVel;
    private float _cachedYSpeedRatio;
    private Vector3 _cachedBodyScale;

    // 字符串缓存：减少拼接GC（GC减少）
    private StringBuilder _sb = new StringBuilder();
    #endregion

    #region 缩放动画配置（保留原变量）
    private float currentYScale; // 仅作用于MyBody的Y轴缩放
    private float targetYStretch;
    public float mainLerpSpeed = 2f;
    public float maxLerpSpeed = 15f;
    private Vector2 baseBodyScale; // 改为记录MyBody的初始缩放

    private Vector3 lastPosition;      // 上一帧的坐标
    private Vector2 estimatedVelocity; // 估算速度（用于网络同步平滑）
    #endregion

    #region 其他变量（保留原逻辑）
    private int ViewTaskID = -1;//当前视野提升任务ID
    private float ChangeSpeed_View = 4;//视野变化速度
    #endregion

    #region 初始化（优化：缓存复用+逻辑冗余）
    public override void Awake()
    {
        base.Awake();

        // 优化1：缓存单例（仅1次获取，避免反复Instance调用）
        _uiManager = UImanager.Instance;
        _myCameraControl = MyCameraControl.Instance;
        _militaryManager = MilitaryManager.Instance;
        _playerRespawnManager = PlayerRespawnManager.Instance;
        _countDownManager = CountDownManager.Instance;
        _globalPictureFlipManager = GlobalPictureFlipManager.Instance;

        // 优化2：初始化MyBody缩放，避免重复判空（逻辑冗余）
        if (MyBody != null)
        {
            baseBodyScale = MyBody.transform.localScale;
            currentYScale = baseBodyScale.y;
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

    #region Mirror生命周期（优化：缓存复用+逻辑冗余+GC减少）
    public override void OnStartServer()
    {
        base.OnStartServer();

        // 优化1：复用Base_Entity的MyRigdboby，避免重复GetComponent（逻辑冗余）
        if (MyRigdboby == null)
        {
            Debug.LogError($"[服务器] 玩家{gameObject.name}缺少Rigidbody2D组件！", this);
            return;
        }

        MyRigdboby.drag = 0.3f;
        MyRigdboby.gravityScale = 1f;

        if (string.IsNullOrEmpty(PlayerName))
        {
            _sb.Clear();
            _sb.Append("玩家").Append(connectionToClient.connectionId);
            PlayerName = _sb.ToString();
            Debug.Log($"[服务器] 玩家{connectionToClient.connectionId}名称兜底：{PlayerName}");
        }
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        LocalPlayer = this;
        Debug.Log($"[本地客户端] 初始化本地玩家：{gameObject.name}");

        // 优化1：缓存组件，避免反复GetComponent（缓存复用）
        if (myStats == null) myStats = GetComponent<playerStats>();
        if (myInputSystem == null) myInputSystem = GetComponent<MyPlayerInput>();
        if (MyHandControl == null && playerHandPos != null)
            MyHandControl = playerHandPos.GetComponent<playerHandControl>();

        // 优化2：StringBuilder拼接字符串，减少GC（GC减少）
        string localName = UOSRelaySimple.Instance.playerName;
        if (string.IsNullOrEmpty(localName))
        {
            _sb.Clear();
            _sb.Append("玩家").Append(Random.Range(1000, 9999));
            localName = _sb.ToString();
        }
        CmdSyncPlayerName(localName);

        // 优化3：缓存UI面板，避免反复GetPanel（缓存复用）
        if (_uiManager != null)
        {
            _playerPanel = _uiManager.ShowPanel<PlayerPanel>();
            _playerPreparaPanel = _uiManager.GetPanel<PlayerPreparaPanel>();
            _gameScorePanel = _uiManager.GetPanel<GameScorePanel>();
        }

        // 保留原逻辑
        if (myInputSystem != null)
        {
            myInputSystem.Initialize(this, myStats);
            Debug.Log("Player：本地玩家输入系统初始化完成！");
        }

        if (MyHandControl != null)
        {
            MyHandControl.ownerPlayer = LocalPlayer;
            if (TouchInputHandler.Instance != null)
                TouchInputHandler.Instance.GetPlayerHand(MyHandControl);
        }

        if (_myCameraControl != null)
            _myCameraControl.SetCameraMode_FollowPlayerMode(this.gameObject, true);

        if (_playerRespawnManager != null && _playerRespawnManager.IsGameStart)
        {
            PlayerAndGameInfoManger.Instance.EquipCurrentSlot();
            if (_globalPictureFlipManager != null)
                _globalPictureFlipManager.TriggerGlobalFlip(CurrentTeam == Team.Blue);
        }

        if (_gameScorePanel != null)
            _gameScorePanel.ChangeTeamSprite(CurrentTeam);
    }

    public override void OnStopLocalPlayer()
    {
        base.OnStopLocalPlayer();
        if (LocalPlayer == this)
            LocalPlayer = null;
    }
    #endregion

    #region 核心业务逻辑（优化：缓存复用+逻辑冗余+GC减少）
    // 补充弹药（保留原逻辑）
    public void CmdBulletSupplement()
    {
        currentGun?.CmdBulletSupplement();
    }

    [Command]
    public void CmdChangeEnterRoomState(bool isEnterRoom)
    {
        IsEnterRoom = isEnterRoom;
    }

    private void OnChangeEnterRoomState(bool OldValue, bool NewValue)
    {
        // 优化：空值防护提前做，减少重复判空（逻辑冗余）
        mySortingLayerControl?.SetSortingLayer(NewValue);
    }

    private void OnChangeArmorState(ArmorType OldType, ArmorType NewType)
    {
        // 优化：缓存myStats，避免反复GetComponent（缓存复用）
        if (myStats == null) myStats = GetComponent<playerStats>();
        if (myStats == null) return;

        // 保留原逻辑
        if (OldType != ArmorType.Empty_handed)
        {
            var oldInfo = _militaryManager?.GetArmorInfoPack(OldType);
            if (oldInfo != null) myStats.RemoveArmorEffect(oldInfo);
        }

        var newInfo = _militaryManager?.GetArmorInfoPack(NewType);
        if (newInfo == null) return;

        if (ArmorSprite != null) ArmorSprite.sprite = newInfo.ArmorSprite;
        if (HelmetSprite != null) HelmetSprite.sprite = newInfo.HelmetSprite;

        if (NewType != ArmorType.Empty_handed)
        {
            myStats.AddArmorEffect(newInfo);
        }

        WearArmorAnimatorStart();
    }

    public void WearArmorAnimatorStart()
    {
        // 优化：空值防护提前做，减少重复判空（逻辑冗余）
        if (TimeLine_Helmet == null || ArmorSprite == null) return;

        // 保留原逻辑
        TimeLine_Helmet.time = 0;
        TimeLine_Helmet.Play();
        ArmorSprite.DOKill();
        ArmorSprite.color = ColorManager.SetColorAlpha(ArmorSprite.color, 0);
        ArmorSprite.DOFade(1, 1.5f);
    }

    public void getArmor(ArmorType Type)
    {
        CmdGetArmor(Type);
    }

    [Command]
    public void CmdGetArmor(ArmorType Type)
    {
        CurrentArmorType = Type;
    }

    public void ChangePreparaState(bool State)
    {
        if (!isLocalPlayer) return;
        CmdChangePreparaState(State);
    }

    [Command]
    public void CmdChangePreparaState(bool State)
    {
        IsPrepara = State;
        // 优化：缓存单例，避免反复Instance调用（缓存复用）
        _playerRespawnManager?.ServerHandlePlayerPrepareChange(connectionToClient, State);
    }

    private void OnChangeTeam(Team oldValue, Team newValue)
    {
        if (oldValue == newValue || !isLocalPlayer)
            return;

        // 优化：复用缓存的UI面板，避免反复GetPanel（缓存复用）
        _playerPreparaPanel?.ChangeTeamSprite(newValue);
        _gameScorePanel?.ChangeTeamSprite(oldValue);
    }

    [Command]
    public void ChangeTeam()
    {
        // 优化：StringBuilder拼接字符串，减少GC（GC减少）
        if (CurrentTeam == Team.Red)
        {
            CurrentTeam = Team.Blue;
            _sb.Clear();
            _sb.Append(PlayerName).Append("加入蓝队");
        }
        else
        {
            CurrentTeam = Team.Red;
            _sb.Clear();
            _sb.Append(PlayerName).Append("加入红队");
        }
        // 优化：缓存单例，避免反复Instance调用（缓存复用）
        _playerRespawnManager?.SendGlobalMessage(_sb.ToString(), 1f);
    }

    [Command(requiresAuthority = false)]
    private void CmdSyncPlayerName(string newName)
    {
        if (string.IsNullOrEmpty(newName))
        {
            // 优化：StringBuilder拼接字符串，减少GC（GC减少）
            _sb.Clear();
            _sb.Append("玩家").Append(connectionToClient.connectionId);
            newName = _sb.ToString();
        }
        PlayerName = newName;
        Debug.Log($"[服务器] 同步玩家{connectionToClient.connectionId}名称：{newName}");
    }
    #endregion

    #region 缩放动画（优化：高频函数+GC减少+缓存复用）
    private void Update()
    {
        PlayerMoveStretchAnima();
    }

    public void PlayerMoveStretchAnima()
    {
        // 优化：空值防护提前做，减少重复判空（逻辑冗余）
        if (MyRigdboby == null || MyBody == null) return;

        // 优化：缓存高频访问的变量，减少属性调用（高频函数优化）
        _cachedXVel = MyRigdboby.velocity.x;
        _cachedYVel = MyRigdboby.velocity.y;
        _cachedTime = Time.time;

        bool isGroundMove = Mathf.Abs(_cachedYVel) < 0.1f && Mathf.Abs(_cachedXVel) > 0.1f;
        bool isJumpStretch = Mathf.Abs(_cachedYVel) >= 0.1f;

        if (isGroundMove)
        {
            // 优化：缓存计算结果，减少重复Mathf调用（高频函数优化）
            targetYStretch = baseBodyScale.y + Mathf.Sin(_cachedTime * myStats.MoveBumpySpeed) * myStats.MoveBumpyRange;
            currentYScale = Mathf.Lerp(currentYScale, targetYStretch, 5f * Time.deltaTime);
        }
        else if (isJumpStretch)
        {
            // 优化：缓存计算结果，减少重复Mathf调用（高频函数优化）
            _cachedYSpeedRatio = Mathf.Abs(_cachedYVel) / myStats.MaxYSpeed;
            targetYStretch = baseBodyScale.y + _cachedYSpeedRatio * myStats.MaxYStretch;

            float scaleDelta = Mathf.Abs(targetYStretch - currentYScale);
            float dynamicLerpSpeed = Mathf.Lerp(mainLerpSpeed, maxLerpSpeed, _cachedYSpeedRatio + scaleDelta * 2f);
            currentYScale = Mathf.Lerp(currentYScale, targetYStretch, dynamicLerpSpeed * Time.deltaTime);
            currentYScale = Mathf.Max(currentYScale, baseBodyScale.y * 0.8f);
        }
        else
        {
            targetYStretch = baseBodyScale.y;
            currentYScale = Mathf.Lerp(currentYScale, targetYStretch, 2f * Time.deltaTime);
        }

        // 优化：缓存缩放向量，减少临时Vector3创建（GC减少）
        _cachedBodyScale = MyBody.transform.localScale;
        _cachedBodyScale.x = Mathf.Sign(_cachedBodyScale.x) * Mathf.Abs(baseBodyScale.x);
        _cachedBodyScale.y = currentYScale;
        MyBody.transform.localScale = _cachedBodyScale;
    }
    #endregion

    #region 枪械管理（优化：缓存复用+逻辑冗余）
    private void OnGunChanged(BaseGun oldGun, BaseGun newGun)
    {
        if (!isClient || playerHandPos == null)
            return;

        // 保留原逻辑
        if (oldGun != null)
        {
            oldGun.transform.SetParent(null);
            EventCenter.Instance.TriggerEvent(E_EventType.E_playerLoseGun, this);
            mySortingLayerControl.RemoveSpriteRendererFromManager(oldGun.GetComponent<SpriteRenderer>());
        }

        if (newGun != null)
        {
            Vector3 gunScale = newGun.transform.localScale;
            gunScale.x = -Mathf.Abs(gunScale.x);
            newGun.transform.localScale = gunScale;

            newGun.transform.SetParent(playerHandPos);
            newGun.transform.localPosition = Vector3.zero;
            newGun.transform.localRotation = Quaternion.identity;
            EventCenter.Instance.TriggerEvent(E_EventType.E_playerGetGun, this);
            mySortingLayerControl.AddSpriteRendererInManager(newGun.GetComponent<SpriteRenderer>());
        }

        if (!isLocalPlayer)
            return;

        // 优化：复用缓存的UI面板，避免反复GetPanel（缓存复用）
        if (newGun != null)
        {
            _playerPanel?.ShowGunBackGround();
            Debug.Log($"[本地客户端] 玩家{gameObject.name}持有新枪械，调整视野");
            if (ViewTaskID != -1)
            {
                _myCameraControl?.ResetZoomTask(ViewTaskID);
                ViewTaskID = -1;
            }
            float zoomPercent = 1 + newGun.gunInfo.ViewRange;
            ViewTaskID = _myCameraControl?.AddZoomTask_ByPercent_TemporaryManual(zoomPercent, ChangeSpeed_View) ?? -1;
            Debug.Log($"[本地客户端] 枪械缩放任务ID：{ViewTaskID}，百分比：{zoomPercent}");
        }
        else
        {
            _playerPanel?.HideGunBackGround();
            if (ViewTaskID != -1)
            {
                _myCameraControl?.ResetZoomTask(ViewTaskID);
                ViewTaskID = -1;
            }
        }
    }

    public void PickUpSceneGun()
    {
        if (!isLocalPlayer || CurrentTouchGun == null)
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
        if (!isLocalPlayer || _militaryManager == null)
            return;

        LocalPlayer.CmdSpawnAndPickGun(gunName);
        // 优化：缓存单例，避免反复Instance调用（缓存复用）
        _countDownManager?.CreateTimer(false, 500, () => {
            if (currentGun != null)
                currentGun.TriggerReload();
            PlayerAndGameInfoManger.Instance.ShowTactic();
        });
    }

    [Command]
    private void CmdSpawnAndPickGun(string gunName)
    {
        if (!isServer || _militaryManager == null) return;

        GameObject gunPrefab = _militaryManager.GetGun(gunName);
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
            ServerHandleDropGun(currentGun.gameObject, false);
        }

        Vector3 gunScale = gunObj.transform.localScale;
        gunScale.x = -Mathf.Abs(gunScale.x);
        gunObj.transform.localScale = gunScale;

        gunObj.transform.SetParent(playerHandPos);
        gunObj.transform.localPosition = Vector3.zero;
        gunObj.transform.localRotation = Quaternion.identity;

        newGun.isInPlayerHand = true;
        currentGun = newGun;
        newGun.ownerPlayer = this;

        newGun.SafeServerOnGunPicked();
    }

    public void DropCurrentGun(bool IsDestroy = false)
    {
        if (!isLocalPlayer || currentGun == null)
            return;
        LocalPlayer.CmdDropCurrentGun(IsDestroy);
    }

    [Command]
    private void CmdDropCurrentGun(bool IsDestroy)
    {
        if (!isServer || currentGun == null)
            return;
        ServerHandleDropGun(currentGun.gameObject, IsDestroy);
        currentGun = null;
    }

    [Server]
    public void ServerHandleDropGun(GameObject gunObj, bool IsDestroy)
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
        gunNetId.RemoveClientAuthority();
        if (IsDestroy)
            NetworkServer.Destroy(gunObj);
    }

    [ClientRpc]
    private void RpcResetGunTransform(uint gunNetId, Vector3 worldPos, float rotZ)
    {
        if (!NetworkClient.spawned.TryGetValue(gunNetId, out NetworkIdentity gunNetIdentity))
            return;

        GameObject gunObj = gunNetIdentity.gameObject;
        gunObj.transform.position = worldPos;
        gunObj.transform.rotation = Quaternion.Euler(0, 0, rotZ);
    }
    #endregion

    #region 辅助方法（优化：逻辑冗余+缓存复用）
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

    public string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(PlayerName))
        {
            return PlayerName;
        }
        if (isServer)
        {
            // 优化：StringBuilder拼接字符串，减少GC（GC减少）
            _sb.Clear();
            _sb.Append("玩家").Append(connectionToClient?.connectionId ?? -1);
            return _sb.ToString();
        }
        else
        {
            string localName = UOSRelaySimple.Instance?.playerName;
            if (string.IsNullOrEmpty(localName))
            {
                _sb.Clear();
                _sb.Append("玩家").Append(Random.Range(1000, 9999));
                localName = _sb.ToString();
            }
            return localName;
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
        // 优化：缓存单例，避免反复Instance调用（缓存复用）
        _playerRespawnManager?.ServerUpdateTeamInfo();
    }

    [Command]
    public void CmdRequestStartGame()
    {
        // 优化：缓存单例，避免反复Instance调用（缓存复用）
        if (_playerRespawnManager != null)
        {
            _playerRespawnManager.NoticePlayerGameStart();
            _playerRespawnManager.InitGameData();
        }
    }

    public void Transmit(Vector3 Pos)
    {
        Player.LocalPlayer.transform.position = Pos;
    }
    #endregion

    #region 销毁清理（优化：GC减少+逻辑冗余）
    protected  void OnDestroy()
    {

        if (ArmorSprite != null) ArmorSprite.DOKill();

        CancelInvoke();

        // 优化3：清空缓存，避免内存泄漏（GC减少）
        _uiManager = null;
        _myCameraControl = null;
        _militaryManager = null;
        _playerRespawnManager = null;
        _countDownManager = null;
        _globalPictureFlipManager = null;
        _playerPanel = null;
        _playerPreparaPanel = null;
        _gameScorePanel = null;
        _sb = null;
    }
    #endregion
}

