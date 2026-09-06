using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;

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
