using DG.Tweening;
using Mirror;
using System.Collections;
using UnityEngine;

public class playerHandControl : NetworkBehaviour
{
    #region 基础配置与引用
    [Header("手臂旋转核心配置")]
    public float RotateSpeed = 500f;
    public float VerticalAngleLimit = 30f;
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

    [Header("依赖引用")]
    public Camera mainCamera;
    public Player ownerPlayer;
    public Transform TacticRootTransform;

    private BaseGun _currentGun;
    private bool _isReloading = false;
    private int _currentFacingDir = 1;
    private Vector3 _originLocalPos;
    private bool _isDebug = true;
    public bool _isHolsterAnimaPlaying = false;
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

    #region 战术道具冷却系统
    public float tactic_1CoolTime;//战术道具1剩余冷却时间
    public float tactic_2CoolTime;//战术道具2剩余冷却时间
    public bool IsTrigger_tactic1 = false;//战术道具1触发
    public bool IsTrigger_tactic2 = false;//战术道具2触发
    public float tactic_1CoolTime_precent = 0;//剩余百分比时间
    public float tactic_2CoolTime_precent = 0;//剩余百分比时间

    private int tactic1taskID = 0;
    private int tactic2taskID = 0;

    public void SetTactic_1Trigger()//战术道具1触发
    {
        if (IsTrigger_tactic1 == true)
            return;

        SimpleAnimatorTool.Instance.StopFloatLerpById(tactic1taskID);
        //赋值当前的冷却时间
        var AllTime = PlayerAndGameInfoManger.Instance.GetCurrentTacticInfo(1).CoolTime;
        tactic_1CoolTime = AllTime;
        IsTrigger_tactic1 = true;
        tactic1taskID = SimpleAnimatorTool.Instance.StartFloatLerp(tactic_1CoolTime, 0, tactic_1CoolTime, (v) => { tactic_1CoolTime = v; tactic_1CoolTime_precent = tactic_1CoolTime / AllTime; }, () => { IsTrigger_tactic1 = false; });
    }

    public void SetTactic_2Trigger()//战术道具2触发
    {
        if (IsTrigger_tactic2 == true)
            return;

        SimpleAnimatorTool.Instance.StopFloatLerpById(tactic2taskID);
        IsTrigger_tactic2 = true;
        //赋值当前的冷却时间
        var AllTime = PlayerAndGameInfoManger.Instance.GetCurrentTacticInfo(2).CoolTime;
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
            if (_isDebug) Debug.Log($"[收枪] 开始执行动画", this);
        }

        if (isOwned)
            _isHolsterAnimaPlaying = true;
        transform.localRotation = Quaternion.Euler(0, 0, DefaultRotationZ);
        if (isOwned)
            SetRotationZ(DefaultRotationZ);

        transform.DOKill(true);
        transform.DOLocalMove(new Vector3(HolsterPosition.x, HolsterPosition.y, transform.localPosition.z), HolsterDuration).SetEase(Ease.InCubic).SetUpdate(true);
        transform.DOLocalRotate(new Vector3(0, 0, HolsterRotationZ), HolsterDuration).SetEase(Ease.InCubic).SetUpdate(true).OnComplete(() => {
            if (isOwned)
            {
                _isHolsterAnimaPlaying = false;
                if (_isDebug)
                    Debug.Log("[收枪] 动画完成", this);
            }
        });
    }

    private void UnholsterGun()
    {
        if (!isClient)
            return;
        if (isOwned && _isDebug) Debug.Log($"[拿枪] 开始执行动画", this);
        if (isOwned) _isHolsterAnimaPlaying = true;

        transform.DOKill(true);
        transform.DOLocalMove(_originLocalPos, HolsterDuration).SetEase(Ease.OutCubic).SetUpdate(true);
        transform.DOLocalRotate(new Vector3(0, 0, DefaultRotationZ), HolsterDuration).SetEase(Ease.OutCubic).SetUpdate(true).OnComplete(() => {
            if (isOwned) { _isHolsterAnimaPlaying = false; if (_isDebug) Debug.Log("[拿枪完成]", this); }
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
            if (oldThrowScript?.ThrowObjTimeLine != null) oldThrowScript.ThrowObjTimeLine.Stop();
            SetAimPointActive(false);
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
                if (_isDebug) Debug.Log($"[投掷物挂载] 已挂载", this);

                if (isLocalPlayer)//只有本地才更新
                    UImanager.Instance.GetPanel<PlayerPanel>()?.shootButton.ChangeIcon(MilitaryManager.Instance.GetTacticUISprite(throwScript.tacticType));//更新UI的图标设置为投掷物的图标
            }
        }
        if (NewObj == null)
        {
            SetHolsterState(false);
            SetAimPointActive(false);
            //恢复射击按钮图标
            if (isLocalPlayer)//只有本地才更新
                UImanager.Instance.GetPanel<PlayerPanel>()?.shootButton.ResetIcon();//还原UI的图标设置为默认图标
        }
    }

    private void OnChangeInjection(GameObject OldInjection, GameObject NewInjection)
    {
        if (!isClient) return;
        if (OldInjection != null && OldInjection != NewInjection)
        {
            OldInjection.transform.SetParent(null);
            var oldInjectionScript = OldInjection.GetComponent<Injection>();
            if (oldInjectionScript?.TimeLine_Inject != null) oldInjectionScript.TimeLine_Inject.Stop();
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
                if (_isDebug) Debug.Log($"[注射器挂载] 已挂载", this);
            }
        }
    }

    private void OnRotationValueSynced(float oldFloat, float newFloat)
    {
        _currentRotationValue_Z = newFloat;
        if (!isOwned) transform.rotation = Quaternion.Euler(0, 0, newFloat);
    }

    private void OnIsEnterAimChanged(bool oldValue, bool newValue)
    {
        if (!isClient) return;
        if (newValue) EnterAimState();
        else ExitAimState();
    }

    private void OnIsHolsterGun(bool oldValue, bool newValue)
    {
        if (!isClient) return;
        if (newValue) HolsterGun();
        else UnholsterGun();
    }
    #endregion

    #region 生命周期
    private void Awake()
    {
        mainCamera = MyCameraControl.Instance?.GetComponentInChildren<Camera>();
        if (mainCamera == null && _isDebug) Debug.LogWarning("[初始化] 未找到MyCameraControl", this);
        _originLocalPos = transform.localPosition;

        ownerPlayer = GetComponentInParent<Player>();
        if (ownerPlayer == null) ownerPlayer = GetComponent<Player>();
        if (ownerPlayer != null) _currentFacingDir = ownerPlayer.FacingDir;

        if (TacticRootTransform == null) TacticRootTransform = transform.parent;
    }

    private void Update()
    {
        if (!isClient || !isOwned) return;

        UpdateCurrentGunState();

        if (IsHolsterGun || _isHolsterAnimaPlaying)
        {
            if (IsStartAim) HandleMouseAimAngle();
            UpdateThrowAimPoints();
            return;
        }

        if (!_isReloading) HandleMouseRotation_Bidirectional();
        else ResetRotationOnReload();

        SyncHandRotation();
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
        transform.rotation = Quaternion.Euler(0, 0, _currentRotationValue_Z);
        if (_isDebug) Debug.Log($"[初始化] 初始角度：{_currentRotationValue_Z}°", this);

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

    #region 手臂旋转核心逻辑
    private void HandleMouseRotation_Bidirectional()
    {
        if (mainCamera == null || !mainCamera.orthographic)
        {
            if (_isDebug) Debug.LogError("[错误] 主相机未赋值/非2D正交相机！", this);
            return;
        }

        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(
            Input.mousePosition.x, Input.mousePosition.y,
            transform.position.z - mainCamera.transform.position.z
        ));
        mouseWorldPos.z = transform.position.z;

        bool isMouseInValidArea = mouseWorldPos.x >= transform.position.x;
        if (!isMouseInValidArea) return;

        Vector2 dirToMouse = new Vector2(mouseWorldPos.x - transform.position.x, mouseWorldPos.y - transform.position.y);
        if (dirToMouse.sqrMagnitude < 0.001f) return;

        float rawAngle = Mathf.Atan2(dirToMouse.y, dirToMouse.x) * Mathf.Rad2Deg;
        rawAngle = Mathf.DeltaAngle(0, rawAngle);

        float targetAngle = rawAngle;
        float verticalOffset = Mathf.Abs(rawAngle) > 90 ? 180 - Mathf.Abs(rawAngle) : Mathf.Abs(rawAngle);

        if (verticalOffset > VerticalAngleLimit)
        {
            targetAngle = rawAngle > 0
                ? (rawAngle > 90 ? 180 - VerticalAngleLimit : VerticalAngleLimit)
                : (rawAngle < -90 ? -180 + VerticalAngleLimit : -VerticalAngleLimit);
        }

        if (_currentFacingDir == -1)
        {
            targetAngle = -targetAngle;
            if (_isDebug) Debug.Log($"[角度翻转] 朝左，原始角度：{rawAngle:F1}° → 翻转后：{targetAngle:F1}°", this);
        }

        SetRotationZ(targetAngle);
    }
    #endregion

    #region 旋转同步与换弹重置
    private void SyncHandRotation()
    {
        if (IsHolsterGun || _isHolsterAnimaPlaying || !isOwned) return;

        float currentAngle = transform.eulerAngles.z;
        float targetAngle = _currentRotationValue_Z;
        float deltaAngle = Mathf.DeltaAngle(currentAngle, targetAngle);
        if (Mathf.Abs(deltaAngle) > 0.1f)
        {
            float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, RotateSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0, 0, newAngle);
        }
    }

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

        transform.DOKill();
        transform.DORotate(new Vector3(0, 0, DefaultRotationZ), ReloadResetRotateDuration)
                 .SetEase(Ease.OutCubic)
                 .OnComplete(() => SetRotationZ(DefaultRotationZ));
    }

    public void SetRotationZ(float targetZ)
    {
        if (IsHolsterGun || _isHolsterAnimaPlaying) return;
        if (isServer) _currentRotationValue_Z = targetZ;
        else if (isClient && isOwned) CmdSetRotationZ(targetZ);
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
        if (IsHolsterGun) return;
        if (isServer) _isEnterAim = wantAim;
        else if (isClient && isOwned) CmdSetAimState(wantAim);
    }

    [Command(requiresAuthority = true)]
    private void CmdSetAimState(bool wantAim)
    {
        _isEnterAim = wantAim;
    }

    public void EnterAimState()
    {
        if (IsHolsterGun || !isClient) return;
        transform.DOKill();
        transform.DOLocalMoveX(AimStateTransform_X, EnterAimDuration).SetEase(aimEaseCurve).SetUpdate(false);
    }

    public void ExitAimState()
    {
        if (!isClient) return;
        transform.DOKill();
        transform.DOLocalMoveX(_originLocalPos.x, EnterAimDuration).SetEase(aimEaseCurve).SetUpdate(false);
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
        if (MilitaryManager.Instance == null) { Debug.LogError("[CmdCreateThrowObj] MilitaryManager.Instance 为 null！", this); return; }
        var throwObjPrefab = MilitaryManager.Instance.GetTactic(Type);
        if (throwObjPrefab == null) { Debug.LogError($"[CmdCreateThrowObj] GetTactic({Type}) 返回 null！", this); return; }
        if (throwObjPrefab.GetComponent<ThrowObj>() == null) { Debug.LogError($"[CmdCreateThrowObj] 预制体缺少 ThrowObj 脚本！", this); return; }

        GameObject spawnedThrowObj = Instantiate(throwObjPrefab);
        NetworkServer.Spawn(spawnedThrowObj, connectionToClient);
        CurrentThrowObj = spawnedThrowObj;
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
            throwScript.playerHandControl = this;
            throwScript.IsTackOut = false;
        }
        SetAimPointActive(false);
    }

    public GameObject CreateTactic(TacticType Type)
    {
        if (!isServer) { Debug.LogError("[CreateTactic] 非服务器环境", this); return null; }
        if (MilitaryManager.Instance == null) { Debug.LogError("[CreateTactic] MilitaryManager 为 null", this); return null; }
        var Obj = MilitaryManager.Instance.GetTactic(Type);
        if (Obj == null) { Debug.LogError($"[CreateTactic] GetTactic({Type}) 返回 null", this); return null; }

        GameObject spawnedObj = Instantiate(Obj);
        NetworkServer.Spawn(spawnedObj, connectionToClient);
        return spawnedObj;
    }
    #endregion

    #region 投掷物瞄准点系统
    private GameObject[] _aimPoints;
    public Vector2 LaunchForce;
    public float CurrentAimAngle;
    public bool IsStartAim = false;

    private void InitAimPoints()
    {
        _aimPoints = new GameObject[AimPointCount];
        for (int i = 0; i < AimPointCount; i++)
        {
            _aimPoints[i] = Instantiate(AimPointPrefab);
            _aimPoints[i].SetActive(false);
            _aimPoints[i].transform.SetParent(TacticRootTransform);
            SpriteRenderer sr = _aimPoints[i].GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                Debug.LogError($"瞄准点实例（索引{i}）缺少SpriteRenderer组件", this);
                sr = _aimPoints[i].AddComponent<SpriteRenderer>();
            }
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0);
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
            if (this == null || _aimPoints == null || _aimPoints[i] == null) yield break;

            SpriteRenderer sr = _aimPoints[i].GetComponent<SpriteRenderer>();
            if (sr == null) continue;

            if (isActive)
            {
                _aimPoints[i].SetActive(true);
                sr.DOFade(1, 0.1f);
            }
            else
            {
                sr.DOFade(0, 0.1f).OnComplete(() => {
                    if (_aimPoints != null && i < _aimPoints.Length && _aimPoints[i] != null)
                        _aimPoints[i].SetActive(false);
                });
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
        Vector2 pos = (Vector2)TacticRootTransform.transform.position
                    + forceDisplacement
                    + 0.5f * Physics2D.gravity * t * t;
        return pos;
    }

    private void UpdateThrowAimPoints()
    {
        if (!IsStartAim || _aimPoints == null || ownerPlayer == null) return;
        for (int i = 0; i < _aimPoints.Length; i++)
        {
            if (_aimPoints[i] == null) continue;
            _aimPoints[i].transform.position = CalculateAimPointPosition(i * AimPointDistance);
        }
    }

    private void ClearAimPoints()
    {
        if (_aimPoints == null) return;
        foreach (var point in _aimPoints)
        {
            if (point != null) Destroy(point);
        }
        _aimPoints = null;
    }

    private void HandleMouseAimAngle()
    {
        if (mainCamera == null || !mainCamera.orthographic) return;
        if (TacticRootTransform == null) return;

        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(
            Input.mousePosition.x, Input.mousePosition.y,
            TacticRootTransform.position.z - mainCamera.transform.position.z
        ));
        mouseWorldPos.z = TacticRootTransform.position.z;

        Vector2 dirToMouse = new Vector2(
            mouseWorldPos.x - TacticRootTransform.position.x,
            mouseWorldPos.y - TacticRootTransform.position.y
        );

        if (dirToMouse.sqrMagnitude < 0.001f) return;
        float targetAngle = Mathf.Atan2(dirToMouse.y, dirToMouse.x) * Mathf.Rad2Deg;

        CalculateLaunchForce(targetAngle);

        if (_isDebug) Debug.DrawLine(TacticRootTransform.position, mouseWorldPos, Color.green);
    }
    #endregion

    #region 发射逻辑
    public void LaunchCurrentThrowObj()
    {
        if (!isOwned || CurrentThrowObj == null || CurrentThrowObj.GetComponent<ThrowObj>().IsInAnimation)//处于动画中就不发射
            return;

        CmdLaunchThrowObj(LaunchForce);
        //UI进行更新
        if (isLocalPlayer)//只有本地才更新
            PlayerTacticControl.Instance?.SetIsChooseButton(false);
    }

    [Command(requiresAuthority = true)]
    private void CmdLaunchThrowObj(Vector2 serverLaunchForce)
    {
        if (CurrentThrowObj == null)
            return;

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