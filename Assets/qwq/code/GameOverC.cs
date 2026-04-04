using UnityEngine;

public class GameOverC : MonoBehaviour
{
    public static GameOverC instance;
    [SerializeField] GameOverM gameOverM;
    private void Awake()
    {
        if (instance) Destroy(this);
        else instance = this;
    }
    public void GameOver()
    {
        gameOverM.enterUI();
    }
}
