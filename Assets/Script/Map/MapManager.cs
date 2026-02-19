using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

[System.Serializable]
public class TeamBornPosList
{
    public List<Transform> RedTeamBornPosList;//红队出生位置列表
    public List<Transform> BlueTeamBornPosList;//蓝队队出生位置列表
}


public class MapManager : MonoBehaviour
{
    public TeamBornPosList teamBornPosList = new TeamBornPosList();

    [Header("入场动画")]
    public PlayableDirector RedTimeLine;//红队入场动画
    public PlayableDirector BlueTimeLine;//蓝队入场动画

    public void TriggerAnima()
    {
        //根据玩家当前的队伍状态播放对应的Timeline
        if (Player.LocalPlayer.CurrentTeam == Team.Red)
            RedTimeLine.Play();//播放
        else
            BlueTimeLine.Play();//播放蓝方动画
    }

    public void AnimaEnd()
    {
        //动画结束
        UImanager.Instance.GetPanel<PlayerPanel>().SimpleShowPanel();
        UImanager.Instance.ShowPanel<GameScorePanel>();//打开比分面板
        PlayerAndGameInfoManger.Instance.EquipCurrentSlot();//获取战备
    }

}