using DG.Tweening;
using Mirror;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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

    // 触碰列表：记录所有当前触碰枪械的玩家
    private List<Player> CurrentTouchList;
    public bool HasPickUpPlayer = false; // 是否有玩家拥有拾取权
    private Player _localTouchPlayer; // 本地触碰的玩家

    // 避免短时间重复触发
    private bool isTriggering = false;
    private float triggerCD = 0.1f;
    private float triggerTimer;

    // 碰撞体缓存
    private Collider2D _gunCollider;

    #region 生命周期 & 初始化
    private void Awake()
    {
        // 初始化碰撞体
        _gunCollider = GetComponent<Collider2D>();
        if (_gunCollider == null)
            _gunCollider = GetComponentInParent<Collider2D>();

        CurrentTouchList = new List<Player>();

        if (CurrentGun == null)
        {
            CurrentGun = GetComponentInParent<BaseGun>();
            if (CurrentGun == null && isDebug)
                Debug.LogError($"[{gameObject.name}] Awake：未找到父物体的BaseGun组件！");
        }
        if (GunCanvas == null)
        {
            GunCanvas = GetComponentInChildren<CanvasGroup>();
            if (GunCanvas == null && isDebug)
                Debug.LogError($"[{gameObject.name}] Awake：未找到CanvasGroup组件！");
        }

        // 初始化UI状态：隐藏
        if (GunCanvas != null)
        {
            GunCanvas.alpha = 0;
            GunCanvas.blocksRaycasts = false;
        }
    }

    private void Start()
    {
        // 初始更新一次信息（仅当枪械激活时）
        if (_isGunActive && CurrentGun != null)
            UpdateInfo();
    }

    private void Update()
    {
        // 功能关闭时，直接返回，不执行任何逻辑
        if (!_isGunActive)
            return;

        // 碰撞防抖CD（仅用于防止短时间重复触发碰撞事件）
        if (isTriggering)
        {
            triggerTimer += Time.deltaTime;
            if (triggerTimer >= triggerCD)
            {
                isTriggering = false;
                triggerTimer = 0;
            }
        }
    }

    private void OnDestroy()
    {
        // 清理动画序列，防止内存泄漏
        if (CurrentSequence != null && CurrentSequence.IsActive())
            CurrentSequence.Kill();
        DOTween.Kill(this);
    }
    #endregion

    #region SyncVar钩子
    /// <summary>
    /// 枪械交互状态变更钩子
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
            if (_gunCollider != null)
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
        }
        else
        {
            // 激活碰撞检测
            if (_gunCollider != null)
                _gunCollider.enabled = true;
            HasPickUpPlayer = false;
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

    #region 碰撞检测（核心修改：UI显示与拾取权解耦）
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 功能关闭/防抖/非玩家/无枪械 直接返回
        if (!_isGunActive || isTriggering || !collision.CompareTag("Player") || CurrentGun == null)
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
            // 避免重复添加玩家到触碰列表
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
        // 功能关闭/防抖/非玩家/无枪械 直接返回
        if (!_isGunActive || isTriggering || !collision.CompareTag("Player") || CurrentGun == null)
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

    #region 核心方法：权限转移/列表检查/UI控制
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
    /// 显示枪械UI
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
            // 降级方案：直接用DOTween
            CurrentSequence = DOTween.Sequence();
            CurrentSequence.Append(GunCanvas.DOFade(1, uiFadeDuration))
                          .OnComplete(() => GunCanvas.blocksRaycasts = true);
        }
    }

    /// <summary>
    /// 隐藏枪械UI
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
            CurrentSequence = DOTween.Sequence();
            CurrentSequence.Append(GunCanvas.DOFade(0, uiFadeDuration))
                          .OnComplete(() => GunCanvas.blocksRaycasts = false);
        }
    }

    /// <summary>
    /// 更新枪械UI信息
    /// </summary>
    public void UpdateInfo()
    {
        if (!_isGunActive || CurrentGun == null || GunName == null || BulletInfo == null)
        {
            if (isDebug)
                Debug.LogWarning($"[{gameObject.name}] UpdateInfo：组件缺失或功能已关闭！");
            return;
        }

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