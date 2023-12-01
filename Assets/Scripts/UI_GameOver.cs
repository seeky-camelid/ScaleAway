using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

public class GameOver : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI gameOverText;
    // Start is called before the first frame update
    void Start()
    {
        Assert.IsNotNull(gameOverText);
        gameOverText.text = "You lost! Final score: " + GameManager.instance.Score;
    }
}
