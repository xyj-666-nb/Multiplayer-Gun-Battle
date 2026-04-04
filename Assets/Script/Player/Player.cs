using DG.Tweening;
using Mirror;
using UnityEngine;
using UnityEngine.Playables;
using System.Text;
using static Player;

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
    private BaseGun _currentTouchGun;

    [Header("护盾组件")]
    public ReBornShield reBornShield;

    [Header("表情控制系统")]
    public playerWorldExpressionSystem MyExpressionSystem;//玩家表情系统

    public void TriggerExpression(int ExpressionID)
    {
        MyExpressionSystem.CmdPlayExpression(ExpressionID);
    }

    public void TriggerShield()
    {
        reBornShield.TriggerShield();//触发护盾
    }

    // 公开属性
    public BaseGun CurrentTouchGun
    {
        get
        {
            return _currentTouchGun;
        }
        set
        {
            _currentTouchGun = value;
            if (isLocalPlayer)
                UImanager.Instance.GetPanel<PlayerPanel>().IsTriggerPickUpGunButton(value); //只要是本地玩家触碰到了枪械就触发UI显示
        }
    }

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

    #region 缓存变量
    private UImanager _uiManager;
    private MyCameraControl _myCameraControl;
    private MilitaryManager _militaryManager;
    private PlayerRespawnManager _playerRespawnManager;
    private CountDownManager _countDownManager;
    private GlobalPictureFlipManager _globalPictureFlipManager;

    // UI面板缓存：避免反复GetPanel
    private PlayerPanel _playerPanel;
    private PlayerPreparaPanel _playerPreparaPanel;
    private GameScorePanel _gameScorePanel;

    // 缩放动画缓存：减少临时变量
    private float _cachedTime;
    private float _cachedXVel;
    private float _cachedYVel;
    private float _cachedYSpeedRatio;
    private Vector3 _cachedBodyScale;

    // 字符串缓存：减少拼接GC
    private StringBuilder _sb = new StringBuilder();
    #endregion

    #region 缩放动画配置
    private float currentYScale; // 仅作用于MyBody的Y轴缩放
    private float targetYStretch;
    public float mainLerpSpeed = 2f;
    public float maxLerpSpeed = 15f;
    private Vector2 baseBodyScale; // 改为记录MyBody的初始缩放

    private Vector3 lastPosition;      // 上一帧的坐标
    private Vector2 estimatedVelocity; // 估算速度
    #endregion

    #region 其他变量
    private int ViewTaskID = -1;//当前视野提升任务ID
    private float ChangeSpeed_View = 4;//视野变化速度
    #endregion

    #region 清除身上所有的物体

    [Command]
    public void CmdClearAllPlayerObj()
    {
        if(currentGun!=null)
        {
            //清除
            NetworkServer.Destroy(currentGun.gameObject);
        }
        CurrentArmorType = ArmorType.Empty_handed;//清理护甲
        MyHandControl.ClearAllHandObj();//清理投掷物
    }

    #endregion

    #region 初始化
    public override void Awake()
    {
        base.Awake();

        _uiManager = UImanager.Instance;
        _myCameraControl = MyCameraControl.Instance;
        _militaryManager = MilitaryManager.Instance;
        _playerRespawnManager = PlayerRespawnManager.Instance;
        _countDownManager = CountDownManager.Instance;
        _globalPictureFlipManager = GlobalPictureFlipManager.Instance;


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
        Debug.Log("护盾触发");
        reBornShield.TriggerShield();//重生就触发护盾
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("护盾触发");
        reBornShield.TriggerShield();//重生就触发护盾
    }

    #endregion

    #region Mirror生命周期（优化：缓存复用+逻辑冗余+GC减少）
    public override void OnStartServer()
    {
        base.OnStartServer();

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

        if (myStats == null) myStats = GetComponent<playerStats>();
        if (myInputSystem == null) myInputSystem = GetComponent<MyPlayerInput>();
        if (MyHandControl == null && playerHandPos != null)
            MyHandControl = playerHandPos.GetComponent<playerHandControl>();

        string localName = UOSRelaySimple.Instance.playerName;
        if (string.IsNullOrEmpty(localName))
        {
            _sb.Clear();
            _sb.Append("玩家").Append(Random.Range(1000, 9999));
            localName = _sb.ToString();
        }
        CmdSyncPlayerName(localName);

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

    #region 核心业务逻辑
    // 补充弹药
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
        mySortingLayerControl?.SetSortingLayer(NewValue);
    }

    private void OnChangeArmorState(ArmorType OldType, ArmorType NewType)
    {
        if (myStats == null) 
            myStats = GetComponent<playerStats>();
        if (myStats == null)
            return;

     
        // 保留原逻辑
        if (OldType != ArmorType.Empty_handed)
        {
            var oldInfo = _militaryManager?.GetArmorInfoPack(OldType);
            if (oldInfo != null)
                myStats.RemoveArmorEffect(oldInfo);
        }
        else
        {
            //如果为空就直接清除身上的图片
            ArmorSprite.sprite = null;
            HelmetSprite.sprite = null;
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

        if (TimeLine_Helmet == null || ArmorSprite == null)
            return;

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
        _playerRespawnManager?.ServerHandlePlayerPrepareChange(connectionToClient, State);
    }

    private void OnChangeTeam(Team oldValue, Team newValue)
    {
        if (oldValue == newValue || !isLocalPlayer)
            return;

        _playerPreparaPanel?.ChangeTeamSprite(newValue);
        _gameScorePanel?.ChangeTeamSprite(oldValue);
    }

    [Command]
    public void ChangeTeam()
    {
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
        _playerRespawnManager?.SendGlobalMessage(_sb.ToString(), 1f);
    }

    [Command(requiresAuthority = false)]
    private void CmdSyncPlayerName(string newName)
    {
        if (string.IsNullOrEmpty(newName))
        {
            _sb.Clear();
            _sb.Append("玩家").Append(connectionToClient.connectionId);
            newName = _sb.ToString();
        }
        PlayerName = newName;
        Debug.Log($"[服务器] 同步玩家{connectionToClient.connectionId}名称：{newName}");
    }
    #endregion

    #region 缩放动画
    private void Update()
    {
        PlayerMoveStretchAnima();
    }

    public void PlayerMoveStretchAnima()
    {
        if (MyRigdboby == null || MyBody == null) 
            return;

        _cachedXVel = MyRigdboby.velocity.x;
        _cachedYVel = MyRigdboby.velocity.y;
        _cachedTime = Time.time;

        bool isGroundMove = Mathf.Abs(_cachedYVel) < 0.1f && Mathf.Abs(_cachedXVel) > 0.1f;
        bool isJumpStretch = Mathf.Abs(_cachedYVel) >= 0.1f;

        if (isGroundMove)
        {
            targetYStretch = baseBodyScale.y + Mathf.Sin(_cachedTime * myStats.MoveBumpySpeed) * myStats.MoveBumpyRange;
            currentYScale = Mathf.Lerp(currentYScale, targetYStretch, 5f * Time.deltaTime);
        }
        else if (isJumpStretch)
        {

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

        _cachedBodyScale = MyBody.transform.localScale;
        _cachedBodyScale.x = Mathf.Sign(_cachedBodyScale.x) * Mathf.Abs(baseBodyScale.x);
        _cachedBodyScale.y = currentYScale;
        MyBody.transform.localScale = _cachedBodyScale;
    }
    #endregion

    #region 枪械管理
    private void OnGunChanged(BaseGun oldGun, BaseGun newGun)
    {
        if ( playerHandPos == null)
            return;

        // 保留原逻辑
        if (oldGun != null)
        {
            oldGun.transform.SetParent(null);
            EventCenter.Instance.TriggerEvent(E_EventType.E_playerLoseGun, this);
            mySortingLayerControl.RemoveSpriteRendererFromManager(oldGun.GetComponent<SpriteRenderer>());
            //判断当前是否处于换弹
           if(UImanager.Instance.GetPanel<PlayerPanel>().IsInReloadProcess)
           {
                UImanager.Instance.GetPanel<PlayerPanel>().StopReloadPrompt();
           }
        }

        if (newGun != null)
        {
            Vector3 gunScale = newGun.transform.localScale;

            newGun.transform.SetParent(playerHandPos);
            gunScale.x = -Mathf.Abs(gunScale.x);
            newGun.transform.localScale = gunScale;
            newGun.transform.localPosition = Vector3.zero;
            newGun.transform.localRotation = Quaternion.identity;
            EventCenter.Instance.TriggerEvent(E_EventType.E_playerGetGun, this);
            mySortingLayerControl.AddSpriteRendererInManager(newGun.GetComponent<SpriteRenderer>());
        }

        if (!isLocalPlayer)
            return;

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
            ViewTaskID = _myCameraControl?.AddZoomTask_ByPercent_TemporaryManual(zoomPercent, ChangeSpeed_View, (value) => {
                CameraSizeBaseValue=value;//复制基础值
            }) ?? -1;
            Debug.Log($"[本地客户端] 枪械缩放任务ID：{ViewTaskID}，百分比：{zoomPercent}");        }
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

    public float CameraSizeBaseValue;//当前摄像机大小基础值

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


        GunType gunType = MilitaryManager.Instance.GetGunType(gunName);
        GunSkinInfo skinInfo = new GunSkinInfo();//声明结构体
        var bulletConfig = GameSkinManager.Instance.ReturnBulletVisualConfig(gunType);
        var flashConfig = GameSkinManager.Instance.ReturnMuzzleFlashConfig(gunType);

        // 做空值保护，避免空引用报错
        skinInfo.BulletID = bulletConfig != null ? bulletConfig.BulletID : 0;
        skinInfo.MuzzleFlashID = flashConfig != null ? flashConfig.MuzzleFlashID : 0;

        LocalPlayer.CmdSpawnAndPickGun(gunName, skinInfo);

        _countDownManager?.CreateTimer(false, 100, () => {
            if (currentGun != null)
                currentGun.TriggerReload();
            PlayerAndGameInfoManger.Instance.ShowTactic();
        });
    }

    public struct GunSkinInfo
    {
        public int MuzzleFlashID;
        public int BulletID;//子弹ID
    }

    [Command]
    private void CmdSpawnAndPickGun(string gunName, GunSkinInfo skinInfo)
    {
        if (!isServer || _militaryManager == null)
            return;

        GameObject gunPrefab = _militaryManager.GetGun(gunName);
        if (gunPrefab == null) return;

        GameObject gunObj = Instantiate(gunPrefab);
        NetworkServer.Spawn(gunObj, connectionToClient);

        BaseGun gun = gunObj.GetComponent<BaseGun>();
        gun.SetGunConfig(skinInfo.MuzzleFlashID, skinInfo.BulletID);

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

    public string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(PlayerName))
        {
            return PlayerName;
        }
        if (isServer)
        {
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
        _playerRespawnManager?.ServerUpdateTeamInfo();
    }

    [Command]
    public void CmdRequestStartGame()
    {
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

    #region 销毁清理


    protected override void DeserializeSyncVars(NetworkReader reader, bool initialState)
    {
        base.DeserializeSyncVars(reader, initialState);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (ArmorSprite != null) 
            ArmorSprite.DOKill();

        CancelInvoke();

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

