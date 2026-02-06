using System.Collections.Generic;
using UnityEngine;

#region 对话数据包结构类
[System.Serializable]
public class DialogueInfoPack
{
    [Header("说话人信息")]
    [Tooltip("对话的发言者名称")]
    [Space(1)]
    public string speakerName = "未命名说话人";

    [Space(5)]
    [Tooltip("对话内容，TextArea限制显示行数")]
    [Space(1)]
    [TextArea(1, 5)]
    public string dialogueContent = "请输入对话内容...";

    [Space(5)]
    [Tooltip("说话人的头像Sprite（可以不填）")]
    [Space(1)]
    public Sprite speakerAvatar;

    [Space(5)]
    [Tooltip("当前句子结束等待的时间(只有在自动播放模式下有效)")]
    public float WaitTimer = 1.0f;//当前句对话播放完成后等待的时间
}

[System.Serializable]
public class DialogueDataPack
{
    [Header("对话包基础信息")]
    [Tooltip("对话包的名称")]
    [Space(1)]
    public string dialoguePackName = "新对话包";

    [Space(5)]
    [Tooltip("对话包的唯一ID（自动生成）")]
    [Space(1)]
    public int dialoguePackID;

    [Space(10)]
    [Header("对话内容列表")]
    [Tooltip("该对话包包含的所有单条对话")]
    public List<DialogueInfoPack> dialogueInfoPacks = new List<DialogueInfoPack>();

    [HideInInspector] public string displayName => string.IsNullOrEmpty(dialoguePackName) ? $"对话包ID:{dialoguePackID}" : dialoguePackName;
}
#endregion

#region 对话包播放类型
public enum DialoguePlayType//对话包播放类型
{
    Interact,//玩家点击交互
    AutoPlay,//自动播放
}
#endregion

/// <summary>
/// 对话管理类：负责管理和播放游戏中的对话包
///  在这里配置对话的数据
/// </summary>
public class DialogueManager : SingleMonoAutoBehavior<DialogueManager>
{
    #region 对话数据包以标识
    [Header("全局对话数据包列表")]
    [Tooltip("游戏中所有的对话数据包配置")]
    public List<DialogueDataPack> dialogueDataPacksList = new List<DialogueDataPack>();

    [Header("自动播放情况下单句完成后的停留时间")]
    public float AutoPlaySingleDialogueStayTime = 1.5f;//自动播放情况下单句完成后的停留时间

    public bool IsPlayDialogue = false;//是否正在播放对话
    private DialogueDataPack CurrentPlayingDialogueDataPack;//当前正在播放的对话包
    private DialoguePlayType CurrenPlayType;//当前播放类型
    #endregion

    #region 播放对话的两种方法(传入ID和传入对话名称)
    //播放对话
    public void StartPlayDialogue(int ID, DialoguePlayType PlayType = DialoguePlayType.Interact, bool IsCanSkip = true)
    {
        if (IsPlayDialogue)//如果正在播放对话，直接返回
            return;

        foreach (var pack in dialogueDataPacksList)
        {
            if (pack.dialoguePackID == ID)
            {
                CurrentPlayingDialogueDataPack = pack;
                CurrenPlayType = PlayType;
                PlayDialogue(IsCanSkip);
                return;
            }
        }
    }

    public void StartPlayDialogue(string DialogueName, DialoguePlayType PlayType = DialoguePlayType.Interact, bool IsCanSkip = true)
    {
        if (IsPlayDialogue)//如果正在播放对话，直接返回
            return;

        foreach (var pack in dialogueDataPacksList)
        {
            if (pack.dialoguePackName == DialogueName)
            {
                CurrentPlayingDialogueDataPack = pack;
                CurrenPlayType = PlayType;
                PlayDialogue(IsCanSkip);
                return;
            }
        }
    }

    private void PlayDialogue(bool IsCanSkip)//内部方法,调用面板进行调用
    {
        //对当前选中的对话数据包进行播放
        Debug.Log("开始播放对话");
        UImanager.Instance.ShowPanel<DialoguePanel>().StartDialoguePack(CurrentPlayingDialogueDataPack, CurrenPlayType, IsCanSkip);//显示对话面板并开始播放对话
    }
    #endregion

    #region Json数据导入对话数据的方法（未完成）

    #endregion

    #region 对话包ID分配
    public void ReassignAllIDs()
    {
        if (dialogueDataPacksList == null) return;

        // 重新分配从1开始的连续ID
        for (int i = 0; i < dialogueDataPacksList.Count; i++)
        {
            dialogueDataPacksList[i].dialoguePackID = i + 1;
        }
    }

    public int GetNextAvailableID()
    {
        if (dialogueDataPacksList == null || dialogueDataPacksList.Count == 0)
        {
            return 1;
        }

        int maxID = 0;
        foreach (var pack in dialogueDataPacksList)
        {
            if (pack.dialoguePackID > maxID)
            {
                maxID = pack.dialoguePackID;
            }
        }
        return maxID + 1;
    }
    #endregion
}