using DG.Tweening;
using Mirror;
using System.Collections;
using UnityEngine;

public class playerHandControl : NetworkBehaviour
{
    #region 基础配置与引用
    [Header("手臂旋转核心配置")]
    public float RotateSpeed = 500f;
    public float VerticalAngleLimit = 30f; // 手臂最大旋转角度限制
    public float DefaultRotationZ = 0f;

    [Header("换弹归位配置")]
    public float ReloadResetRotateDuration = 0.2f;

    [Header("瞄准状态配置")]
    public float AimStateTransform_X;
    public float EnterAimDuration = 0.5f;
    public AnimationCurve aimEaseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("收枪与拿出枪配置")]
    public Vector2 HolsterPosition;
    public float HolsterDuration = 0.3f;
    public float HolsterRotationZ = -100f;

    [Header("投掷物瞄准点配置")]
    [SerializeField] private GameObject AimPointPrefab;
    [SerializeField] private float AimPointDistance = 0.3f;
    [SerializeField] private int AimPointCount = 5;
    [SerializeField] private float LaunchForceBase = 10f;

    [Header("后坐力配置（开枪向上抬枪）")]
    public float RecoilRecoverySpeed = 8f;   // 后坐力回弹阻力
    public float RecoilPower = 1.2f;         // 后坐力强度
    public float MaxRecoilAngle = 25f;        // 最大向上抬枪角度

    [Header("依赖引用")]
    public Camera mainCamera;
    public Player ownerPlayer;
    public Transform TacticRootTransform;

    private Transform _selfTransform;
    private PlayerAndGameInfoManger _playerGameInfoManager;
    private MilitaryManager _militaryManager;
    private UImanager _uiManager;
    private PlayerTacticControl _playerTacticControl;

    private BaseGun _currentGun;
    private bool _isReloading = false;
    private int _currentFacingDir = 1;
    private Vector3 _originLocalPos;
    private bool _isDebug = true;
    public bool _isHolsterAnimaPlaying = false;

    // 后坐力核心变量
    private float _recoilOffsetZ;
    #endregion

    #region 网络同步状态（SyncVar）
    [SyncVar(hook = nameof(OnRotationValueSynced))]
    private float _currentRotationValue_Z = 0f;

    [SyncVar(hook = nameof(OnIsEnterAimChanged))]
    private bool _isEnterAim = false;
    public bool IsEnterAim => _isEnterAim;

    [SyncVar(hook = nameof(OnIsHolsterGun))]
    public bool IsHolsterGun = false;

    [SyncVar(hook = nameof(OnChangeInjection))]
    public GameObject CurrentInjection;

    [SyncVar(hook = nameof(OnChangeThrowObj))]
    public GameObject CurrentThrowObj;
    #endregion

    #region 清理投掷物和针剂
    [Server]
    public void ClearAllHandObj()
    {
        if (CurrentInjection != null)
            NetworkServer.Destroy(CurrentInjection);
        if (CurrentThrowObj != null)
        {
            NetworkServer.Destroy(CurrentThrowObj);
        }
    }
    #endregion

    #region 战术道具冷却系统
    public float tactic_1CoolTime;
    public float tactic_2CoolTime;
    public bool IsTrigger_tactic1 = false;
    public bool IsTrigger_tactic2 = false;
    public float tactic_1CoolTime_precent = 0;
    public float tactic_2CoolTime_precent = 0;

    private int tactic1taskID = 0;
    private int tactic2taskID = 0;

    public void SetTactic_1Trigger()
    {
        if (IsTrigger_tactic1 == true)
            return;

        SimpleAnimatorTool.Instance.StopFloatLerpById(tactic1taskID);
        var AllTime = _playerGameInfoManager != null
            ? _playerGameInfoManager.GetCurrentTacticInfo(1).CoolTime
            : PlayerAndGameInfoManger.Instance.GetCurrentTacticInfo(1).CoolTime;
        tactic_1CoolTime = AllTime;
        IsTrigger_tactic1 = true;
        tactic1taskID = SimpleAnimatorTool.Instance.StartFloatLerp(tactic_1CoolTime, 0, tactic_1CoolTime, (v) => { tactic_1CoolTime = v; tactic_1CoolTime_precent = tactic_1CoolTime / AllTime; }, () => { IsTrigger_tactic1 = false; });
    }

    public void SetTactic_2Trigger()
    {
        if (IsTrigger_tactic2 == true)
            return;

        SimpleAnimatorTool.Instance.StopFloatLerpById(tactic2taskID);
        IsTrigger_tactic2 = true;

        var AllTime = _playerGameInfoManager != null
            ? _playerGameInfoManager.GetCurrentTacticInfo(2).CoolTime
            : PlayerAndGameInfoManger.Instance.GetCurrentTacticInfo(2).CoolTime;
        tactic_2CoolTime = AllTime;

        tactic2taskID = SimpleAnimatorTool.Instance.StartFloatLerp(tactic_2CoolTime, 0, tactic_2CoolTime, (v) => { tactic_2CoolTime = v; tactic_2CoolTime_precent = tactic_2CoolTime / AllTime; }, () => { IsTrigger_tactic2 = false; });
    }
    #endregion

    #region 收枪与拿枪逻辑
    [Command(requiresAuthority = true)]
    public void SetHolsterState(bool wantHolster)
    {
        if (!isServer)
            return;
        if (ownerPlayer == null)
            return;
        if (ownerPlayer.currentGun != null && ownerPlayer.currentGun.IsInReload)
            return;

        IsHolsterGun = wantHolster;
    }

    private void HolsterGun()
    {
        if (!isClient) return;

        if (isOwned)
        {
            if (HolsterPosition == Vector2.zero && _isDebug)
                Debug.LogError("[收枪] HolsterPosition 未配置！", this);
            if (HolsterDuration <= 0 && _isDebug) Debug.LogError("[收枪] HolsterDuration 不能为0！", this);
        }

        if (isOwned)
            _isHolsterAnimaPlaying = true;
        _selfTransform.localRotation = Quaternion.Euler(0, 0, DefaultRotationZ);
        if (isOwned)
            SetRotationZ(DefaultRotationZ);

        _selfTransform.DOKill(true);
        _selfTransform.DOLocalMove(new Vector3(HolsterPosition.x, HolsterPosition.y, _selfTransform.localPosition.z), HolsterDuration).SetEase(Ease.InCubic).SetUpdate(true);
        _selfTransform.DOLocalRotate(new Vector3(0, 0, HolsterRotationZ), HolsterDuration).SetEase(Ease.InCubic).SetUpdate(true).OnComplete(() => {
            if (isOwned)
                _isHolsterAnimaPlaying = false;
        });
    }

    private void UnholsterGun()
    {
        if (!isClient)
            return;
        if (isOwned) _isHolsterAnimaPlaying = true;

        _selfTransform.DOKill(true);
        _selfTransform.DOLocalMove(_originLocalPos, HolsterDuration).SetEase(Ease.OutCubic).SetUpdate(true);
        _selfTransform.DOLocalRotate(new Vector3(0, 0, DefaultRotationZ), HolsterDuration).SetEase(Ease.OutCubic).SetUpdate(true).OnComplete(() => {
            if (isOwned) _isHolsterAnimaPlaying = false;
        });
    }
    #endregion

    #region SyncVar钩子回调
    private void OnChangeThrowObj(GameObject OldObj, GameObject NewObj)
    {
        if (OldObj != null && OldObj != NewObj)
        {
            OldObj.transform.SetParent(null);
            var oldThrowScript = OldObj.GetComponent<ThrowObj>();
            if (oldThrowScript?.ThrowObjTimeLine != null)
                oldThrowScript.ThrowObjTimeLine.Stop();
            SetAimPointActive(false);
            ownerPlayer.mySortingLayerControl.RemoveSpriteRendererFromManager(OldObj.GetComponent<SpriteRenderer>());
        }

        if (NewObj != null && NewObj != OldObj)
        {
            NewObj.transform.DOKill(true);
            NewObj.transform.SetParent(TacticRootTransform, false);
            NewObj.transform.localPosition = Vector3.zero;
            NewObj.transform.SetAsLastSibling();
            SetHolsterState(true);
            var throwScript = NewObj.GetComponent<ThrowObj>();
            if (throwScript != null)
            {
                throwScript.IsTackOut = true;
                ownerPlayer.mySortingLayerControl.AddSpriteRendererInManager(NewObj.GetComponent<SpriteRenderer>());
                if (isLocalPlayer)
                    (_uiManager ?? UImanager.Instance).GetPanel<PlayerPanel>()?.shootButton.ChangeIcon((_militaryManager ?? MilitaryManager.Instance).GetTacticUISprite(throwScript.tacticType));
            }
        }
        if (NewObj == null)
        {
            SetHolsterState(false);
            SetAimPointActive(false);
            if (isLocalPlayer)
                (_uiManager ?? UImanager.Instance).GetPanel<PlayerPanel>()?.shootButton.ResetIcon();
        }
    }

    private void OnChangeInjection(GameObject OldInjection, GameObject NewInjection)
    {
        if (!isClient)
            return;
        if (OldInjection != null && OldInjection != NewInjection)
        {
            OldInjection.transform.SetParent(null);
            var oldInjectionScript = OldInjection.GetComponent<Injection>();
            if (oldInjectionScript?.TimeLine_Inject != null)
            {
                oldInjectionScript.TimeLine_Inject.Stop();
                ownerPlayer.mySortingLayerControl.RemoveSpriteRendererFromManager(OldInjection.GetComponentInChildren<SpriteRenderer>());
            }
        }

        if (NewInjection != null && NewInjection != OldInjection)
        {
            NewInjection.transform.DOKill(true);
            NewInjection.transform.SetParent(TacticRootTransform, false);
            NewInjection.transform.localPosition = new Vector3(0f, 0f, 0f);
            NewInjection.transform.SetAsLastSibling();
            var injectionScript = NewInjection.GetComponent<Injection>();
            if (injectionScript != null)
            {
                injectionScript._playerHand = this;
                ownerPlayer.mySortingLayerControl.AddSpriteRendererInManager(NewInjection.GetComponentInChildren<SpriteRenderer>());
            }
        }
    }

    private void OnRotationValueSynced(float oldFloat, float newFloat)
    {
        _currentRotationValue_Z = newFloat;
        if (!isOwned)
            _selfTransform.localRotation = Quaternion.Euler(0, 0, newFloat);
    }

    private void OnIsEnterAimChanged(bool oldValue, bool newValue)
    {
        if (!isClient)
            return;
        if (newValue)
            EnterAimState();
        else
            ExitAimState();
    }

    private void OnIsHolsterGun(bool oldValue, bool newValue)
    {
        if (!isClient)
            return;
        if (newValue)
            HolsterGun();
        else
            UnholsterGun();
    }
    #endregion

    #region 生命周期
    private void Awake()
    {
        _selfTransform = transform;
        _playerGameInfoManager = PlayerAndGameInfoManger.Instance;
        _militaryManager = MilitaryManager.Instance;
        _uiManager = UImanager.Instance;
        _playerTacticControl = PlayerTacticControl.Instance;

        mainCamera = MyCameraControl.Instance?.GetComponentInChildren<Camera>();
        _originLocalPos = _selfTransform.localPosition;

        ownerPlayer = GetComponentInParent<Player>();
        if (ownerPlayer == null)
            ownerPlayer = GetComponent<Player>();
        if (ownerPlayer != null)
            _currentFacingDir = ownerPlayer.FacingDir;

        if (TacticRootTransform == null) TacticRootTransform = _selfTransform.parent;
    }

    private void Update()
    {
        if (!isClient || !isOwned) return;

        UpdateCurrentGunState();

        if (IsHolsterGun || _isHolsterAnimaPlaying)
        {
            if (IsStartAim) HandleTouchAimAngle();
            UpdateThrowAimPoints();
            return;
        }

        if (!_isReloading) HandleTouchRotation_Bidirectional();
        else ResetRotationOnReload();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        _currentRotationValue_Z = DefaultRotationZ;
        IsHolsterGun = false;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        _selfTransform.localRotation = Quaternion.Euler(0, 0, _currentRotationValue_Z);

        if (isOwned && CurrentInjection != null && TacticRootTransform != null)
        {
            CurrentInjection.transform.SetParent(TacticRootTransform, false);
            CurrentInjection.transform.localPosition = Vector3.zero;
            CurrentInjection.transform.localRotation = Quaternion.identity;
            CurrentInjection.transform.localScale = Vector3.one;
        }

        if (isClient)
        {
            if (IsHolsterGun) HolsterGun();
            else UnholsterGun();
        }

        if (isOwned) InitAimPoints();
    }

    private void OnDestroy()
    {
        ClearAimPoints();
    }
    #endregion

    #region 手臂旋转核心逻辑（增量压枪版）
    // 记录当前的实际瞄准角度（包含所有后坐力）
    private float _currentAimAngle;

    private void HandleTouchRotation_Bidirectional()
    {
        if (mainCamera == null || !mainCamera.orthographic)
        {
            if (_isDebug) Debug.LogError("[错误] 主相机未赋值/非2D正交相机！", this);
            return;
        }

        TouchInputHandler touchHandler = TouchInputHandler.Instance;
        if(touchHandler == null)
        {
            if (_isDebug)
                Debug.LogWarning("[错误] 场景中缺少 TouchInputHandler！", this);
            return;
        }

        if (touchHandler != null && touchHandler.IsTouchActive)
        {
            // 获取这一帧手指滑动了多少像素
            Vector2 touchDelta = touchHandler.TouchDelta;

            // 简单的区域判定：如果在角色左侧，可能不响应或者翻转Y轴，根据需求来
            // 这里假设只要触摸有效就响应

            // 将像素移动量转化为角度变化量
            // 注意：这里我们主要用 Y 轴 (上下滑动) 来控制旋转
            float angleChange = touchDelta.y * touchHandler.TouchSensitivity;

            // 如果是朝左，可能需要反转输入，看你需求
            if (_currentFacingDir == -1)
            {
                angleChange = -angleChange; // 或者根据情况调整
            }

            // 玩家往下滑 (touchDelta.y 为负) -> angleChange 为负 -> 角度减小 -> 枪口下压
            _currentAimAngle += angleChange;
        }

        // ===================== 2. 限制角度范围 =====================
        // 不管是玩家操作还是后坐力，最终角度不能太离谱
        _currentAimAngle = Mathf.Clamp(_currentAimAngle, -VerticalAngleLimit, VerticalAngleLimit);

        // ===================== 3. 后坐力逻辑 =====================
        // 后坐力恢复
        _recoilOffsetZ = Mathf.Lerp(_recoilOffsetZ, 0, Time.deltaTime * RecoilRecoverySpeed);

        // ===================== 4. 最终旋转 =====================
        // 注意：这里不再是 aimAngle + recoil，因为 _currentAimAngle 已经是混合体了
        // 为了让后坐力更清晰，我们保留 _recoilOffsetZ 并直接加在显示上
        // 但为了手感，我们也可以直接把后坐力加到 _currentAimAngle 里，然后让玩家的输入去对抗它

        // 方案 A (推荐)：后坐力也是物理角度的一部分，玩家的输入直接对抗这个物理角度
        // 开枪时，我们直接把后坐力加到 _currentAimAngle 上

        float finalAngle = _currentAimAngle + _recoilOffsetZ;

        // 稍微放宽最终显示限制，允许后坐力暂时超出一点
        finalAngle = Mathf.Clamp(finalAngle, -VerticalAngleLimit - 20, VerticalAngleLimit + 20);

        _selfTransform.localRotation = Quaternion.Euler(0, 0, finalAngle);
        SetRotationZ(_selfTransform.localEulerAngles.z);
    }

    // 修改开枪逻辑
    public void AddGunMomentOfForce()
    {
        if (ownerPlayer == null || ownerPlayer.currentGun == null)
            return;

        var gunInfo = ownerPlayer.currentGun.gunInfo;
        float recoilPower = (100 - gunInfo.ControlPower) * 0.1f * RecoilPower;
        recoilPower = Mathf.Min(recoilPower, MaxRecoilAngle);

        // 【关键修改】
        // 方案A：直接把后坐力加到我们的目标角度上
        // 这样枪口就会真的上跳，玩家必须向下滑动手指来把 _currentAimAngle 减回去
        _currentAimAngle += recoilPower;

        // 如果你还想保留一点视觉上的额外抖动，可以保留 _recoilOffsetZ，
        // 但主要的压枪手感应该来自上面这一行。
        // _recoilOffsetZ += recoilPower * 0.5f; 
    }
    #endregion

    #region 旋转同步与换弹重置
    private void UpdateCurrentGunState()
    {
        if (ownerPlayer == null) { _isReloading = false; return; }
        _currentGun = ownerPlayer.currentGun;
        if (_currentGun == null) { _isReloading = false; return; }
        _isReloading = _currentGun.IsInReload;
    }

    [Client]
    public void ResetRotationOnReload()
    {
        if (!isOwned) return;
        if (IsHolsterGun || _isHolsterAnimaPlaying) return;

        _recoilOffsetZ = 0;
        _selfTransform.DOKill();
        _selfTransform.DOLocalRotate(new Vector3(0, 0, DefaultRotationZ), ReloadResetRotateDuration)
                 .SetEase(Ease.OutCubic)
                 .OnComplete(() => SetRotationZ(DefaultRotationZ));
    }

    public void SetRotationZ(float targetZ)
    {
        if (IsHolsterGun || _isHolsterAnimaPlaying) return;
        if (isServer)
            _currentRotationValue_Z = targetZ;
        else if (isClient && isOwned)
            CmdSetRotationZ(targetZ);
    }

    [Command(requiresAuthority = true)]
    private void CmdSetRotationZ(float targetZ)
    {
        _currentRotationValue_Z = targetZ;
    }
    #endregion

    #region 瞄准状态与动画
    public void SetAimState(bool wantAim)
    {
        if (IsHolsterGun) 
            return;
        if (isServer)
            _isEnterAim = wantAim;
        else if 
            (isClient && isOwned) 
            CmdSetAimState(wantAim);
    }

    [Command(requiresAuthority = true)]
    private void CmdSetAimState(bool wantAim)
    {
        _isEnterAim = wantAim;
    }

    public void EnterAimState()
    {
        if (IsHolsterGun || !isClient) return;
        _selfTransform.DOKill();
        _selfTransform.DOLocalMoveX(AimStateTransform_X, EnterAimDuration).SetEase(aimEaseCurve).SetUpdate(false);
    }

    public void ExitAimState()
    {
        if (!isClient) return;
        _selfTransform.DOKill();
        _selfTransform.DOLocalMoveX(_originLocalPos.x, EnterAimDuration).SetEase(aimEaseCurve).SetUpdate(false);
    }
    #endregion

    #region 战术设备管理
    public void TriggerInjection(TacticType Type)
    {
        SetHolsterState(true);
        CmdCreateInjection(Type);
    }

    [Command(requiresAuthority = true)]
    public void CmdCreateInjection(TacticType Type)
    {
        var spawnedObj = CreateTactic(Type);
        var injectionScript = spawnedObj.GetComponent<Injection>();
        if (injectionScript != null)
        {
            injectionScript.BindToPlayer(connectionToClient.identity);
            injectionScript.CmdTriggerInjection();
        }
        else
        {
            Debug.LogError("[CmdCreateInjection] 生成的注射器对象缺少 Injection 脚本", this);
            NetworkServer.Destroy(spawnedObj);
            CurrentInjection = null;
            return;
        }
        CurrentInjection = spawnedObj;
    }

    [Command(requiresAuthority = true)]
    public void CmdCreateThrowObj(TacticType Type)
    {
        Debug.Log($"[CmdCreateThrowObj] 服务器请求生成战术设备 → 类型：{Type}", this);
        var militaryManager = _militaryManager ?? MilitaryManager.Instance;
        if (militaryManager == null)
        { Debug.LogError("[CmdCreateThrowObj] MilitaryManager.Instance 为 null！", this); return; }
        var throwObjPrefab = militaryManager.GetTactic(Type);
        if (throwObjPrefab == null)
        { Debug.LogError($"[CmdCreateThrowObj] GetTactic({Type}) 返回 null！", this); return; }
        if (throwObjPrefab.GetComponent<ThrowObj>() == null)
        { Debug.LogError($"[CmdCreateThrowObj] 预制体缺少 ThrowObj 脚本！", this); return; }

        GameObject spawnedThrowObj = Instantiate(throwObjPrefab);
        NetworkServer.Spawn(spawnedThrowObj, connectionToClient);
        CurrentThrowObj = spawnedThrowObj;

        CurrentThrowObj.GetComponent<ThrowObj>().HandControl = this;
    }

    public void TriggerThrowObj(TacticType Type)
    {
        CmdCreateThrowObj(Type);
        SetAimPointActive(true);
    }

    [Command(requiresAuthority = true)]
    public void CmdRecycleThrowObj()
    {
        if (CurrentThrowObj == null) return;
        var throwScript = CurrentThrowObj.GetComponent<ThrowObj>();
        if (throwScript != null)
        {
            throwScript.HandControl = this;
            throwScript.IsTackOut = false;
        }
        SetAimPointActive(false);
    }

    public GameObject CreateTactic(TacticType Type)
    {
        if (!isServer) { Debug.LogError("[CreateTactic] 非服务器环境", this); return null; }

        var militaryManager = _militaryManager ?? MilitaryManager.Instance;
        if (militaryManager == null) { Debug.LogError("[CreateTactic] MilitaryManager 为 null", this); return null; }
        var Obj = militaryManager.GetTactic(Type);
        if (Obj == null) { Debug.LogError($"[CreateTactic] GetTactic({Type}) 返回 null", this); return null; }

        GameObject spawnedObj = Instantiate(Obj);
        NetworkServer.Spawn(spawnedObj, connectionToClient);
        return spawnedObj;
    }
    #endregion

    #region 投掷物瞄准点系统
    private GameObject[] _aimPoints;
    private Vector2[] _aimPointVelocities;

    public Vector2 LaunchForce;
    public float CurrentAimAngle;
    public bool IsStartAim = false;

    [Header("瞄准点设置")]
    public float AimPointTimeInterval = 0.1f;
    public float SmoothTime = 0.05f;

    private void InitAimPoints()
    {
        _aimPoints = new GameObject[AimPointCount];
        _aimPointVelocities = new Vector2[AimPointCount];

        for (int i = 0; i < AimPointCount; i++)
        {
            _aimPoints[i] = Instantiate(AimPointPrefab);
            _aimPoints[i].SetActive(false);
            _aimPoints[i].transform.SetParent(TacticRootTransform);
            SpriteRenderer sr = _aimPoints[i].GetComponent<SpriteRenderer>();
            if (sr == null)
                sr = _aimPoints[i].AddComponent<SpriteRenderer>();
        }
    }

    public void SetAimPointActive(bool isActive)
    {
        if (!isOwned || _aimPoints == null)
        {
            IsStartAim = isActive;
            return;
        }
        StartCoroutine(UpdateAimPointActive(isActive));
    }

    private IEnumerator UpdateAimPointActive(bool isActive)
    {
        if (_aimPoints == null) yield break;
        IsStartAim = isActive;

        for (int i = 0; i < _aimPoints.Length; i++)
        {
            int index = i;
            if (_aimPoints[index] == null) continue;
            SpriteRenderer sr = _aimPoints[index].GetComponent<SpriteRenderer>();
            if (sr == null) continue;

            if (isActive)
            {
                _aimPoints[index].SetActive(true);
                sr.DOFade(1, 0.1f);
            }
            else
            {
                sr.DOFade(0, 0.1f).OnComplete(() => _aimPoints[index].SetActive(false));
            }
            yield return new WaitForSeconds(0.05f);
        }
    }

    public void CalculateLaunchForce(float angle)
    {
        CurrentAimAngle = angle;
        float radAngle = angle * Mathf.Deg2Rad;
        float baseX = Mathf.Cos(radAngle) * LaunchForceBase;
        float baseY = Mathf.Sin(radAngle) * LaunchForceBase;
        baseX *= ownerPlayer?.FacingDir ?? 1;
        LaunchForce = new Vector2(baseX, baseY);
    }

    private Vector2 CalculateAimPointPosition(float t)
    {
        Vector2 forceDisplacement = LaunchForce * t;
        Vector2 pos = (Vector2)TacticRootTransform.transform.position + forceDisplacement + 0.5f * Physics2D.gravity * t * t;
        return pos;
    }

    public void UpdateThrowAimPoints()
    {
        if (!IsStartAim || _aimPoints == null || ownerPlayer == null) return;

        for (int i = 0; i < _aimPoints.Length; i++)
        {
            if (_aimPoints[i] == null) continue;
            Vector2 targetPos = CalculateAimPointPosition(i * AimPointTimeInterval);
            _aimPoints[i].transform.position = Vector2.SmoothDamp(_aimPoints[i].transform.position, targetPos, ref _aimPointVelocities[i], SmoothTime);
        }
    }

    private void ClearAimPoints()
    {
        if (_aimPoints == null) return;
        foreach (var point in _aimPoints)
        {
            if (point != null) Destroy(point);
        }
    }

    private void HandleTouchAimAngle()
    {
        if (mainCamera == null || !mainCamera.orthographic || TacticRootTransform == null) return;

        TouchInputHandler touchHandler = TouchInputHandler.Instance;
        if (touchHandler == null || !touchHandler.IsTouchActive) return;

        Vector3 targetWorldPos = touchHandler.CurrentTouchWorldPos;
        Vector2 dirToTarget = new Vector2(targetWorldPos.x - TacticRootTransform.position.x, targetWorldPos.y - TacticRootTransform.position.y);
        if (dirToTarget.sqrMagnitude < 0.001f) return;

        float targetAngle = Mathf.Atan2(dirToTarget.y, dirToTarget.x) * Mathf.Rad2Deg;
        CalculateLaunchForce(targetAngle);
    }
    #endregion

    #region 发射逻辑
    public void LaunchCurrentThrowObj()
    {
        if (!isOwned || CurrentThrowObj == null || CurrentThrowObj.GetComponent<ThrowObj>().IsInAnimation) return;

        CmdLaunchThrowObj(LaunchForce);
        if (isLocalPlayer)
            (_playerTacticControl ?? PlayerTacticControl.Instance).SetIsChooseButton(false);
    }

    [Command(requiresAuthority = true)]
    private void CmdLaunchThrowObj(Vector2 serverLaunchForce)
    {
        if (CurrentThrowObj == null) return;
        ThrowObj throwScript = CurrentThrowObj.GetComponent<ThrowObj>();
        if (throwScript == null)
        {
            Debug.LogError("[发射] 缺少 ThrowObj 脚本！", CurrentThrowObj);
            return;
        }

        CurrentThrowObj.transform.SetParent(null);
        throwScript.ServerLaunch(serverLaunchForce);
        CurrentThrowObj = null;
        SetHolsterState(false);
    }
    #endregion

    #region 对外暴露属性
    public float CurrentRotationValue_Z => _currentRotationValue_Z;
    public bool IsReloading => _isReloading;
    #endregion
}