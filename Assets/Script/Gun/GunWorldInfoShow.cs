using DG.Tweening;
using Mirror;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using System.Collections; // 协程必需命名空间

public class GunWorldInfoShow : NetworkBehaviour
{
    [Header("=== 核心开关（SyncVar同步所有客户端） ===")]
    [SyncVar(hook = nameof(OnIsGunActiveChanged))]
    private bool _isGunActive = true; // 枪械是否处于可交互状态

    [SyncVar(hook = nameof(OnCurrentPlayerChanged))]
    public Player CurrentPlayer; // 拥有拾取权的玩家

    [Header("UI显示相关")]
    public CanvasGroup GunCanvas;
    public TextMeshProUGUI GunName;
    public TextMeshProUGUI BulletInfo;
    private Sequence CurrentSequence; // UI渐变动画序列
    private bool isUiShowing = false; // UI显示状态，避免重复触发

    [Header("当前关联的枪械")]
    public BaseGun CurrentGun;

    [Header("调试/配置")]
    public bool isDebug = true;
    public float uiFadeDuration = 0.2f; // UI渐变时长
    [Tooltip("协程执行频率（秒/次），0=每帧执行，>0=固定时间步")]
    public float coroutineInterval = 0f; // 建议0（每帧），如需降频可设0.016f（60帧）

    [Header("防抖配置")]
    public float triggerCD = 0.1f; // 碰撞防抖CD

    // 核心缓存（新增/优化）
    private List<Player> CurrentTouchList;
    public bool HasPickUpPlayer = false; // 是否有玩家拥有拾取权
    private Player _localTouchPlayer; // 本地触碰的玩家

    // 防抖相关（优化）
    private bool isTriggering = false;
    private float triggerTimer;

    // 碰撞体缓存（优化）
    private Collider2D _gunCollider;
    // 高频访问缓存（新增）
    private Transform _selfTransform; // 自身Transform
    private Transform _gunTransform; // 枪械Transform
    private int _lastGunScaleSign = 1; // 缓存枪械缩放符号，减少重复计算
    private int _lastSelfScaleSign = 1; // 缓存自身缩放符号

    // 协程管理（核心新增）
    private Coroutine _mainLogicCoroutine; // 主逻辑协程
    private const string PLAYER_TAG = "Player"; // 缓存Tag字符串，避免重复创建

    #region 生命周期 & 初始化（优化）
    private void Awake()
    {
        // 1. 缓存高频访问的Transform
        _selfTransform = transform;
        // 2. 初始化碰撞体（优化：提前判空，减少层级查找）
        _gunCollider = GetComponent<Collider2D>() ?? GetComponentInParent<Collider2D>();
        // 3. 初始化触碰列表
        CurrentTouchList = new List<Player>();

        // 4. 缓存枪械组件（优化：减少重复GetComponent）
        if (CurrentGun == null)
        {
            CurrentGun = GetComponentInParent<BaseGun>();
            if (CurrentGun == null && isDebug)
                Debug.LogError($"[{gameObject.name}] Awake：未找到父物体的BaseGun组件！");
            else
                _gunTransform = CurrentGun.transform; // 缓存枪械Transform
        }

        // 5. 缓存UI组件（优化：减少重复GetComponent）
        if (GunCanvas == null)
        {
            GunCanvas = GetComponentInChildren<CanvasGroup>();
            if (GunCanvas == null && isDebug)
                Debug.LogError($"[{gameObject.name}] Awake：未找到CanvasGroup组件！");
        }

        // 6. 初始化UI状态（优化：仅在CanvasGroup有效时执行）
        if (GunCanvas != null)
        {
            GunCanvas.alpha = 0;
            GunCanvas.blocksRaycasts = false;
        }

        // 7. 初始化缩放符号缓存
        if (_gunTransform != null)
            _lastGunScaleSign = Mathf.Sign(_gunTransform.localScale.x) == 1 ? 1 : -1;
        _lastSelfScaleSign = Mathf.Sign(_selfTransform.localScale.x) == 1 ? 1 : -1;
    }

    private void Start()
    {
        // 初始更新一次信息（仅当枪械激活时）
        if (_isGunActive && CurrentGun != null)
            UpdateInfo();

        // 启动主逻辑协程（仅当枪械激活时）
        if (_isGunActive)
            StartMainLogicCoroutine();
    }

    private void OnDestroy()
    {
        // 1. 清理动画序列，防止内存泄漏
        CurrentSequence?.Kill();
        DOTween.Kill(this);
        // 2. 停止协程，防止内存泄漏
        StopMainLogicCoroutine();
    }
    #endregion

    #region 协程核心逻辑（替换Update）
    /// <summary>
    /// 启动主逻辑协程（封装，避免重复启动）
    /// </summary>
    private void StartMainLogicCoroutine()
    {
        if (_mainLogicCoroutine == null)
            _mainLogicCoroutine = StartCoroutine(MainLogicCoroutine());
    }

    /// <summary>
    /// 停止主逻辑协程（封装，避免空引用）
    /// </summary>
    private void StopMainLogicCoroutine()
    {
        if (_mainLogicCoroutine != null)
        {
            StopCoroutine(_mainLogicCoroutine);
            _mainLogicCoroutine = null;
        }
    }

    /// <summary>
    /// 主逻辑协程（替代Update）
    /// </summary>
    private IEnumerator MainLogicCoroutine()
    {
        // 无限循环，直到协程被停止
        while (true)
        {
            // ========== 逻辑1：UI显示时同步缩放符号 ==========
            if (isUiShowing && _gunTransform != null)
            {
                // 优化：仅当缩放符号不一致时才执行修改
                _lastGunScaleSign = Mathf.Sign(_gunTransform.localScale.x) == 1 ? 1 : -1;
                _lastSelfScaleSign = Mathf.Sign(_selfTransform.localScale.x) == 1 ? 1 : -1;

                if (_lastGunScaleSign != _lastSelfScaleSign)
                {
                    var uiScale = _selfTransform.localScale;
                    uiScale.x *= -1;
                    _selfTransform.localScale = uiScale;
                    // 更新缓存的自身缩放符号
                    _lastSelfScaleSign = -_lastSelfScaleSign;
                }
            }

            // ========== 逻辑2：枪械未激活时，跳过后续逻辑 ==========
            if (!_isGunActive)
            {
                // 协程间隔（避免卡死）
                if (coroutineInterval > 0)
                    yield return new WaitForSeconds(coroutineInterval);
                else
                    yield return null;
                continue;
            }

            // ========== 逻辑3：碰撞防抖CD计时 ==========
            if (isTriggering)
            {
                triggerTimer += Time.deltaTime;
                if (triggerTimer >= triggerCD)
                {
                    isTriggering = false;
                    triggerTimer = 0;
                }
            }

            // ========== 协程执行间隔 ==========
            if (coroutineInterval > 0)
                yield return new WaitForSeconds(coroutineInterval);
            else
                yield return null; // 每帧执行（等同于Update）
        }
    }
    #endregion

    #region SyncVar钩子（优化）
    /// <summary>
    /// 枪械交互状态变更钩子（优化：协程启停管理）
    /// </summary>
    private void OnIsGunActiveChanged(bool oldValue, bool newValue)
    {
        if (isDebug)
            Debug.Log($"[客户端] {gameObject.name} 交互状态：{(newValue ? "开启" : "关闭")}");

        if (!newValue)
        {
            // 隐藏所有客户端的UI
            HideGunUI();
            // 禁用碰撞检测
            _gunCollider.enabled = false;
            // 清空本地触碰玩家
            _localTouchPlayer = null;

            // 服务器端清理数据
            if (isServer)
            {
                CurrentTouchList.Clear();
                if (CurrentPlayer != null)
                {
                    CurrentPlayer.CurrentTouchGun = null;
                    CurrentPlayer = null;
                }
                HasPickUpPlayer = false;
            }

            // 停止协程（核心优化）
            StopMainLogicCoroutine();
        }
        else
        {
            // 激活碰撞检测
            _gunCollider.enabled = true;
            HasPickUpPlayer = false;

            // 启动协程（核心优化）
            StartMainLogicCoroutine();
        }
    }

    /// <summary>
    /// 拾取权玩家变更钩子（仅同步拾取权状态，不影响UI显示）
    /// </summary>
    private void OnCurrentPlayerChanged(Player OldValue, Player NewValue)
    {
        // 功能关闭时，不执行任何拾取权逻辑
        if (!_isGunActive) return;

        // 清理旧玩家的拾取权
        if (OldValue != null && isServer)
        {
            OldValue.CurrentTouchGun = null;
            if (isDebug)
                Debug.Log($"[SyncVar] {gameObject.name} 拾取权从 {OldValue.name} 移除");
        }

        if (isClient)
        {
            // 如果新的拾取权玩家是本地玩家，就把当前枪械赋值给本地玩家的 CurrentTouchGun
            if (NewValue != null && NewValue.isLocalPlayer)
            {
                NewValue.CurrentTouchGun = CurrentGun;
                if (isDebug)
                    Debug.Log($"[客户端] 本地玩家 {NewValue.name} 的 CurrentTouchGun 已设置为 {CurrentGun.name}");
            }
            // 如果旧的拾取权玩家是本地玩家，就清空本地玩家的 CurrentTouchGun
            else if (OldValue != null && OldValue.isLocalPlayer)
            {
                OldValue.CurrentTouchGun = null;
                if (isDebug)
                    Debug.Log($"[客户端] 本地玩家 {OldValue.name} 的 CurrentTouchGun 已清空");
            }
        }

        // 标记是否有玩家拥有拾取权
        HasPickUpPlayer = NewValue != null;

        // 调试日志
        if (NewValue != null && isDebug)
        {
            if (NewValue.isLocalPlayer)
                Debug.Log($"[客户端] 你获得了 {gameObject.name} 的拾取权");
            else if (isServer)
                Debug.Log($"[服务器] {NewValue.name} 获得 {gameObject.name} 拾取权");
        }
    }
    #endregion

    #region 碰撞检测（优化：减少重复计算）
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 功能关闭/防抖/非玩家/无枪械 直接返回（优化：缓存Tag，减少字符串比较）
        if (!_isGunActive || isTriggering || !collision.CompareTag(PLAYER_TAG) || CurrentGun == null)
            return;

        Player touchPlayer = collision.GetComponent<Player>();
        if (touchPlayer == null)
            return;

        // 标记防抖（防止短时间重复触发）
        isTriggering = true;

        // ========== 客户端逻辑：本地玩家触碰就显示UI ==========
        if (touchPlayer.isLocalPlayer)
        {
            _localTouchPlayer = touchPlayer;
            UpdateInfo(); // 先更新信息再显示
            ShowGunUI();
            if (isDebug)
                Debug.Log($"[客户端] 本地玩家 {touchPlayer.name} 触碰枪械，显示UI");
        }

        // ========== 服务器逻辑：管理触碰列表 + 分配拾取权 ==========
        if (isServer)
        {
            // 避免重复添加玩家到触碰列表（优化：IsInList已内置空元素清理）
            if (!IsInList(touchPlayer))
            {
                CurrentTouchList.Add(touchPlayer);
                if (isDebug)
                    Debug.Log($"[服务器] {touchPlayer.name} 加入 {gameObject.name} 触碰列表，当前列表数：{CurrentTouchList.Count}");
            }

            // 只有当前无拾取权玩家时，才分配给第一个触碰的玩家
            if (CurrentPlayer == null && touchPlayer.CurrentTouchGun == null)
            {
                CurrentPlayer = touchPlayer; // SyncVar自动同步所有客户端
                touchPlayer.CurrentTouchGun = CurrentGun; // 服务器赋值玩家的拾取权
                if (isDebug)
                    Debug.Log($"[服务器] {touchPlayer.name} 获得 {gameObject.name} 拾取权（第一个触碰）");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // 功能关闭/防抖/非玩家/无枪械 直接返回（优化：缓存Tag，减少字符串比较）
        if (!_isGunActive || isTriggering || !collision.CompareTag(PLAYER_TAG) || CurrentGun == null)
            return;

        Player leavePlayer = collision.GetComponent<Player>();
        if (leavePlayer == null)
            return;

        // 标记防抖
        isTriggering = true;

        // ========== 客户端逻辑：本地玩家离开就隐藏UI ==========
        if (leavePlayer.isLocalPlayer)
        {
            _localTouchPlayer = null;
            HideGunUI();
            if (isDebug)
                Debug.Log($"[客户端] 本地玩家 {leavePlayer.name} 离开枪械，隐藏UI");
        }

        // ========== 服务器逻辑：管理触碰列表 + 转移拾取权 ==========
        if (isServer)
        {
            // 从触碰列表移除离开的玩家
            if (IsInList(leavePlayer))
            {
                CurrentTouchList.Remove(leavePlayer);
                if (isDebug)
                    Debug.Log($"[服务器] {leavePlayer.name} 离开 {gameObject.name} 触碰列表，当前列表数：{CurrentTouchList.Count}");
            }

            // 如果离开的是拥有拾取权的玩家 → 转移拾取权
            if (leavePlayer == CurrentPlayer)
            {
                // 清空当前玩家的拾取权
                leavePlayer.CurrentTouchGun = null;
                CurrentPlayer = null;

                // 权限转移：从剩余触碰列表中找下一个无枪械的玩家
                TransferPickUpRight();
            }
        }
    }
    #endregion

    #region 核心方法：权限转移/列表检查/UI控制（优化）
    /// <summary>
    /// 【服务器专用】拾取权转移：从触碰列表中找第一个可拾取的玩家
    /// </summary>
    [Server]
    private void TransferPickUpRight()
    {
        // 功能关闭时，不执行转移逻辑
        if (!_isGunActive) return;

        // 清理列表中的空玩家（防止空引用）
        CleanNullPlayersInList();

        // 触碰列表为空 → 无玩家可转移
        if (CurrentTouchList.Count == 0)
        {
            if (isDebug)
                Debug.Log($"[服务器] {gameObject.name} 触碰列表为空，拾取权空置");
            HasPickUpPlayer = false;
            return;
        }

        // 遍历列表，找第一个无拾取权的玩家
        foreach (var player in CurrentTouchList)
        {
            // 玩家有效 + 无当前拾取权 → 分配拾取权
            if (player != null && player.CurrentTouchGun == null)
            {
                CurrentPlayer = player;
                player.CurrentTouchGun = CurrentGun;
                HasPickUpPlayer = true;
                if (isDebug)
                    Debug.Log($"[服务器] {gameObject.name} 拾取权转移给 {player.name}");
                return;
            }
        }

        // 列表中所有玩家都有拾取权 → 权限空置
        if (isDebug)
            Debug.Log($"[服务器] {gameObject.name} 触碰列表中所有玩家都有拾取权，权限空置");
        HasPickUpPlayer = false;
    }

    /// <summary>
    /// 检查玩家是否在触碰列表中（内置空元素清理）
    /// </summary>
    public bool IsInList(Player player)
    {
        if (CurrentTouchList == null || player == null)
            return false;

        // 倒序遍历，清理空元素 + 检查是否存在
        for (int i = CurrentTouchList.Count - 1; i >= 0; i--)
        {
            if (CurrentTouchList[i] == null)
            {
                CurrentTouchList.RemoveAt(i); // 清理空玩家
                continue;
            }
            if (CurrentTouchList[i] == player)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 【服务器专用】清理触碰列表中的空玩家
    /// </summary>
    [Server]
    private void CleanNullPlayersInList()
    {
        if (CurrentTouchList == null) return;

        for (int i = CurrentTouchList.Count - 1; i >= 0; i--)
        {
            if (CurrentTouchList[i] == null)
            {
                CurrentTouchList.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 显示枪械UI（优化：减少重复判断）
    /// </summary>
    private void ShowGunUI()
    {
        if (!_isGunActive || GunCanvas == null || isUiShowing)
            return;

        isUiShowing = true;
        // 兼容SimpleAnimatorTool，加判空防止崩溃
        if (SimpleAnimatorTool.Instance != null)
        {
            SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(GunCanvas, ref CurrentSequence, true, () =>
            {
                GunCanvas.blocksRaycasts = true; // 显示时允许射线交互
            });
        }
        else
        {
            // 降级方案：直接用DOTween（优化：复用Sequence）
            CurrentSequence?.Kill();
            CurrentSequence = DOTween.Sequence();
            CurrentSequence.Append(GunCanvas.DOFade(1, uiFadeDuration))
                          .OnComplete(() => GunCanvas.blocksRaycasts = true);
        }
    }

    /// <summary>
    /// 隐藏枪械UI（优化：减少重复判断）
    /// </summary>
    private void HideGunUI()
    {
        if (GunCanvas == null || !isUiShowing)
            return;

        isUiShowing = false;
        if (SimpleAnimatorTool.Instance != null)
        {
            SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(GunCanvas, ref CurrentSequence, false, () =>
            {
                GunCanvas.blocksRaycasts = false;
            });
        }
        else
        {
            // 降级方案：直接用DOTween（优化：复用Sequence）
            CurrentSequence?.Kill();
            CurrentSequence = DOTween.Sequence();
            CurrentSequence.Append(GunCanvas.DOFade(0, uiFadeDuration))
                          .OnComplete(() => GunCanvas.blocksRaycasts = false);
        }
    }

    /// <summary>
    /// 更新枪械UI信息（优化：减少重复null检查）
    /// </summary>
    public void UpdateInfo()
    {
        if (!_isGunActive || CurrentGun == null || GunName == null || BulletInfo == null)
        {
            if (isDebug)
                Debug.LogWarning($"[{gameObject.name}] UpdateInfo：组件缺失或功能已关闭！");
            return;
        }

        // 优化：空合并运算符简化逻辑
        GunName.text = CurrentGun.gunInfo?.name ?? "未知枪械";
        BulletInfo.text = $"{CurrentGun.CurrentMagazineBulletCount:F0}/{CurrentGun.AllReserveBulletCount:F0}";
    }
    #endregion

    #region 服务器辅助方法
    /// <summary>
    /// 枪械被拾取后，关闭所有交互功能
    /// </summary>
    [Server]
    public void ServerOnGunPicked()
    {
        _isGunActive = false; // SyncVar同步所有客户端关闭功能
        // 清理拾取权
        if (CurrentPlayer != null)
        {
            CurrentPlayer.CurrentTouchGun = null;
            CurrentPlayer = null;
        }
        // 清理触碰列表
        CurrentTouchList.Clear();
        HasPickUpPlayer = false;
        if (isDebug)
            Debug.Log($"[服务器] {gameObject.name} 已被拾取，关闭所有交互功能");
    }

    /// <summary>
    /// 枪械被丢弃后，恢复交互功能
    /// </summary>
    [Server]
    public void ServerOnGunDropped()
    {
        _isGunActive = true; // SyncVar同步所有客户端开启功能
        HasPickUpPlayer = false;
        if (isDebug)
            Debug.Log($"[服务器] {gameObject.name} 已被丢弃，恢复交互功能");
    }
    #endregion
}