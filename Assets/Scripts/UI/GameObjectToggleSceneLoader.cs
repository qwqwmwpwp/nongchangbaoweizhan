using UnityEngine;

public class GameObjectToggleSceneLoader : MonoBehaviour
{
    [SerializeField] private GameObject targetGameObject;

    public void ToggleGameObjectActive()
    {
        GameObject target = targetGameObject != null ? targetGameObject : gameObject;
        target.SetActive(!target.activeSelf);
    }

    public void CloseGameObject()
    {
        GameObject target = targetGameObject != null ? targetGameObject : gameObject;
        target.SetActive(false);
    }

    public void LoadLevel1()
    {
        LoadScene("Level1");
    }

    public void LoadLevel2()
    {
        LoadScene("Level2");
    }

    private void LoadScene(string sceneName)
    {
        if (SceneLoadManager.Instance == null)
        {
            Debug.LogError("GameObjectToggleSceneLoader: SceneLoadManager.Instance is null.", this);
            return;
        }

        SceneLoadManager.Instance.LoadScene(sceneName);
    }
}
