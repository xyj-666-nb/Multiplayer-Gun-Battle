using Mirror;
using UnityEngine;

public class Player : Base_Entity
{
    public static Player LocalPlayer { get; private set; }

    [Header("核心组件")]
    public playerStats myStats;
    private MyPlayerInput myInputSystem;

    [Header("枪械挂载")]
    public Transform playerHandPos;
    public playerHandControl MyHandControl;
    [SyncVar(hook = nameof(OnGunChanged))]
    public BaseGun currentGun;

    [Header("当前玩家触碰到的枪械")]
    public BaseGun CurrentTouchGun;

    #region 缩放动画配置
    private float currentYScale;
    private float targetYStretch;
    public float mainLerpSpeed = 2f;
    public float maxLerpSpeed = 15f;
    private Vector2 baseScale;
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
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        LocalPlayer = this;
        Debug.Log($"[本地客户端] 初始化本地玩家：{gameObject.name}");

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
    }

    public override void OnStopLocalPlayer()
    {
        base.OnStopLocalPlayer();
        if (LocalPlayer == this)
            LocalPlayer = null;
    }
    #endregion

    #region 初始化
    public override void Awake()
    {
        base.Awake();
        baseScale = transform.localScale;
        currentYScale = baseScale.y;
        MyRigdboby = GetComponent<Rigidbody2D>();
    }
    #endregion

    #region 动画更新
    private void Update()
    {
        PlayerMoveStretchAnima();
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
            targetYStretch = baseScale.y + bumpyOffset;
            currentYScale = Mathf.Lerp(currentYScale, targetYStretch, 5f * Time.deltaTime);
        }
        else if (isJumpStretch)
        {
            float ySpeedRatio = Mathf.Abs(currentYVel) / myStats.MaxYSpeed;
            targetYStretch = baseScale.y + ySpeedRatio * myStats.MaxYStretch;

            float scaleDelta = Mathf.Abs(targetYStretch - currentYScale);
            float dynamicLerpSpeed = Mathf.Lerp(mainLerpSpeed, maxLerpSpeed, ySpeedRatio + scaleDelta * 2f);
            currentYScale = Mathf.Lerp(currentYScale, targetYStretch, dynamicLerpSpeed * Time.deltaTime);
            currentYScale = Mathf.Max(currentYScale, baseScale.y * 0.8f);
        }
        else
        {
            targetYStretch = baseScale.y;
            currentYScale = Mathf.Lerp(currentYScale, targetYStretch, 2f * Time.deltaTime);
        }

        transform.localScale = new Vector3(transform.localScale.x, currentYScale, transform.localScale.z);
    }
    #endregion

    #region 枪械管理
    // 钩子仅做「挂载/解挂载」，刚体状态由BaseGun的SyncVar钩子自动同步

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
            gunScale.x = -Mathf.Abs(gunScale.x); // 核心：强制X轴为负
            newGun.transform.localScale = gunScale;

            newGun.transform.SetParent(playerHandPos); // 仅挂载
            newGun.transform.localPosition = Vector3.zero;
            newGun.transform.localRotation = Quaternion.identity;
            EventCenter.Instance.TriggerEvent(E_EventType.E_playerGetGun, this);
        }


        if (!isLocalPlayer) return;

        // 对视野进行设置
        if (newGun != null)
        {
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
        if (GunManager.Instance == null)
            return;

        LocalPlayer.CmdSpawnAndPickGun(gunName);
        if (UImanager.Instance.GetPanel<PlayerPanel>() != null)
        {
            UImanager.Instance.ShowPanel<PlayerPanel>().ShowGunBackGround();
        }
    }

    [Command]
    private void CmdSpawnAndPickGun(string gunName)
    {
        if (!isServer || GunManager.Instance == null) return;

        GameObject gunPrefab = GunManager.Instance.GetGun(gunName);
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
        //UI更新
        if (UImanager.Instance.GetPanel<PlayerPanel>() != null)
        {
            UImanager.Instance.ShowPanel<PlayerPanel>().ShowGunBackGround();
        }
    }

    public void DropCurrentGun()
    {
        if (!isLocalPlayer || currentGun == null)
            return;
        LocalPlayer.CmdDropCurrentGun();
        if (UImanager.Instance.GetPanel<PlayerPanel>() != null)
        {
            UImanager.Instance.ShowPanel<PlayerPanel>().HideGunBackGround();//关闭UI显示
        }
    }

    [Command]
    private void CmdDropCurrentGun()
    {
        if (!isServer || currentGun == null) return;
        ServerHandleDropGun(currentGun.gameObject);
        currentGun = null;
    }

    private void ServerHandleDropGun(GameObject gunObj)
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
        if (!NetworkClient.spawned.TryGetValue(gunNetId, out NetworkIdentity gunNetIdentity)) return;

        GameObject gunObj = gunNetIdentity.gameObject;
        // 直接设置位置，无插值（消除延迟）
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
}