using UnityEngine;

//战术道具信息类
[CreateAssetMenu(
    fileName = "NewTacticInfo",
    menuName = "Game/Tactic Info",
    order = 100
)]
[System.Serializable]
public class TacticInfo : ScriptableObject
{
    [Header("通用的设计")]
    public string Name;
    [TextArea(3, 5)]
    public string Description;
    public Sprite UISprite;//ui使用的图标
    public Sprite GameBodySprite;//游戏中使用的图标
    public GameObject TacticPrefab;//效果预制体
    public TacticType tacticType;//战术道具类型
}
