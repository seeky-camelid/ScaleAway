using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public enum GameState
{
    // TODO: Add calibration
    //CalibrateLow,
    //CalibrateHigh,
    GameReady,
    Game,
    GameOver
}

public class GameManager : MonoBehaviour
{
    // Q: In C# what's the ctor-dtor model? If we initialize all members here, when are
    //    they actually initialized when used with a ctor?
    public static GameManager instance = null;

    public event Action StartGameEvent;
    public event Action EndGameEvent;
    public event Action RestartGameEvent;
    public event Action<int, int> CMajNoteAcceptedEvent; // (acceptedNote, currentStreak)
    public event Action<int> RewardCMajScaleEvent; // (finalStreak)

    private GameState state = GameState.GameReady;
    private string selectedMic = null;

    private int score = 0;
    private Coroutine incrementScore = null;

    public GameState State
    {
        get { return state; }
    }
    public string SelectedMic
    {
        get { return selectedMic; }
        set { selectedMic = value; }
    }

    public int Score
    {
        get { return score; }
    }

    // Q: In Unity3D, when is Awake, relative to ctor? When (or if) should we use ctor?
    private void Awake()
    {
        print("GameManager awakened");
        if (instance == null)
        {
            print("First GameManager awakened");
            instance = this;
        }
        else
        {
            print("Further GameManager awakened");
            Destroy(gameObject);
        }

        // Persist the gameobject across all scenes
        DontDestroyOnLoad(gameObject);
    }

    //public void RestartCalibrate()
    //{
    //    Assert.IsTrue(state == GameState.GameReady || state == GameState.GameOver);
    //    state = GameState.CalibrateLow;
    //}
    //public void FinishCalibrateLow()
    //{
    //    Assert.IsTrue(state == GameState.CalibrateLow);
    //    state = GameState.CalibrateHigh;
    //}
    //public void FinishCalibrateHigh()
    //{
    //    Assert.IsTrue(state == GameState.CalibrateHigh);
    //    state = GameState.GameReady;
    //}
    private IEnumerator AutoIncrementScore()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            score++;
        }
    }

    public void StartGame()
    {
        print("StartGame");
        Assert.IsTrue(state == GameState.GameReady);
        score = 0;
        state = GameState.Game;
        incrementScore = StartCoroutine(AutoIncrementScore());
        StartGameEvent?.Invoke();
    }
    public void EndGame()
    {
        print("EndGame");
        Assert.IsTrue(state == GameState.Game);
        state = GameState.GameOver;
        StopCoroutine(incrementScore);
        EndGameEvent?.Invoke();
    }
    public void RestartGame()
    {
        print("RestartGame");
        Assert.IsTrue(state == GameState.GameOver);
        state = GameState.GameReady;
        RestartGameEvent?.Invoke();
    }

    public void AcceptCMajScaleNote(int note, int streak)
    {
        print("AcceptCMajScaleNote");
        CMajNoteAcceptedEvent?.Invoke(note, streak);
    }
    public void RewardCMajScale(int finalStreak)
    {
        print("RewardCMajScale");
        score += finalStreak * 10; 
        RewardCMajScaleEvent?.Invoke(finalStreak);
    }

    private void OnDestroy()
    {
        print("GameManager destroyed");
    }
}
