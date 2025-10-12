using UnityEngine;

[RequireComponent(typeof(PlatformWithMotors))]
public class Engine : MonoBehaviour
{
    [Header("Engine Settings")]
    public float drivePower = 30f;

    private PlatformWithMotors platform;

    [HideInInspector]
    public float inputThrust;

    void Awake()
    {
        platform = GetComponent<PlatformWithMotors>();
    }

    void Update()
    {

        inputThrust = 0f;
        if (Input.GetKey(KeyCode.W)) inputThrust += 1f;
        if (Input.GetKey(KeyCode.S)) inputThrust -= 1f;

        if (platform)
            platform.SetThrust(inputThrust * drivePower);
    }
}
