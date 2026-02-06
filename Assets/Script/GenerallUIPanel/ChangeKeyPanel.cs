using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;
/// <summary>
/// 改键面板，这里的按钮按键包取名有讲究，名字要取成和PlayInput文档里面的一样
/// </summary>
public class ChangeKeyPanel : BasePanel
{
    public List<ChangeKeyPack> KeyPackList;

    public override void Awake()
    {
        base.Awake();
        foreach (var keyPack in KeyPackList)
            keyPack.PackInit();
    }
    public override void Start()
    {
        base.Start();
    }

    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        if (controlName == "ExitButton")
            UImanager.Instance.HidePanel<ChangeKeyPanel>();
        else if (controlName == "SetDefaultButton")
        {
            InputInfoManager.Instance.inputInfo.ReturnDefaultKeyInfo();//重置信息
            foreach (var keyPack in KeyPackList)
                keyPack.UpdateInfo();
        }
    }

    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
    {
        base.HideMe(callback, isNeedDefaultAnimator);
        InputInfoManager.Instance.SaveInfo();//关闭面板保存信息
        // 隐藏面板时，停止所有改键等待动画，避免残留
        foreach (var keyPack in KeyPackList)
        {
            keyPack.StopChangeKeyAnimation();
        }
    }


    #region 必须完成的特殊动画函数
    protected override void SpecialAnimator_Hide()
    {

    }

    protected override void SpecialAnimator_Show()
    {

    }

    #endregion
}

[System.Serializable]
public class ChangeKeyPack
{
    public GameObject Pack;
    private TextMeshProUGUI Name;
    private TextMeshProUGUI KeyName;
    private TextMeshProUGUI ButtonText;

    private Button keyButton;
    private Sequence ButtonSequence;//按钮动画序列
    private Image keyButtonImage; // 按钮图像组件
    private Color defaultButtonColor; // 按钮默认颜色
    private float defaultTextAlpha; // 文本默认透明度

    public ChangeKeyPack()
    {

    }

    #region 按键包初始化
    public void PackInit()
    {
        Name = Pack.transform.Find("Name").GetComponent<TextMeshProUGUI>();
        KeyName = Pack.transform.Find("KeyName").GetComponent<TextMeshProUGUI>();
        keyButton = Pack.GetComponentInChildren<Button>();
        ButtonText = keyButton.GetComponentInChildren<TextMeshProUGUI>();//获取按钮的文本组件
        // 获取按钮图像组件
        keyButtonImage = keyButton.GetComponent<Image>();

        // 保存默认状态
        if (keyButtonImage != null)
        {
            defaultButtonColor = keyButtonImage.color;
        }
        if (ButtonText != null)
        {
            defaultTextAlpha = ButtonText.alpha;
        }

        keyButton.onClick.AddListener(() => {
            StartChangeKeyAnimation();
            ButtonText.text = "Changing";
            InputSystem.onAnyButtonPress.CallOnce(ChangeKeyReally);
        });

        UpdateInfo();
    }

    #endregion

    #region 按键包数据更新
    public void UpdateInfo()
    {
        string keyPath = InputInfoManager.Instance.inputInfo.FindkeyInfo(Name.text);
        if (string.IsNullOrEmpty(keyPath))
        {
            KeyName.text = "Unknown";
            return;
        }

        string[] StrList = keyPath.Split('/');
        if (StrList.Length < 2)
        {
            KeyName.text = keyPath;
            return;
        }
        KeyName.text = StrList[1];
    }

    public void ChangeInfo(string newKeyPath)
    {
        InputInfoManager.Instance.inputInfo.ChangeKeyInfo(Name.text, newKeyPath);
        UpdateInfo();//更新显示
    }

    public void ChangeKeyReally(InputControl Control)
    {
        ButtonText.text = "Change";
        string[] str = Control.path.Split('/');
        string Path = "<" + str[1] + ">/" + str[2];

        if (InputInfoManager.Instance.inputInfo.CheckRepeat_Key(Path))
            RepeatKeyPrompt(); //重复按键提示
        else
        {
            ChangeInfo(Path); // 传入生成的新路径
            StopChangeKeyAnimation();
        }

    }
    #endregion

    #region 按键包的提示动画
    /// <summary>
    /// 启动改键等待动画：按钮变绿 + 文本透明度循环渐变闪烁
    /// </summary>
    private void StartChangeKeyAnimation()
    {
        StopChangeKeyAnimation();

        if (keyButtonImage != null)
        {
            keyButtonImage.DOColor(ColorManager.ChangeKeyWaiting, 0.2f); // 平滑过渡到绿色，耗时0.2秒
        }

        if (ButtonText != null)
        {
            ButtonSequence = DOTween.Sequence();
            ButtonSequence.Append(ButtonText.DOFade(0f, 0.5f))
                          .Append(ButtonText.DOFade(defaultTextAlpha, 0.5f))
                          .SetLoops(-1, LoopType.Yoyo)
                          .SetEase(Ease.Linear); 
        }
    }

    /// <summary>
    /// 停止改键等待动画，恢复按钮和文本的默认状态
    /// </summary>
    public void StopChangeKeyAnimation()
    {
        if (ButtonSequence != null && ButtonSequence.IsPlaying())
        {
            ButtonSequence.Kill();
            ButtonSequence = null;
        }

        if (keyButtonImage != null)
        {
            keyButtonImage.DOColor(defaultButtonColor, 0.2f);
        }

        if (ButtonText != null)
        {
            ButtonText.DOFade(defaultTextAlpha, 0.2f); 
        }
    }

    public void RepeatKeyPrompt()//重复按键提示动画
    {
        if (ButtonSequence != null && ButtonSequence.IsPlaying())
        {
            ButtonSequence.Kill();
        }
        ButtonSequence = DOTween.Sequence();

        Sequence fastSubSeq = DOTween.Sequence();
        fastSubSeq.Append(ButtonText.DOFade(defaultTextAlpha, 0.1f)) // 淡入时间和震动一致
                  .Append(keyButtonImage.DOColor(ColorManager.BrickRed, 0.1f))
                  .Append(keyButton.GetComponent<RectTransform>().DOShakeAnchorPos(
            0.3f, // 总时长
            new Vector2(2, 2), // 震动幅度
            20, // 震动次数
            90f, // 随机度
            true // 淡入淡出
        ));

        // 添加子序列 + 暂停0.5秒 + 恢复颜色
        ButtonSequence.Append(fastSubSeq)
                      .AppendInterval(0.5f) // 震动完成后立即暂停
                      .Append(keyButtonImage.DOColor(defaultButtonColor, 0.2f));

        ButtonText.text = "Repeat";
        ButtonSequence.OnComplete(() => { ButtonText.text = "Change"; });
    }
    #endregion
}