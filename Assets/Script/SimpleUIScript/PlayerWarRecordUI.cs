using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerWarRecordUI : MonoBehaviour
{
    public TextMeshProUGUI PlayerKillText;
    public TextMeshProUGUI PlayerDeadText;
    public TextMeshProUGUI PlayerName;
    public Image CurrentPlayerGunSprite;

    public void InitInfo(string KillText, string DeathCount, string Name, Sprite GunSprite)
    {
        UpdateInfo(KillText, Name, DeathCount, GunSprite);
    }

    public void UpdateInfo(string KillText,string DeathCount, string Name, Sprite GunSprite)
    {
        PlayerKillText.text = KillText;
        PlayerDeadText.text=DeathCount;

        if (PlayerName != null)
            PlayerName.text = Name;

        if (CurrentPlayerGunSprite != null)
        {
            CurrentPlayerGunSprite.sprite = GunSprite;
            // 如果没有图片，可以隐藏，或者设为透明
            CurrentPlayerGunSprite.enabled = (GunSprite != null);
        }
    }
}