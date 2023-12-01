using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public static MenuManager instance = null;

    [SerializeField]
    private GameObject gameReadyMenu;
    [SerializeField]
    private GameObject gameOverMenu;
    [SerializeField]
    private GameObject gameUI;

    private GameManager gameManager;

    private void Awake()
    {
        print("MenuManager Awaken");
        if (instance == null)
        {
            print("First MenuManager Awaken");
            instance = this;
        }
        else
        {
            print("Further MenuManager Awaken");
            Destroy(gameObject);
        }

        // Persist the gameobject across all scenes
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // RULE: Only grab a singleton at Start
        gameManager = GameManager.instance;

        Assert.IsTrue(gameManager != null);
        gameManager.StartGameEvent += OnStartGame;
        gameManager.EndGameEvent += OnEndGame;
        gameManager.RestartGameEvent += OnRestartGame;
        Assert.IsTrue(gameManager.State == GameState.GameReady);
        
    }

    private void OnStartGame()
    {
        gameReadyMenu.SetActive(false);
        gameOverMenu.SetActive(false);
        gameUI.SetActive(true);
    }

    private void OnEndGame()
    {
        gameReadyMenu.SetActive(false);
        gameOverMenu.SetActive(true);
        gameUI.SetActive(true);
    }
    private void OnRestartGame()
    {
        gameReadyMenu.SetActive(true);
        gameOverMenu.SetActive(false);
        gameUI.SetActive(false);
        SceneManager.LoadScene("Main");
    }

    private void OnDestroy()
    {
        print("MenuManager destroyed!!!!!");
        if (gameManager != null)
        {
            gameManager.StartGameEvent -= OnStartGame;
            gameManager.EndGameEvent -= OnEndGame;
            gameManager.RestartGameEvent -= OnRestartGame;
        }
    }

}
