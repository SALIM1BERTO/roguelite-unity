using UnityEngine;
using UnityEngine.UI;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;

    public GameObject upgradePanel;
    public Button[] upgradeButtons = new Button[3];
    public Text[] upgradeTexts = new Text[3];

    private void Awake()
    {
        Instance = this;
    }

    public void TriggerLevelUp()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ShowLevelUpScreen();
        }
    }
}
