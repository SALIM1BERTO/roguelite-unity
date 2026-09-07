using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

[InitializeOnLoad]
public static class InputModuleSetup
{
    static InputModuleSetup()
    {
        EditorApplication.delayCall += ConvertEventSystem;
    }

    [MenuItem("NeonWorlds/Setup/Convert EventSystem To New Input")]
    public static void ConvertEventSystem()
    {
        EventSystem es = Object.FindAnyObjectByType<EventSystem>();
        if (es == null) return;

        StandaloneInputModule standalone = es.GetComponent<StandaloneInputModule>();
        if (standalone != null)
        {
            Undo.DestroyObjectImmediate(standalone);
        }

        InputSystemUIInputModule inputModule = es.GetComponent<InputSystemUIInputModule>();
        if (inputModule == null)
        {
            inputModule = Undo.AddComponent<InputSystemUIInputModule>(es.gameObject);
            inputModule.AssignDefaultActions();
        }
        else if (inputModule.actionsAsset == null)
        {
            inputModule.AssignDefaultActions();
        }

        EditorUtility.SetDirty(es.gameObject);
        if (!Application.isPlaying && es.gameObject.scene.isLoaded)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(es.gameObject.scene);
        }
    }
}
