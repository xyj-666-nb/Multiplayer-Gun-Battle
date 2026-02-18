using System.Collections.Generic;
using UnityEngine;

//地图信息类
[CreateAssetMenu(
    fileName = "NewMapInfo",
    menuName = "Game/ MapInfo",
    order = 100
)]

[System.Serializable]
public class MapInfo : ScriptableObject
{
    //地图信息类
    public string Name;//地图名
    public string Description;//地图主描述
    public Sprite mapSprite_UI;//地图UI

    public List<MapDetailPack> MapDetailPackList = new List<MapDetailPack>();

}

[System.Serializable]
public class MapDetailPack
{
    public Sprite mapSprite;//地图的介绍图片
    public string mapDetailDescription;//当前图片的介绍信息
}

