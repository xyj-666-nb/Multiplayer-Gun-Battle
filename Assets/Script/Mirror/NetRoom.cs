using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class NetRoom : MonoBehaviour//房间列表中每一行的UI逻辑脚本，负责显示房间信息和处理加入房间按钮的点击事件
{
    public TextMeshProUGUI RoomName;//显示房间名称的文本组件
    public TextMeshProUGUI PlayerCount;//显示当前人数和最大人数的文本组件
    public TextMeshProUGUI PlayerName;//显示房主名称的文本组件
    public TextMeshProUGUI gameModeTMesh;//显示游戏模式的文本组件
    public Button EnterButton;//加入房间的按钮组件

    public void Bind(string name, int playerCount, int maxPlayers, string _PlayerName, GameMode gameMode, Uri uri, System.Action<Uri> onJoin)
    {
        RoomName.text = $"房间名：{name}";
        PlayerCount.text = $"人数：{playerCount}/{maxPlayers}";
        PlayerName.text = $"房主名称：{_PlayerName}";
        string Name = "";
        switch (gameMode)
        {
            case GameMode.Team_Battle:
                Name = "团队竞技";
                break;
            case GameMode.Control_Point:
                Name = "站点模式";
                break;
            case GameMode.Bomb_Mode:
                Name = "爆破模式";
                break;
        }
        gameModeTMesh.text = $"游戏模式：{Name}";
        EnterButton.onClick.RemoveAllListeners();
        EnterButton.onClick.AddListener(() => { UImanager.Instance.HidePanel<EnterRoomPanel>(); onJoin(uri); });//点击当前按钮就传入当前获取的Uri，然后触发传进来的加入房间的逻辑，触发进入房间
    }
    public void UpdateCount(int playerCount, int maxPlayers)
    {
        PlayerCount.text = $"人数：{playerCount}/{maxPlayers}";
    }
}
