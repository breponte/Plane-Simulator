using UnityEngine;
using UnityEngine.InputSystem;

public class PlaneController : MonoBehaviour
{
    public InputActionAsset InputActions;

    private InputAction m_rollAction;
    private InputAction m_pitchAction;
    private InputAction m_yawAction;
    private InputAction m_throttleAction;

    [Header("Plane Stats")]
    [Tooltip("How much the throttle ramps up or down.")]
    public float throttleIncrement = 0.5f;
    [Tooltip("Maximum engine thrust when at 100% throttle.")]
    public float maxThrottle = 100f;
    [Tooltip("How responsive the plane is when rolling, pitching, and yawing.")]
    public float responsiveness = 10f;
    [Tooltip("How much lift force this plane generates as it gains speed.")]
    public float lift = 135f;

    private float throttle;
    private float roll;
    private float pitch;
    private float yaw;

    private float responseModifier
    {
        get {  
            return (rb.mass / 10f) * responsiveness;
        }
    }

    Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        m_rollAction = InputSystem.actions.FindAction("Roll");
        m_pitchAction = InputSystem.actions.FindAction("Pitch");
        m_yawAction = InputSystem.actions.FindAction("Yaw");
        m_throttleAction = InputSystem.actions.FindAction("Throttle");
    }

    private void HandleInputs()
    {
        // set rotational values from our axis inputs
        roll = m_rollAction.ReadValue<float>();
        pitch = m_pitchAction.ReadValue<float>();
        yaw = m_yawAction.ReadValue<float>();
        if (m_throttleAction.ReadValue<float>() > 0) throttle += throttleIncrement;
        else if (m_throttleAction.ReadValue<float>() < 0) throttle -= throttleIncrement;
        throttle = Mathf.Clamp(throttle, 0f, 100f);
    }

    private void Update()
    {
        HandleInputs();
    }

    private void FixedUpdate()
    {
        // apply forces to our plane (rotation of plane is scrambled)
        rb.AddForce(transform.forward * maxThrottle * throttle);
        rb.AddTorque(transform.up * yaw * responseModifier);
        rb.AddTorque(transform.right * pitch * responseModifier);
        rb.AddTorque(transform.forward * roll * responseModifier);

        rb.AddForce(Vector3.up * rb.linearVelocity.magnitude * lift);
    }
}
