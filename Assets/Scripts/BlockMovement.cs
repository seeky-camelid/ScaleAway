using System.Collections;
using System.Collections.Generic;
using System.Xml.Schema;
using UnityEngine;

public class BlockMovement : MonoBehaviour
{
    [SerializeField] 
    private float moveSpeed = 3f;
    public float MoveSpeed
    {
        get { return moveSpeed; }
        set { moveSpeed = value; }
    }

    private float xMax = 20f;
    private float xMin = -20f;

    private GameManager gameManager;
    private List<GameObject> parentList;

    // Start is called before the first frame update
    void Start()
    {
        gameManager = GameManager.instance;
    }

    // Q: In Unity3D, When (or if) should we use ctor over Setup?
    public void Setup(float moveSpeed = 3f, List<GameObject> parentList = null, float xMax = 20f, float xMin = -20f)
    {
        this.moveSpeed = moveSpeed;
        this.parentList = parentList;
        this.xMax = xMax;
        this.xMin = xMin;
    }


    // Update is called once per frame
    void Update()
    {
        if (gameManager.State != GameState.Game)
        {
            return;
        }
        transform.position += Vector3.left * moveSpeed * Time.deltaTime;
        if (transform.position.x < xMin || transform.position.x > xMax)
        {
            parentList?.Remove(gameObject);
            Destroy(gameObject);
        }
    }
}
