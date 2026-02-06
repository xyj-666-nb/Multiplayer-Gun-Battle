
[System.Serializable]
public enum E_EventType
{
    /// <summary>
    /// 获取场景加载进度
    /// </summary>
    E_LoadSceneChange,
    /// <summary>
    /// 游戏暂停
    /// </summary>
    E_GamePause,
    /// <summary>
    /// 游戏暂停结束
    /// </summary>
    E_GameResume,
    //玩家生成
    E_PlayerInit,
    //玩家销毁
    E_PlayerDestroy,

}
