using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class PrivacyConsentPanelPrefabBuilder
{
    private const string PrefabPath = "Assets/Resources/UI/PrivacyConsentPanel.prefab";
    private const string AutoCreateSessionKey = "PrivacyConsentPanelPrefabBuilder.AutoCreate";

    [InitializeOnLoadMethod]
    private static void AutoCreateIfMissing()
    {
        if (SessionState.GetBool(AutoCreateSessionKey, false))
        {
            return;
        }

        SessionState.SetBool(AutoCreateSessionKey, true);

        if (File.Exists(PrefabPath))
        {
            return;
        }

        CreatePrefab();
    }

    [MenuItem("Tools/UI/Rebuild Privacy Consent Panel Prefab")]
    public static void CreatePrefab()
    {
        Directory.CreateDirectory("Assets/Resources/UI");

        GameObject root = new GameObject("PrivacyConsentPanel", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(PrivacyConsentPanel));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image rootImage = root.GetComponent<Image>();
        rootImage.color = new Color(0f, 0f, 0f, 0.72f);

        PrivacyConsentPanel panel = root.GetComponent<PrivacyConsentPanel>();
        panel.PriorityIndex = 999;
        panel.IsCanDestroy = true;
        panel.alphaSpeed = 12f;

        GameObject dialog = CreateRect("Dialog", root.transform);
        RectTransform dialogRect = dialog.GetComponent<RectTransform>();
        dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
        dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
        dialogRect.pivot = new Vector2(0.5f, 0.5f);
        dialogRect.sizeDelta = new Vector2(760f, 620f);
        dialogRect.anchoredPosition = Vector2.zero;

        Image dialogImage = dialog.AddComponent<Image>();
        dialogImage.color = new Color(0.08f, 0.09f, 0.11f, 0.98f);

        TextMeshProUGUI title = CreateText(dialog.transform, "Title", "隐私政策与用户协议", 34, FontStyles.Bold, TextAlignmentOptions.Center);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -18f);
        titleRect.sizeDelta = new Vector2(-64f, 54f);

        ScrollRect scrollRect = CreateScroll(dialog.transform);
        TextMeshProUGUI policyText = CreateText(scrollRect.content, "PolicyText", GetPolicyText(), 24, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        RectTransform textRect = policyText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        policyText.margin = new Vector4(18f, 18f, 18f, 18f);
        policyText.enableWordWrapping = true;
        policyText.overflowMode = TextOverflowModes.Overflow;

        CreateButton(dialog.transform, "PrivacyPolicyButton", "查看《隐私政策》", new Vector2(280f, 44f), new Vector2(-170f, 82f), new Color(0.13f, 0.31f, 0.52f, 1f), 21f);
        CreateButton(dialog.transform, "UserAgreementButton", "查看《用户协议》", new Vector2(280f, 44f), new Vector2(170f, 82f), new Color(0.13f, 0.31f, 0.52f, 1f), 21f);
        CreateButton(dialog.transform, "RejectButton", "不同意并退出", new Vector2(250f, 58f), new Vector2(-160f, 34f), new Color(0.24f, 0.25f, 0.29f, 1f), 24f);
        CreateButton(dialog.transform, "AcceptButton", "同意并继续", new Vector2(250f, 58f), new Vector2(160f, 34f), new Color(0.19f, 0.58f, 0.34f, 1f), 24f);

        SerializedObject serializedPanel = new SerializedObject(panel);
        serializedPanel.FindProperty("titleText").objectReferenceValue = title;
        serializedPanel.FindProperty("contentText").objectReferenceValue = policyText;
        serializedPanel.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static ScrollRect CreateScroll(Transform parent)
    {
        GameObject viewport = CreateRect("PolicyViewport", parent);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = new Vector2(0.08f, 0.28f);
        viewportRect.anchorMax = new Vector2(0.92f, 0.82f);
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(0.03f, 0.035f, 0.045f, 0.72f);
        viewport.AddComponent<RectMask2D>();

        GameObject content = CreateRect("PolicyContent", viewport.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(-28f, 520f);

        ScrollRect scrollRect = viewport.AddComponent<ScrollRect>();
        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 36f;
        return scrollRect;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 size, Vector2 anchoredPosition, Color color, float fontSize)
    {
        GameObject buttonObj = CreateRect(name, parent);
        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        Image image = buttonObj.AddComponent<Image>();
        image.color = color;

        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = image;

        TextMeshProUGUI text = CreateText(buttonObj.transform, "Label", label, fontSize, FontStyles.Bold, TextAlignmentOptions.Center);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        return button;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string content, float fontSize, FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject textObj = CreateRect(name, parent);
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static GameObject CreateRect(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static string GetPolicyText()
    {
        return "欢迎游玩《球球战争》。在继续游戏前，请你阅读并同意《用户协议》和《隐私政策》。\n\n" +
               "我们会按照隐私政策处理为提供游戏服务所必需的信息，包括账号登录、联机匹配、防沉迷合规、崩溃与基础运行数据等。未经同意，我们不会主动进入 TapTap 登录或联网相关流程。\n\n" +
               "如果你不同意相关条款，可以点击“不同意并退出”。如果你点击“同意并继续”，表示你已阅读并同意上述协议。";
    }
}
