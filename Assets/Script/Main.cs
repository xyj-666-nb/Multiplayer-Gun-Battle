using UnityEngine;
using UnityEngine.InputSystem;

public class Main : SingleMonoAutoBehavior<Main>
{
    public LanguageType CurrentLanguageType;
    protected override void Awake()
    {
        base.Awake();
    
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    void Start()
    {
        Developer_GUITestManger.Instance.RegisterGuiButton("播放对话1", () => { DialogueManager.Instance.StartPlayDialogue(1, DialoguePlayType.AutoPlay); });
        Developer_GUITestManger.Instance.RegisterGuiButton("播放对话2", () => { DialogueManager.Instance.StartPlayDialogue(2); });
        Developer_GUITestManger.Instance.RegisterGuiButton("发送一条消息1", () => { SendMessageManger.Instance.SendMessage("这是一条测试消息持续2秒", 2f); });
        Developer_GUITestManger.Instance.RegisterGuiButton("打开警告面板", () => { WarnTriggerManager.Instance.TriggerDoubleInteractionWarn("22", "22", () => { }, () => { }); });
        Developer_GUITestManger.Instance.RegisterGuiButton("打开标题面板", () => { UImanager.Instance.ShowPanel<ShowTopicPanel>().SetDurationTime(3); });
        Developer_GUITestManger.Instance.RegisterGuiButton("摄像机震动", () => { MyCameraControl.Instance.AddTimeBasedShake(1, 1); });
        Developer_GUITestManger.Instance.RegisterGuiButton_TwoWay("打开设置面板", "关闭设置面板", () => { UImanager.Instance.ShowPanel<SettingPanel>(); },() => { UImanager.Instance.HidePanel<SettingPanel>(); } );
        Developer_GUITestManger.Instance.RegisterGuiButton_TwoWay("打开当前调试信息", "关闭当前调试信息", () => { Developer_GUITestManger.Instance.IsShowAllInfo(true); }, () => { Developer_GUITestManger.Instance.IsShowAllInfo(false); });
    }


    private void Update()
    {

    }
}
public enum LanguageType
{
    Chinese,
    English,
}