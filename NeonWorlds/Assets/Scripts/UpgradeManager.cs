using UnityEngine;
using UnityEngine.UI;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;
    
    public GameObject upgradePanel;
    public Button[] upgradeButtons = new Button[3];
    public Text[] upgradeTexts = new Text[3];

    private void Awake() { Instance = this; }

    public void TriggerLevelUp()
    {
        Time.timeScale = 0f; // Pausa o jogo
        if (upgradePanel) upgradePanel.SetActive(true);

        string[] pool = { 
            "+ Velocidade", 
            "+ Tiro Rápido", 
            "+ Vida Máxima", 
            "Cura Completa" 
        };
        
        for (int i = 0; i < 3; i++)
        {
            int choice = Random.Range(0, pool.Length);
            if (upgradeTexts[i]) upgradeTexts[i].text = pool[choice];
            
            if (upgradeButtons[i])
            {
                upgradeButtons[i].onClick.RemoveAllListeners();
                upgradeButtons[i].onClick.AddListener(() => ApplyUpgrade(choice));
            }
        }
    }

    void ApplyUpgrade(int choice)
    {
        PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
        Weapon w = FindAnyObjectByType<Weapon>();
        
        if (choice == 0 && pm) pm.moveSpeed += 2f;
        else if (choice == 1 && w) w.fireRate = Mathf.Max(0.05f, w.fireRate - 0.05f); // Atira mais rapido
        else if (choice == 2) { 
            if (GameManager.Instance) {
                GameManager.Instance.maxHp += 20; 
                GameManager.Instance.hp += 20; 
            }
        }
        else if (choice == 3) {
            if (GameManager.Instance) GameManager.Instance.hp = GameManager.Instance.maxHp;
        }

        if (upgradePanel) upgradePanel.SetActive(false);
        Time.timeScale = 1f; // Volta o jogo
    }
}
