using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Linq.Expressions;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

/** Smooth time series data using moving average
 */
class MovingAverageFilter
{
    private Queue<float> window;
    private int maxWindowSize = 3;
    public MovingAverageFilter(int maxWindowSize)
    {
        Debug.Assert(maxWindowSize > 0);
        this.window = new Queue<float>();
        this.maxWindowSize = maxWindowSize;
    }

    /** Smooth signal val
     *  If val == NaN means no signal
     *  Return NaN if no signal
     */
    public float Smooth(float val)
    {
        float smoothedVal = val;
        if (float.IsNaN(val))
        {
            if (window.Count > 0)
            {
                window.Dequeue();
            }
        }
        else
        {
            //print("val: " + val);
            window.Enqueue(val);
            if (window.Count > maxWindowSize)
            {
                window.Dequeue();
            }

            if (window.Count == maxWindowSize)
            {
                smoothedVal = window.Average();
            }
        }
        return smoothedVal;
    }
}

/** Smooth time series data using spike filtering
 * https://gregstanleyandassociates.com/whitepapers/FaultDiagnosis/Filtering/Spike-Filter/spike-filter.htm
 */
class SpikeFilter
{
    private float prevValidVal;
    private float valThreshold;
    private int spikeCountThreshold;
    private int spikeCount;
    public SpikeFilter(float valThreshold, int spikeCountThreshold)
    {
        Debug.Assert(valThreshold > 0);
        Debug.Assert(spikeCountThreshold > 0);
        this.valThreshold = valThreshold;
        this.spikeCountThreshold = spikeCountThreshold;
        this.spikeCount = 0;
        this.prevValidVal = float.NaN;
    }

    /** Smooth signal val
     *  If val == NaN means no signal
     *  Return NaN if no signal
     */
    public float Smooth(float val)
    {
        float smoothedVal = val;
        if (!float.IsNaN(val))
        {
            //print("val: " + val);
            if (!float.IsNaN(prevValidVal) &&
                Mathf.Abs(val - prevValidVal) > valThreshold &&
                spikeCount <= spikeCountThreshold)
            {
                // It's a spike, use previous valid val
                smoothedVal = prevValidVal;
                spikeCount++;
            }
            else
            {
                // It's a normal value, update to new value
                smoothedVal = val;
                prevValidVal = smoothedVal;
                spikeCount = 0;
            }
        }
        return smoothedVal;
    }
}

/** Smooth time series data using moving average
 */
class QuantizeFilter
{
    private float step = 1;
    private float baseV = 0;
    public QuantizeFilter(float step, float baseV = 0)
    {
        Debug.Assert(step > 0);
        this.step = step;
        this.baseV = baseV;
    }

    /** Smooth signal val
     *  If val == NaN means no signal
     *  Return NaN if no signal
     */
    public float Smooth(float val)
    {
        float smoothedVal = val;
        if (!float.IsNaN(val))
        {
            float testAmount = val - baseV;
            float roundedTestAmount = Mathf.Round(testAmount / step) * step;
            smoothedVal = roundedTestAmount + baseV;
        }
        return smoothedVal;
    }
}

/**
 * Detect if there's a sequence of continuous notes from Major C scale: C, D, E, F, G, A, B
 * e.g. 
 *      C          Not OK (must be at least 2 notes)
 *      C->D->E    OK
 *      C->E       Not OK (must be continuous) 
 *      F->F#      Not OK (must be from Major C family)
 *      C->Blnk Not OK (silence doesn't count)
 *  0     1      2,   3,    4,  5,   6,   7,    8,   9,   10,   11,   12
 *  Blnk "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"
 */
public class CMajorScaleDetector
{
    private static List<int> CMajor = new List<int>{ 1, 3, 5, 6, 8, 10, 12};
    private Stack<int> notes = new Stack<int>();

    public event Action<int, int> NoteAccepted; // (acceptedNote, currentStreak)
    public event Action<int> ScaleDiscontinued; // (finalStreak)
    public static int Freq2Note(float frequency)
    {
        var noteNumber = Mathf.RoundToInt(12 * Mathf.Log(frequency / 440) / Mathf.Log(2) + 69);
        return noteNumber % 12 + 1;
    }
    public static string Note2Name(int note)
    {
        string[] names = {
            "Blank", "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"
        };
        return names[note];
    }

    static private bool AreConsecutiveNotes(int note0, int note1)
    {
        if (note0 == 1 && note1 == 12)
        {
            return true;
        }
        if (note0 == 12 && note1 == 1)
        {
            return true;
        }
        return Math.Abs(CMajor.IndexOf(note0) - CMajor.IndexOf(note1)) == 1;
    }

    public void AddNote(int note)
    {
        if (CMajor.Contains(note))
        {
            if (notes.Count == 0)
            {
                Debug.Log("A CMajor note " + note);
                notes.Push(note);
            }
            else
            {
                if (note != notes.Peek())
                {
                    if (AreConsecutiveNotes(note, notes.Peek()))
                    {
                        // A new, continuous CMajor note is added
                        notes.Push(note);
                        NoteAccepted?.Invoke(note, notes.Count);
                        Debug.Log("A new continuous CMajor note " + note);
                    }
                    else
                    {
                        ScaleDiscontinued?.Invoke(notes.Count);
                        notes.Clear();
                    }
                }
            }
        }
        else
        {
            if (notes.Count > 0)
            {
                ScaleDiscontinued?.Invoke(notes.Count);
                notes.Clear();
            }
        }
    }

}

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private AudioSource audioSource;
    [SerializeField]
    private AudioPitchEstimator estimator;
    [SerializeField]
    private TextMesh textFrequency;
    [SerializeField]
    private TextMesh textMin;
    [SerializeField]
    private TextMesh textMax;
    [SerializeField]
    private Transform player;
    [SerializeField]
    private float estimateRate = 30;

    private float yMax = 4;
    private float yMin = -4;
    private float yStep = 0.5f; // Smallest unit of player movment in axis y

    // For smoothing the estimated frequency signal

    // Method 2: Spike filter
    [SerializeField]
    private float spikeValThreshold = 15;    // 30 Hz is about 1 whole step
    [SerializeField]
    [Range(1, 10)]
    private int spikeCountThreshold = 3;
    private SpikeFilter spikeFilter;

    [SerializeField]
    private float chaseSpeed = 5f;
    [SerializeField]
    private float waveVel = 2f; // Speed at which to wave while cruising

    [SerializeField]
    private float smoothTime = 0f;

    /** There are 3 Ys:
     * targetY: Target Y level the player should be chasing to and maintaining at
     * followY: The immediate Y to follow. followY always tries to chase targetY to be the same as it
     * realY (player.position.y): The real y the player is currently at.
     *                            At State.Chase and State.ReachedTarget, it tries to be the same as followY.
     *                            At State.Cruise, it performs a wave motion around followY
     */ 
    private float targetY = 0f;
    private float followY = 0f;

    /*
     * Cruise -> if targetY == followY -> Cruise
     * Cruise -> if targetY != followY -> Chase
     * Chase -> if targetY != followY -> Chase
     * Chase -> if targetY == followY -> ReachedTarget
     * ReachedTarget -> if targetY != followY -> Chase
     * ReachedTarget -> if targetY == followY && elapsed time < stable threshold -> ReachedTarget
     * ReachedTarget -> if targetY == followY && elapsed time >= stable threshold -> Cruise
     */
    enum State
    {
        // Cruise at the same y position while doing wave motion
        Cruise,
        // Chase at a new target y position
        Chase,
        // Reached the new target y, but need to stablise before cruising
        ReachedTarget,
    }
    private State state = State.Cruise;
    private float stableThreshold = 0.5f; // How long in seconds till transition from ReachedTarget to Cruise
    private float stableTimer = 0f;
    [SerializeField]
    private float waveMag;              // Wave magnitude

    private CMajorScaleDetector cmajorDet;

    // Start is called before the first frame update
    void Start()
    {
        state = State.Chase;
        //waveMag = yStep / 2;
        spikeFilter = new SpikeFilter(spikeValThreshold, spikeCountThreshold);
        InvokeRepeating(nameof(UpdateF0), 0, 1.0f / estimateRate);
        cmajorDet = new CMajorScaleDetector();
        cmajorDet.NoteAccepted += (int note, int streak) => { GameManager.instance.AcceptCMajScaleNote(note, streak); };
        cmajorDet.ScaleDiscontinued += (int finalStreak) => { GameManager.instance.RewardCMajScale(finalStreak); };
    }

    static float MapRange(float old_v, float old_min, float old_max, float new_min, float new_max)
    {
        var position = (old_v - old_min) / (old_max - old_min);

        return new_min + position * (new_max - new_min);
    }

    void UpdateF0()
    {
        if (GameManager.instance.State != GameState.Game)
        {
            return;
        }
        var frequency = estimator.Estimate(audioSource);
        float smoothedFreq = spikeFilter.Smooth(frequency);
        //print("Smoothed frequency: " + smoothedFreq);

        if (!float.IsNaN(smoothedFreq))
        {
            var freqMin = estimator.frequencyMin;
            var freqMax = estimator.frequencyMax;
            float newYPos = MapRange(smoothedFreq, freqMin, freqMax, yMin, yMax);
            // Update marker text and position
            int note = CMajorScaleDetector.Freq2Note(smoothedFreq);
            string noteName = CMajorScaleDetector.Note2Name(note);
            textFrequency.text = string.Format("{0}\n{1:0.0} Hz", noteName, smoothedFreq);
            textFrequency.transform.position = new Vector3(textFrequency.transform.position.x, newYPos);
            cmajorDet.AddNote(note);
            
            // Update player target y
            float tempYSpeed = 0f;
            targetY = Mathf.SmoothDamp(player.position.y, newYPos, ref tempYSpeed, smoothTime);
            var quantizeF = new QuantizeFilter(yStep, yMin);
            targetY = quantizeF.Smooth(targetY);
        }
        else
        {
            cmajorDet.AddNote(0); // Record a blank note
        }

        // Update other frequency settings
        //textMin.text = string.Format("{0} Hz", estimator.frequencyMin);
        //textMax.text = string.Format("{0} Hz", estimator.frequencyMax);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey("escape"))
        {
            Application.Quit();
        }
    }
    void FixedUpdate()
    {
        if (GameManager.instance.State != GameState.Game)
        {
            return;
        }
        // Update state
        if (followY != targetY)
        {
            state = State.Chase;
        }
        else
        {
            if (state == State.Chase)
            {
                state = State.ReachedTarget;
            }
            else if (state == State.ReachedTarget)
            {
                if (stableTimer >= stableThreshold)
                {
                    state = State.Cruise;
                    stableTimer = 0;
                }
                else
                {
                    stableTimer += Time.fixedDeltaTime;
                }
            }
        }
        //print("Player state: " + state);


        // Update position based on state
        float newY;
        float newLookY;
        switch (state)
        {
            case State.Cruise:
                {
                    newLookY = waveVel > 0 ? followY + waveMag : followY - waveMag;
                    newY = Mathf.MoveTowards(player.position.y, newLookY, Mathf.Abs(waveVel) * Time.fixedDeltaTime);

                    // Change direction if hit boundary
                    // Note that we can't combine the two clauses into one with waveSpeed *= -1, as this may make the player
                    // stuck at one boundary indefinitely
                    if (player.position.y >= followY + waveMag - 0.01f)
                    {
                        waveVel = -1 * Mathf.Abs(waveVel);
                    }
                    else if (player.position.y <= followY - waveMag + 0.01f)
                    {
                        waveVel = Mathf.Abs(waveVel);
                    }
                    break;
                }
            case State.Chase:
                {
                    newLookY = targetY;
                    //followY = Mathf.MoveTowards(player.position.y, targetY,  chaseSpeed * Time.fixedDeltaTime);
                    float tempYSpeed = 0f;
                    followY = Mathf.SmoothDamp(player.position.y, targetY, ref tempYSpeed, chaseSpeed * Time.fixedDeltaTime);
                    if (Mathf.Abs(followY - targetY) <= 0.01f)
                    {
                        followY = targetY;
                    }
                    newY = followY;
                    break;
                }
            default:
            case State.ReachedTarget:
                {
                    newY = followY;
                    newLookY = followY;
                    break;
                }
        }

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();

        Vector3 lookTargetPos = new Vector3(player.position.x + 5f, newLookY);
        Vector2 lookDir = lookTargetPos - player.position;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
        float angleVel = 0;
        rb.rotation = Mathf.SmoothDampAngle(rb.rotation, angle, ref angleVel, Time.fixedDeltaTime);
        player.position = new Vector2(player.position.x, newY);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (GameManager.instance.State != GameState.Game)
        {
            return;
        }
        if (collision.gameObject.tag.ToLower() == "obstacle")
        {
            GetComponent<Rigidbody2D>().AddForce(new Vector2(-5, 2), ForceMode2D.Impulse);
            GameManager.instance.EndGame();
        }
    }
}

