using System.Collections.Generic;
using UnityEngine;

public class WarRecordPanel : BasePanel
{
    [Header("引用")]
    public GameObject playerInfoPrefabs;
    public Transform RedTeamUIParent;
    public Transform BlueTeamUIParent;

    private List<PlayerWarRecordUI> _redUIList = new List<PlayerWarRecordUI>();
    private List<PlayerWarRecordUI> _blueUIList = new List<PlayerWarRecordUI>();

    public void RefreshWarRecordData(NetworkPlayerInfo[] allPlayerData)
    {
        if (allPlayerData == null)
            return;

        List<NetworkPlayerInfo> redData = new List<NetworkPlayerInfo>();
        List<NetworkPlayerInfo> blueData = new List<NetworkPlayerInfo>();

        foreach (var data in allPlayerData)
        {
            if (data.Team == Team.Red)
                redData.Add(data);
            else if (data.Team == Team.Blue)
                blueData.Add(data);
        }

        UpdateTeamList(RedTeamUIParent, _redUIList, redData);
        UpdateTeamList(BlueTeamUIParent, _blueUIList, blueData);
    }

    private void UpdateTeamList(Transform parent, List<PlayerWarRecordUI> uiList, List<NetworkPlayerInfo> dataList)
    {
        // 判空保护
        if (parent == null || playerInfoPrefabs == null) return;

        for (int i = 0; i < dataList.Count; i++)
        {
            PlayerWarRecordUI ui = null;

            if (i < uiList.Count)
            {
                ui = uiList[i];
            }
            else
            {
                GameObject go = Instantiate(playerInfoPrefabs, parent);
                ui = go.GetComponent<PlayerWarRecordUI>();
                uiList.Add(ui);
            }

            if (ui != null)
            {
                ui.gameObject.SetActive(true);
                ui.UpdateInfo(dataList[i].KillCount.ToString(), dataList[i].DeathCount.ToString() , dataList[i].PlayerName, null);
            }
        }

        // 隐藏多余的
        for (int i = dataList.Count; i < uiList.Count; i++)
        {
            if (uiList[i] != null)
            {
                uiList[i].gameObject.SetActive(false);
            }
        }
    }

    #region 面板显隐 (关键修改在这里)
    public override void ShowMe(bool isNeedDefaultAnimator = true)
    {
        base.ShowMe(isNeedDefaultAnimator);

        NetworkPlayerInfo[] cachedData = PlayerRespawnManager.GetCachedData();
        if (cachedData != null)
        {
            RefreshWarRecordData(cachedData);
        }
        else
        {
            Debug.Log("[战绩面板] 暂无缓存数据，等待服务器同步...");
        }
    }

    protected override void SpecialAnimator_Show()
    {
        throw new System.NotImplementedException();
    }

    protected override void SpecialAnimator_Hide()
    {
        throw new System.NotImplementedException();
    }

    public override void SimpleHidePanel()
    {
        base.SimpleHidePanel();
    }

    public override void SimpleShowPanel()
    {
        base.SimpleShowPanel();
        NetworkPlayerInfo[] cachedData = PlayerRespawnManager.GetCachedData();
        if (cachedData != null)
        {
            RefreshWarRecordData(cachedData);
        }
        else
        {
            Debug.Log("[战绩面板] 暂无缓存数据，等待服务器同步...");
        }
    }
    #endregion

    #region 生命周期
    public override void Awake()
    {
        base.Awake();
    }
    #endregion

    #region UI控件
    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        if (controlName == "ExitButton")
        {
            SimpleHidePanel();
        }
    }
    #endregion
}