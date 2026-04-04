using UnityEngine;

public class GameOverM : MonoBehaviour
{
    [SerializeField] GameObject gameOverUI;
  public  void enterUI()
    {
        gameOverUI.SetActive(true);
    }
}