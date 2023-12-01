using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

public class GameUI : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI scoreText;

    [SerializeField]
    private TextMeshProUGUI rewardText;

    private Coroutine disableRewardText;
    // Start is called before the first frame update
    void Start()
    {
        Assert.IsNotNull(scoreText);
        scoreText.text = "Score: ";
        rewardText.enabled = false;
        GameManager.instance.CMajNoteAcceptedEvent += OnCMajNoteAccepted;
    }

    private void OnCMajNoteAccepted(int note, int streak)
    {
        if (disableRewardText != null)
        {
            StopCoroutine(disableRewardText);
        }
        rewardText.enabled = true;
        rewardText.text = "CMajor Scale combo! " + CMajorScaleDetector.Note2Name(note) + " streak: " + streak;
        disableRewardText = StartCoroutine(DisableRewardText());
    }

    private IEnumerator DisableRewardText()
    {
        yield return new WaitForSeconds(3);
        rewardText.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        scoreText.text = "Score: " + GameManager.instance.Score; 
    }
}
