using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.InputSystem;

public class FlightAgent : Agent
{
    private InputAction m_rollAction;
    private InputAction m_pitchAction;
    private InputAction m_yawAction;

    // Cached input values, read in Update, consumed in Heuristic.
    private int m_roll;
    private int m_pitch;
    private int m_yaw;

    [Header("Plane Stats")]
    [Tooltip("How responsive the plane is when rolling, pitching, and yawing.")]
    public float responsiveness = 200;

    [SerializeField] private Transform targetTransform;

    public Rigidbody rb;

    private float responseModifier
    {
        get
        {
            return (rb.mass / 10f) * responsiveness;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody>();
        m_rollAction = InputSystem.actions.FindAction("Roll");
        m_pitchAction = InputSystem.actions.FindAction("Pitch");
        m_yawAction = InputSystem.actions.FindAction("Yaw");

        // Input actions must be enabled before ReadValue returns anything.
        m_rollAction.Enable();
        m_pitchAction.Enable();
        m_yawAction.Enable();
    }

    public override void OnEpisodeBegin()
    {
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        rb.angularVelocity = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(targetTransform.localPosition);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var c = actions.DiscreteActions;

        int roll = c[0] - 1;
        int pitch = c[1] - 1;
        int yaw = c[2] - 1;

        Debug.Log(roll);

        // apply forces to our plane (rotation of plane is scrambled)
        rb.linearVelocity = transform.forward * 50;
        rb.AddTorque(transform.up * yaw * responseModifier * Time.fixedDeltaTime);
        rb.AddTorque(transform.right * pitch * responseModifier * Time.fixedDeltaTime);
        rb.AddTorque(transform.forward * roll * responseModifier * Time.fixedDeltaTime);
    }

    private void Update()
    {
        // Read input in Update (synced with the Input System's event processing),
        // cache into fields. Heuristic runs in FixedUpdate and just copies these.
        m_roll = (int)m_rollAction.ReadValue<float>();
        m_pitch = (int)m_pitchAction.ReadValue<float>();
        m_yaw = (int)m_yawAction.ReadValue<float>();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = m_roll + 1;
        discreteActions[1] = m_pitch + 1;
        discreteActions[2] = m_yaw + 1;
    }

    public void OnCollisionEnter(Collision other)
    {
        Debug.Log($"trigger entered by: {other.gameObject.name}");

        if (other.gameObject.TryGetComponent<Goal>(out Goal goal))
        {
            SetReward(+1f);
            EndEpisode();
        }
    }
}