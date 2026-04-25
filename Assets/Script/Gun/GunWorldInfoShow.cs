using DG.Tweening;
using Mirror;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using System.Collections;

public class GunWorldInfoShow : NetworkBehaviour
{
    [Header("=== 核心开关（SyncVar同步所有客户端） ===")]
    [SyncVar(hook = nameof(OnIsGunActiveChanged))]
    private bool _isGunActive = true;

    [SyncVar(hook = nameof(OnCurrentPlayerChanged))]
    public Player CurrentPlayer;

    [Header("UI显示相关")]
    public CanvasGroup GunCanvas;
    public TextMeshProUGUI GunName;
    public TextMeshProUGUI BulletInfo;
    private Sequence CurrentSequence;
    private bool isUiShowing = false;

    [Header("当前关联的枪械")]
    public BaseGun CurrentGun;

    [Header("调试/配置")]
    public bool isDebug = true;
    public float uiFadeDuration = 0.2f;
    public float coroutineInterval = 0f;

    [Header("防抖配置")]
    public float triggerCD = 0.1f;

    private List<Player> CurrentTouchList;
    public bool HasPickUpPlayer = false;
    private Player _localTouchPlayer;

    private bool isTriggering = false;
    private float triggerTimer;

    private Collider2D _gunCollider;
    private Transform _selfTransform;
    private Transform _gunTransform;
    private int _lastGunScaleSign = 1;
    private int _lastSelfScaleSign = 1;

    private Coroutine _mainLogicCoroutine;
    private const string PLAYER_TAG = "Player";
    private WaitForSeconds _waitForSeconds;

    #region 生命周期 & 初始化
    private void Awake()
    {
        _selfTransform = transform;
        _gunCollider = GetComponent<Collider2D>() ?? GetComponentInParent<Collider2D>();
        CurrentTouchList = new List<Player>();

        if (coroutineInterval > 0)
            _waitForSeconds = new WaitForSeconds(coroutineInterval);

        if (CurrentGun == null)
        {
            CurrentGun = GetComponentInParent<BaseGun>();
            if (CurrentGun != null)
                _gunTransform = CurrentGun.transform;
        }
        else
        {
            _gunTransform = CurrentGun.transform;
        }

        if (GunCanvas == null)
            GunCanvas = GetComponentInChildren<CanvasGroup>();

        if (GunCanvas != null)
        {
            GunCanvas.alpha = 0;
            GunCanvas.blocksRaycasts = false;
        }

        if (_gunTransform != null)
            _lastGunScaleSign = (int)Mathf.Sign(_gunTransform.localScale.x);
        _lastSelfScaleSign = (int)Mathf.Sign(_selfTransform.localScale.x);
    }

    private void Start()
    {
        if (_isGunActive && CurrentGun != null)
            UpdateInfo();

        if (_isGunActive)
            StartMainLogicCoroutine();
    }

    private void OnDestroy()
    {
        CurrentSequence?.Kill();
        DOTween.Kill(this);
        StopMainLogicCoroutine();
    }
    #endregion

    #region 协程核心逻辑
    private void StartMainLogicCoroutine()
    {
        if (_mainLogicCoroutine == null)
            _mainLogicCoroutine = StartCoroutine(MainLogicCoroutine());
    }

    private void StopMainLogicCoroutine()
    {
        if (_mainLogicCoroutine != null)
        {
            StopCoroutine(_mainLogicCoroutine);
            _mainLogicCoroutine = null;
        }
    }

    private IEnumerator MainLogicCoroutine()
    {
        while (true)
        {
            if (isUiShowing && _gunTransform != null)
            {
                _lastGunScaleSign = (int)Mathf.Sign(_gunTransform.localScale.x);
                _lastSelfScaleSign = (int)Mathf.Sign(_selfTransform.localScale.x);

                if (_lastGunScaleSign != _lastSelfScaleSign)
                {
                    var uiScale = _selfTransform.localScale;
                    uiScale.x *= -1;
                    _selfTransform.localScale = uiScale;
                    _lastSelfScaleSign = -_lastSelfScaleSign;
                }
            }

            if (!_isGunActive)
            {
                yield return coroutineInterval > 0 ? _waitForSeconds : null;
                continue;
            }

            if (isTriggering)
            {
                triggerTimer += Time.deltaTime;
                if (triggerTimer >= triggerCD)
                {
                    isTriggering = false;
                    triggerTimer = 0;
                }
            }


            yield return coroutineInterval > 0 ? _waitForSeconds : null;
        }
    }
    #endregion

    #region SyncVar钩子
    private void OnIsGunActiveChanged(bool oldValue, bool newValue)
    {
        if (isDebug)
            /* Debug.Log("[客户端] " + gameObject.name + " 交互状态：" + (newValue ? "开启" : "关闭")); */

        if (!newValue)
        {
            HideGunUI();
            if (_gunCollider != null)
                _gunCollider.enabled = false;
            _localTouchPlayer = null;

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

            StopMainLogicCoroutine();
        }
        else
        {
            if (_gunCollider != null)
                _gunCollider.enabled = true;
            HasPickUpPlayer = false;
            StartMainLogicCoroutine();
        }
    }

    private void OnCurrentPlayerChanged(Player OldValue, Player NewValue)
    {
        if (!_isGunActive) return;

        if (OldValue != null && isServer)
        {
            OldValue.CurrentTouchGun = null;
        }

        if (isClient)
        {
            if (NewValue != null && NewValue.isLocalPlayer)
            {
                NewValue.CurrentTouchGun = CurrentGun;
            }
            else if (OldValue != null && OldValue.isLocalPlayer)
            {
                OldValue.CurrentTouchGun = null;
            }
        }

        HasPickUpPlayer = NewValue != null;
    }
    #endregion

    #region 碰撞检测
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!_isGunActive || isTriggering || !collision.CompareTag(PLAYER_TAG) || CurrentGun == null)
            return;

        Player touchPlayer = collision.GetComponent<Player>();
        if (touchPlayer == null)
            return;

        isTriggering = true;

        if (touchPlayer.isLocalPlayer)
        {
            _localTouchPlayer = touchPlayer;
            UpdateInfo();
            ShowGunUI();
        }

        if (isServer)
        {
            if (!IsInList(touchPlayer))
            {
                CurrentTouchList.Add(touchPlayer);
            }

            if (CurrentPlayer == null && touchPlayer.CurrentTouchGun == null)
            {
                CurrentPlayer = touchPlayer;
                touchPlayer.CurrentTouchGun = CurrentGun;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!_isGunActive || isTriggering || !collision.CompareTag(PLAYER_TAG) || CurrentGun == null)
            return;

        Player leavePlayer = collision.GetComponent<Player>();
        if (leavePlayer == null)
            return;

        isTriggering = true;

        if (leavePlayer.isLocalPlayer)
        {
            _localTouchPlayer = null;
            HideGunUI();
        }

        if (isServer)
        {
            if (IsInList(leavePlayer))
            {
                CurrentTouchList.Remove(leavePlayer);
            }

            if (leavePlayer == CurrentPlayer)
            {
                leavePlayer.CurrentTouchGun = null;
                CurrentPlayer = null;
                TransferPickUpRight();
            }
        }
    }
    #endregion

    #region 核心方法
    [Server]
    private void TransferPickUpRight()
    {
        if (!_isGunActive) return;

        CleanNullPlayersInList();

        if (CurrentTouchList.Count == 0)
        {
            HasPickUpPlayer = false;
            return;
        }

        foreach (var player in CurrentTouchList)
        {
            if (player != null && player.CurrentTouchGun == null)
            {
                CurrentPlayer = player;
                player.CurrentTouchGun = CurrentGun;
                HasPickUpPlayer = true;
                return;
            }
        }

        HasPickUpPlayer = false;
    }

    public bool IsInList(Player player)
    {
        if (CurrentTouchList == null || player == null)
            return false;

        for (int i = CurrentTouchList.Count - 1; i >= 0; i--)
        {
            if (CurrentTouchList[i] == null)
            {
                CurrentTouchList.RemoveAt(i);
                continue;
            }
            if (CurrentTouchList[i] == player)
                return true;
        }
        return false;
    }

    [Server]
    private void CleanNullPlayersInList()
    {
        if (CurrentTouchList == null) return;

        for (int i = CurrentTouchList.Count - 1; i >= 0; i--)
        {
            if (CurrentTouchList[i] == null)
                CurrentTouchList.RemoveAt(i);
        }
    }

    private void ShowGunUI()
    {
        if (!_isGunActive || GunCanvas == null || isUiShowing)
            return;

        isUiShowing = true;
        if (SimpleAnimatorTool.Instance != null)
        {
            SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(GunCanvas, ref CurrentSequence, true, () =>
            {
                GunCanvas.blocksRaycasts = true;
            });
        }
        else
        {
            CurrentSequence?.Kill();
            CurrentSequence = DOTween.Sequence();
            CurrentSequence.Append(GunCanvas.DOFade(1, uiFadeDuration))
                          .OnComplete(() => GunCanvas.blocksRaycasts = true);
        }
    }

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
            CurrentSequence?.Kill();
            CurrentSequence = DOTween.Sequence();
            CurrentSequence.Append(GunCanvas.DOFade(0, uiFadeDuration))
                          .OnComplete(() => GunCanvas.blocksRaycasts = false);
        }
    }

    public void UpdateInfo()
    {
        if (!_isGunActive || CurrentGun == null || GunName == null || BulletInfo == null)
            return;

        GunName.text = CurrentGun.gunInfo != null ? CurrentGun.gunInfo.name : "未知枪械";
        BulletInfo.text = CurrentGun.CurrentMagazineBulletCount.ToString("F0") + "/" + CurrentGun.AllReserveBulletCount.ToString("F0");
    }
    #endregion

    #region 服务器辅助方法
    [Server]
    public void ServerOnGunPicked()
    {
        _isGunActive = false;
        if (CurrentPlayer != null)
        {
            CurrentPlayer.CurrentTouchGun = null;
            CurrentPlayer = null;
        }
        CurrentTouchList.Clear();
        HasPickUpPlayer = false;
    }

    [Server]
    public void ServerOnGunDropped()
    {
        _isGunActive = true;
        HasPickUpPlayer = false;
    }
    #endregion
}