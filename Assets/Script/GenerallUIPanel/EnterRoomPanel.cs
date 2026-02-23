using System.Collections.Generic;
using TMPro;
using UnityEngine.Events;
using UnityEngine;

public class EnterRoomPanel : BasePanel
{
    #region  核心变量
    [Header("预制体 & 挂点")]
    [SerializeField] private GameObject RoomObj;                 // 房间预制体
    [SerializeField] private Transform ListRoot;                 // ScrollView 的 Content，也是房间需要生成在的地方
    [SerializeField] private LanRoomClientBrowser lanRoomClientBrowser;//房间搜索组件引用
    public TMP_InputField PlayerNameInputField;//玩家名字的输入框

    #region 生成的房间字典以及字典管理
    // 本地缓存：serverId -> 已生成的UI行
    private readonly Dictionary<long, NetRoom> CurrentCreateRoomDic = new();
    private int _rowCount = 0;

    private void ClearList()
    {
        CurrentCreateRoomDic.Clear();
        _rowCount = 0;
        for (int i = ListRoot.childCount - 1; i >= 0; i--)
            Destroy(ListRoot.GetChild(i).gameObject);
    }
    #endregion

    #endregion

    #region 生命周期

    public override void Awake()
    {
        base.Awake();
    }

    public override void Start()
    {
        base.Start();

        var disco = lanRoomClientBrowser?.discovery;//代码订阅事件，监听到服务器广播时调用 HandleServerFound 方法
        if (disco != null)
        {
            disco.OnServerFound.RemoveListener(HandleServerFound);
            disco.OnServerFound.AddListener(HandleServerFound);
        }

        PlayerNameInputField.onValueChanged.AddListener((str) => { Main.PlayerName = str; });
    }

    #endregion

    #region 面板显隐以及特殊动画
    public override void ShowMe(bool IsNeedDefalutAnimator = true)
    {
        base.ShowMe(IsNeedDefalutAnimator);
        ClearList();

        Debug.Log("CLIENT: StartDiscovery()");
        lanRoomClientBrowser.discovery.StartDiscovery();
    }


    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
    {
        lanRoomClientBrowser?.StopScan();
        base.HideMe(callback, isNeedDefaultAnimator);
    }

    protected override void SpecialAnimator_Hide()
    {

    }

    protected override void SpecialAnimator_Show()
    {

    }

    #endregion

    #region UI控件逻辑

    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        switch (controlName)
        {
            case "RefreshButton":
                ClearList();
                lanRoomClientBrowser?.StartScan();   // 重新广播一次
                break;

            case "ExitButton":
                UImanager.Instance.ShowPanel<RoomPanel>();
                UImanager.Instance.HidePanel<EnterRoomPanel>();
                lanRoomClientBrowser?.StopScan();//停止广播，避免在切换面板后还继续接收服务器广播信息
                break;
        }
    }

    #endregion

    #region 创建房间并传入信息

    private void HandleServerFound(ServerResponse info)//当接收到服务器广播时，更新UI列表（这里接受的是房主传来的信息包资源）
    {
        if (CurrentCreateRoomDic.TryGetValue(info.serverId, out var row))//如果已经存在对应服务器ID的UI行，就更新显示信息（房间名称、人数等），否则生成一行新的UI
        {
            row.UpdateCount(info.playerCount, info.maxPlayers);
            return;
        }

        // 生成一行
        var go = Instantiate(RoomObj, ListRoot);
        var netRoom = go.GetComponent<NetRoom>();
        CurrentCreateRoomDic[info.serverId] = netRoom;

        // 绑定显示与加入逻辑
        netRoom.Bind(
            name: info.roomName,
            playerCount: info.playerCount,
            maxPlayers: info.maxPlayers,
            _PlayerName: info.PlayerName,
            gameMode: info.GameMode,
            GameTime: info.gameTime,
            GameScore: info.GoldScore,
            uri: info.uri,
            onJoin: (uri) => lanRoomClientBrowser.JoinByUri(uri)
        );
    }
    #endregion

}
