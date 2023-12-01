using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;

public class BlockGenerator : MonoBehaviour
{
    [SerializeField]
    private GameObject blockPrefab;

    private GameManager gameManager;

    /// 
    ///  TODO: Refactor hardcoded
    ///
    private float yMin = -4;
    private float yMax = 4;
    private float x = 15;
    private float xMin = -13;
    private float xMax = 13;
    private float yStep = 0.5f; // Smallest unit of player movment in axis y
    // What's the minimum number of ySteps in yPos in between two consecutive blocks 
    [SerializeField]
    [Range(0, 10)]
    private int minYGap = 2;
    private float previousY = 0f;

    [SerializeField]
    [Range(1, 15)]
    private int maxNumActiveBlocks = 5; // Max number of active blocks

    private int level = 1;
    public int Level { get { return level; } }
    private const float levelIncrementTime = 30f; // How long, in seconds to increment level

    private List<GameObject> blocks;
    private static float GetBlockYPos(float yMin, float yMax, float yStep, int minYGap, float prevY)
    {
        const int maxTry = 10;
        int tryCount = 0;
        int numberOfSteps = Mathf.FloorToInt((yMax - yMin) / yStep);
        Assert.IsTrue(numberOfSteps > 2);
        Assert.IsTrue(numberOfSteps > minYGap);

        float nextY = Random.Range(1, numberOfSteps - 1) * yStep + yMin;
        tryCount++;
        while ((Mathf.CeilToInt(Mathf.Abs(nextY - prevY) / yStep) < minYGap) && (tryCount <= maxTry))
        {
            nextY = Random.Range(1, numberOfSteps - 1) * yStep + yMin;
            tryCount++;
        }

        return nextY;
    }
    private static float GetBlockSpeed(int level)
    {
        return level * 2;
    }
    private static float GetBlockGenCooldown(float sceneSize, int maxNumActiveBlocks, float blockSpeed)
    {
        // The goal is to make sure we always maintain a fixed number of active blocks
        // So we need to adjust the cooldown as the speed increases

        // Note that we don't need to be exact; it's fine to roughly maintain maxNumActiveBlocks 

        float avgGapSize = (sceneSize / maxNumActiveBlocks);
        float gapSize = Utils.Utils.NextGaussian(avgGapSize, 1f);
        float cooldown = gapSize / blockSpeed;

        return cooldown;
    }

    // Start is called before the first frame update
    void Start()
    {
        // RULE: Only grab a singleton at Start
        gameManager = GameManager.instance;
        gameManager.StartGameEvent += StartGenerating;
        gameManager.EndGameEvent += StopGenerating;
        blocks = new List<GameObject>();
        ResetLevel();
    }


    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.StartGameEvent -= StartGenerating;
            gameManager.EndGameEvent -= StopGenerating;

        }
    }
    private void StartGenerating()
    {
        StartCoroutine(GenerateNextBlock());
        StartCoroutine(IncrementLevel());
    }

    private void StopGenerating()
    {
        StopAllCoroutines();
    }

    public void ResetLevel()
    {
        level = 1;
        UpdateBlocks(level);
    }

    private IEnumerator GenerateNextBlock()
    {
        float cooldown = GetBlockGenCooldown(xMax - xMin, maxNumActiveBlocks, GetBlockSpeed(level));
        yield return new WaitForSeconds(cooldown);
        float yPos = GetBlockYPos(yMin, yMax, yStep, minYGap, previousY);
        var block = Instantiate(blockPrefab, new Vector2(x, yPos), Quaternion.identity);
        previousY = yPos;
        float speed = GetBlockSpeed(level);
        block.GetComponent<BlockMovement>().Setup(speed, blocks);
        blocks.Add(block);
        print("Number of blocks: " + blocks.Count);
        StartCoroutine(GenerateNextBlock());
    }

    private IEnumerator IncrementLevel()
    {
        yield return new WaitForSeconds(levelIncrementTime);
        level++;
        UpdateBlocks(level);
        StartCoroutine(IncrementLevel());
    }

    private void UpdateBlocks(int level)
    {
        float newSpeed = GetBlockSpeed(level);
        foreach (var block in blocks)
        {
            block.GetComponent<BlockMovement>().MoveSpeed = newSpeed;
        }
    }

}
