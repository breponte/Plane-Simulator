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
    private InputAction m_throttleAction;

    // Cached input values, read in Update, consumed in Heuristic.
    private float m_roll;
    private float m_pitch;
    private float m_yaw;
    private float m_throttle;

    [Header("Plane Stats")]
    [Tooltip("How much the throttle ramps up or down.")]
    public float throttleIncrement = 0.5f;
    [Tooltip("Maximum engine thrust when at 100% throttle.")]
    public float maxThrottle = 100f;
    [Tooltip("How responsive the plane is when rolling, pitching, and yawing.")]
    public float responsiveness = 10f;
    [Tooltip("How much lift force this plane generates as it gains speed.")]
    public float lift = 30f;

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
        m_throttleAction = InputSystem.actions.FindAction("Throttle");

        // Input actions must be enabled before ReadValue returns anything.
        m_rollAction.Enable();
        m_pitchAction.Enable();
        m_yawAction.Enable();
        m_throttleAction.Enable();
    }

    public override void OnEpisodeBegin()
    {
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        rb.angularVelocity = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
        m_throttle = 0f;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(targetTransform.localPosition);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var c = actions.ContinuousActions;

        float roll = c[0];
        float pitch = c[1];
        float yaw = c[2];
        float throttle = c[3];

        // apply forces to our plane (rotation of plane is scrambled)
        rb.AddForce(transform.forward * maxThrottle * throttle * Time.deltaTime * 20);
        rb.AddTorque(transform.up * yaw * responseModifier * Time.deltaTime * 20);
        rb.AddTorque(transform.right * pitch * responseModifier * Time.deltaTime * 20);
        rb.AddTorque(transform.forward * roll * responseModifier * Time.deltaTime * 20);

        rb.AddForce(Vector3.up * rb.linearVelocity.magnitude * lift);
    }

    private void Update()
    {
        // Read input in Update (synced with the Input System's event processing),
        // cache into fields. Heuristic runs in FixedUpdate and just copies these.
        m_roll = m_rollAction.ReadValue<float>();
        m_pitch = m_pitchAction.ReadValue<float>();
        m_yaw = m_yawAction.ReadValue<float>();

        float throttleInput = m_throttleAction.ReadValue<float>();
        if (m_throttleAction.ReadValue<float>() > 0) m_throttle += throttleIncrement;
        else if (m_throttleAction.ReadValue<float>() < 0) m_throttle -= throttleIncrement;
        m_throttle = Mathf.Clamp(m_throttle, 0f, 100f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = m_roll;
        continuousActions[1] = m_pitch;
        continuousActions[2] = m_yaw;
        continuousActions[3] = m_throttle;
    }

    public void OnCollisionEnter(Collision other)
    {
        Debug.Log($"trigger entered by: {other.gameObject.name}");

        if (other.gameObject.TryGetComponent<Goal>(out Goal goal))
        {
            SetReward(+1f);
            EndEpisode();
        }
        if (other.gameObject.TryGetComponent<Floor>(out Floor floor))
        {
            SetReward(-1f);
            EndEpisode();
        }
    }
}