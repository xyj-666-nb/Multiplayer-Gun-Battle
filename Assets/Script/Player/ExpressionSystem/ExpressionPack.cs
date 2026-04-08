using UnityEngine;

[CreateAssetMenu(
    fileName = "NewExpressionPackConfig",
    menuName = "Game/ExpressionPack",
    order = 101
)]

//表情数据包
public class ExpressionPack : ScriptableObject
{
    public Sprite ExpressionSprite;
    public int ExpressionID;//表情ID
}