using UnityEditor;
using UnityEngine;

#if NEONWORLDS_LEGACY_AUTO_SETUP
[InitializeOnLoad]
#endif
public class UpdateWeaponOnCompile {
    static UpdateWeaponOnCompile() {
        EditorApplication.delayCall += () => {
            GameObject player = GameObject.Find("Player");
            if (player != null) {
                Weapon w = player.GetComponent<Weapon>();
                if (w != null) {
                    w.fireRate = 2.5f;
                    w.damage = 5;
                    EditorUtility.SetDirty(w);
                    Debug.Log("Updated Weapon fireRate to 0.2 and damage to 5 on the Player after hot reload.");
                }
            }
        };
    }
}
