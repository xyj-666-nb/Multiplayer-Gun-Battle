using System;
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

    private Action onAccepted;

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
            OpenUrl(privacyPolicyUrl);
        }
        else if (controlName == "UserAgreementButton")
        {
            OpenUrl(userAgreementUrl);
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
    }

    private void ApplyDefaultText()
    {
        if (titleText != null)
        {
            titleText.text = "隐私政策与用户协议";
        }

        if (contentText != null)
        {
            contentText.text =
                "欢迎游玩《球球战争》。在继续游戏前，请你阅读并同意《用户协议》和《隐私政策》。\n\n" +
                "我们会按照隐私政策处理为提供游戏服务所必需的信息，包括账号登录、联机匹配、防沉迷合规、崩溃与基础运行数据等。未经同意，我们不会主动进入 TapTap 登录或联网相关流程。\n\n" +
                "如果你不同意相关条款，可以点击“不同意并退出”。如果你点击“同意并继续”，表示你已阅读并同意上述协议。";
        }
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

    private void OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            Debug.LogWarning("Privacy consent URL is empty. Set it on PrivacyConsentPanel prefab before release.");
            return;
        }

        Application.OpenURL(url);
    }

    protected override void SpecialAnimator_Hide()
    {
    }

    protected override void SpecialAnimator_Show()
    {
    }
}
