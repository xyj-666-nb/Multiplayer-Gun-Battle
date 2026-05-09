using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PrivacyConsentPanel : BasePanel
{
    [Header("Policy Links")]
    [SerializeField] private string privacyPolicyUrl = "";
    [SerializeField] private string userAgreementUrl = "";

    [Header("Policy Text")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI contentText;
    [SerializeField] private ScrollRect policyScrollRect;

    private Action onAccepted;

    private const string SummaryTitle = "隐私政策与用户协议";
    private const string SummaryText =
        "欢迎游玩《球球战争》。在继续游戏前，请你阅读并同意《用户协议》和《隐私政策》。\n\n" +
        "我们会按照隐私政策处理为提供游戏服务所必需的信息，包括账号登录、联机匹配、防沉迷合规、崩溃与基础运行数据等。未经同意，我们不会主动进入 TapTap 登录、广告或联网相关流程。\n\n" +
        "你可以点击下方按钮查看完整条款。如果你不同意相关条款，可以点击“不同意并退出”。如果你点击“同意并继续”，表示你已阅读并同意上述协议。";

    private const string PrivacyPolicyText =
        "《球球战争》隐私政策\n\n" +
        "一、信息收集与使用\n" +
        "为了提供游戏登录、联机对战、广告展示、基础安全和运行维护能力，我们可能会在必要范围内处理设备信息、网络状态、游戏账号标识、登录状态、崩溃日志、基础运行日志以及联机对战过程中的必要同步数据。\n\n" +
        "二、第三方 SDK\n" +
        "本应用使用 TapTap SDK 提供第三方登录及广告相关能力，使用 Unity / 团结引擎 UOS 服务提供游戏联机中继或网络连接能力。相关 SDK 可能会根据其服务需要处理必要的信息。\n\n" +
        "三、数据存储与保护\n" +
        "我们会采取合理措施保护你的信息安全。主要游戏配置和进度数据保存在本地；联机过程中仅传输实现对战连接、状态同步和服务稳定所必需的数据。\n\n" +
        "四、权限使用\n" +
        "应用可能会使用网络访问权限，用于账号登录、联机对战、广告服务和基础运行服务。我们不会在未经同意的情况下主动进入登录、广告或联网相关流程。\n\n" +
        "五、未成年人保护\n" +
        "我们会根据法律法规和接入平台要求配合实名认证、防沉迷及未成年人保护相关能力。\n\n" +
        "六、联系我们\n" +
        "如你对隐私政策或个人信息处理有疑问，可通过应用备案或发布页面展示的联系方式与开发者联系。";

    private const string UserAgreementText =
        "《球球战争》用户协议\n\n" +
        "一、服务说明\n" +
        "《球球战争》是一款休闲竞技类游戏，提供本地游玩、在线联机对战、第三方登录和广告等相关功能。\n\n" +
        "二、账号与使用规则\n" +
        "你应当遵守法律法规和平台规则，不得利用游戏进行作弊、攻击、干扰服务器、破坏公平体验或侵犯他人权益的行为。\n\n" +
        "三、联机服务\n" +
        "游戏联机功能依赖网络环境和相关云服务。因网络波动、设备状态、服务维护或第三方服务异常导致的连接失败、延迟或中断，开发者会尽力修复和优化，但无法保证服务在所有情况下持续可用。\n\n" +
        "四、内容与知识产权\n" +
        "游戏内的程序、界面、角色、道具、美术、音效和相关内容归开发者或合法权利人所有。未经许可，不得复制、传播、拆解、修改或用于商业用途。\n\n" +
        "五、协议更新\n" +
        "我们可能会根据产品功能、法律法规或平台要求更新本协议。更新后会在应用内或相关页面提示，继续使用游戏表示你接受更新后的内容。\n\n" +
        "六、退出与不同意\n" +
        "如果你不同意本协议或隐私政策，可以点击“不同意并退出”停止使用本应用。";

    public override void Awake()
    {
        base.Awake();
        CacheTextRefs();
        ApplyDefaultText();
    }

    public void Init(Action acceptedCallback)
    {
        onAccepted = acceptedCallback;
    }

    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);

        if (controlName == "AcceptButton")
        {
            AcceptAndContinue();
        }
        else if (controlName == "RejectButton")
        {
            RejectAndQuit();
        }
        else if (controlName == "PrivacyPolicyButton")
        {
            ShowDocument("隐私政策", PrivacyPolicyText, privacyPolicyUrl);
        }
        else if (controlName == "UserAgreementButton")
        {
            ShowDocument("用户协议", UserAgreementText, userAgreementUrl);
        }
    }

    private void CacheTextRefs()
    {
        if (titleText == null)
        {
            Transform title = transform.Find("Dialog/Title");
            titleText = title != null ? title.GetComponent<TextMeshProUGUI>() : null;
        }

        if (contentText == null)
        {
            Transform content = transform.Find("Dialog/PolicyViewport/PolicyContent/PolicyText");
            contentText = content != null ? content.GetComponent<TextMeshProUGUI>() : null;
        }

        if (policyScrollRect == null)
        {
            Transform scroll = transform.Find("Dialog/PolicyViewport");
            policyScrollRect = scroll != null ? scroll.GetComponent<ScrollRect>() : null;
        }
    }

    private void ApplyDefaultText()
    {
        SetDocument(SummaryTitle, SummaryText);
    }

    private void AcceptAndContinue()
    {
        PrivacyConsentGate.MarkAccepted();
        Action callback = onAccepted;
        onAccepted = null;
        UImanager.Instance.HidePanel<PrivacyConsentPanel>(false);
        callback?.Invoke();
    }

    private void RejectAndQuit()
    {
#if UNITY_EDITOR
        Debug.Log("Privacy consent rejected. Quit is ignored in the Unity Editor.");
#else
        Application.Quit();
#endif
    }

    private void ShowDocument(string title, string fallbackText, string url)
    {
        if (!string.IsNullOrWhiteSpace(url))
        {
            Application.OpenURL(url);
        }

        SetDocument(title, fallbackText);
    }

    private void SetDocument(string title, string text)
    {
        if (titleText != null)
        {
            titleText.text = title;
        }

        if (contentText != null)
        {
            contentText.text = text;
        }

        if (policyScrollRect != null)
        {
            StartCoroutine(RefreshScrollContentNextFrame());
        }
    }

    private IEnumerator RefreshScrollContentNextFrame()
    {
        yield return null;
        RefreshScrollContent();
    }

    private void RefreshScrollContent()
    {
        if (policyScrollRect == null || contentText == null || policyScrollRect.content == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        contentText.ForceMeshUpdate();

        RectTransform contentRect = policyScrollRect.content;
        RectTransform viewportRect = policyScrollRect.viewport != null
            ? policyScrollRect.viewport
            : policyScrollRect.GetComponent<RectTransform>();

        float viewportHeight = viewportRect != null ? viewportRect.rect.height : 0f;
        float preferredHeight = contentText.preferredHeight + contentText.margin.y + contentText.margin.w;
        float targetHeight = Mathf.Max(viewportHeight + 1f, preferredHeight);

        contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
        contentRect.anchoredPosition = Vector2.zero;
        policyScrollRect.velocity = Vector2.zero;
        policyScrollRect.verticalNormalizedPosition = 1f;
        Canvas.ForceUpdateCanvases();
    }

    protected override void SpecialAnimator_Hide()
    {
    }

    protected override void SpecialAnimator_Show()
    {
    }
}
