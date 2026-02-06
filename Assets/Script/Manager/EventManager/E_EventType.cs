
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
    // 触摸相关事件
    E_TouchBegan,    // 触摸开始
    E_TouchMoved,    // 触摸移动
    E_TouchEnded,    // 触摸结束
    E_LongPress,     // 长按
    E_Swipe,         // 滑动

    E_playerLeftMove,//左移动
    E_playerRightMove,//右移动

    E_playerJump,//玩家跳跃
    E_playerReload,

    //玩家射击
    E_playerShootDown,
    E_playerShootSignal,//单次射击的效果事件

    //弹药显示相关
    E_InitGunInfoUI,//更新枪械信息的ui
    E_UpdateGunBulletUi,//更新枪械的弹药信息

    //玩家和枪械
    E_playerLoseGun,//玩家丢弃当前枪械
    E_playerGetGun,//玩家获得枪械

    //玩家生成
    E_PlayerInit,
    //玩家销毁
    E_PlayerDestroy,

}
