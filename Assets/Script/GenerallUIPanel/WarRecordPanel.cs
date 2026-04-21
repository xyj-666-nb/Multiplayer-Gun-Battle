using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WarRecordPanel : BasePanel
{
    private const string UiBackSound = "Music/update415/ui返回";

    [Header("引用")]
    public GameObject playerInfoPrefabs;
    public Transform RedTeamUIParent;
    public Transform BlueTeamUIParent;

    private List<PlayerWarRecordUI> _redUIList = new List<PlayerWarRecordUI>();
    private List<PlayerWarRecordUI> _blueUIList = new List<PlayerWarRecordUI>();

    public Action OnPanelClosed;

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
        if (parent == null || playerInfoPrefabs == null)
            return;

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
                if (dataList[i].GunName != null)
                    ui.UpdateInfo(dataList[i].KillCount.ToString(), dataList[i].DeathCount.ToString(), dataList[i].PlayerName, MilitaryManager.Instance.GetInfo(dataList[i].GunName).GunSprite);
                else
                    ui.UpdateInfo(dataList[i].KillCount.ToString(), dataList[i].DeathCount.ToString(), dataList[i].PlayerName, null);
            }
        }

        for (int i = dataList.Count; i < uiList.Count; i++)
        {
            if (uiList[i] != null)
            {
                uiList[i].gameObject.SetActive(false);
            }
        }
    }

    public void SetCloseCallback(Action callback)
    {
        OnPanelClosed = callback;
    }

    #region 面板显隐
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
    }

    protected override void SpecialAnimator_Hide()
    {
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
            MusicManager.Instance?.PlayEffect(UiBackSound);
            if (UImanager.Instance != null)
            {
                UImanager.Instance.HidePanel<WarRecordPanel>(true, () =>
                {
                    Action callback = OnPanelClosed;
                    OnPanelClosed = null;
                    callback?.Invoke();
                });
                return;
            }

            SimpleHidePanel();
            Action fallbackCallback = OnPanelClosed;
            OnPanelClosed = null;
            fallbackCallback?.Invoke();
        }
    }
    #endregion
}
