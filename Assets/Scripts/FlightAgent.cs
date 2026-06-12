using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.InputSystem;

public class FlightAgent : Agent
{
    // Input actions for human control
    private InputAction m_rollAction;
    private InputAction m_pitchAction;
    private InputAction m_yawAction;

    // Cached input values, read in Update, consumed in Heuristic
    private int m_roll;
    private int m_pitch;
    private int m_yaw;

    [Header("Plane Stats")]
    [Tooltip("How responsive the plane is when rolling, pitching, and yawing.")]
    public float responsiveness = 5;
    private float prevDist = 0;
    float facingCoeff = .01f;
    float distanceCoeff = 0.01f;
    float reachReward = 50f;

    // Transform references for target and guiding pointer objects
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Transform guideTransform;

    public Rigidbody rb;

    // Roll, pitch, yaw responsiveness to inputs
    private float responseModifier
    {
        get
        {
            return (rb.mass / 10f) * responsiveness;
        }
    }

    /**
     * Awake method for class initialization.
     * Sets up rigid body reference and action mapping for human input.
     */
    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody>();
        m_rollAction = InputSystem.actions.FindAction("Roll");
        m_pitchAction = InputSystem.actions.FindAction("Pitch");
        m_yawAction = InputSystem.actions.FindAction("Yaw");

        m_rollAction.Enable();
        m_pitchAction.Enable();
        m_yawAction.Enable();
    }

    /**
     * New episode initialization.
     * Resets plane to neutral position and generates new target position around plane.
     */
    public override void OnEpisodeBegin()
    {
        // Reset plane position and variables
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        rb.angularVelocity = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
        prevDist = 0;

        // Generate random numbers for target position
        float randomX = Random.Range(-1f, 1f);
        float randomY = Random.Range(-1f, 1f);
        float randomZ = Random.Range(-1f, 1f);
        float randomDistance = Random.Range(50, 200);

        // Calculate target position using vectors
        Vector2 randomDirection = new Vector3(randomX, randomY, randomZ);
        randomDirection.Normalize();
        Quaternion rotation = Quaternion.LookRotation(randomDirection, Vector3.forward);
        targetTransform.rotation = rotation;
        targetTransform.position = transform.position + targetTransform.forward * randomDistance;
    }

    /**
     * Allow agent to observe the state of the environment.
     * Specifically passes plane's position and target's position.
     */
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(targetTransform.localPosition);
    }

    /**
     * Per step processing of inputs and rewards.
     * Sets up rewards constants
     */
    public override void OnActionReceived(ActionBuffers actions)
    {
        // Set up rewards constants
        float maxSteps = MaxStep != 0 ? MaxStep : 2000f;

        // Calculate plane's angle and distance from goal; penalize looking away and far away
        Vector3 toGoal = (targetTransform.position - transform.position).normalized;
        float alignment = Vector3.Dot(transform.forward, toGoal);       // +1 = pointing right at goal, -1 = away
        float dist = Vector3.Distance(transform.position, targetTransform.position);

        // facing: per-step, small
        AddReward(alignment * facingCoeff);
        // distance: progress in units, scaled down to comparable magnitude
        AddReward((prevDist - dist) * distanceCoeff);
        // time: penalize stalling
        AddReward(-1f / maxSteps);

        prevDist = dist;

        // Retrieve actions from agent
        var c = actions.DiscreteActions;
        int roll = c[0] - 1;
        int pitch = c[1] - 1;
        int yaw = c[2] - 1;

        // Apply forces to our plane
        rb.linearVelocity = transform.forward * 50;                     // constant speed for simplicity
        rb.AddTorque(transform.up * yaw * responseModifier);
        rb.AddTorque(transform.right * pitch * responseModifier);
        rb.AddTorque(transform.forward * roll * responseModifier);
    }

    /**
     * Environment update, polling human input and guide indicator.
     */
    private void Update()
    {
        // Read input in Update (synced with the Input System's event processing),
        // cache into fields. Heuristic runs in FixedUpdate and just copies these.
        m_roll = (int)m_rollAction.ReadValue<float>();
        m_pitch = (int)m_pitchAction.ReadValue<float>();
        m_yaw = (int)m_yawAction.ReadValue<float>();

        // Calculate guide object to point towards goal relative to plane
        Vector3 guideDirection = targetTransform.position - transform.position;
        guideDirection.Normalize();
        Quaternion rotation = Quaternion.LookRotation(guideDirection, Vector3.up);

        // If the arrow mesh's tip points up (+Y) in local space:
        guideTransform.rotation = rotation * Quaternion.Euler(90f, 0f, 0f);
        guideTransform.position = transform.position;
    }

    /**
     * Human input processing into agent environment.
     * Utilized for manually testing and demo recordings.
     */
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = m_roll + 1;
        discreteActions[1] = m_pitch + 1;
        discreteActions[2] = m_yaw + 1;
    }

    /**
     * Condition check for reaching reward
     */
    public void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.TryGetComponent<Goal>(out Goal goal))
        {
            AddReward(reachReward);
            EndEpisode();
        }
    }
}