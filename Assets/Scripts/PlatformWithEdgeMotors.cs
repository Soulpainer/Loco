using UnityEngine;
using UnityEngine.Splines;

public class PlatformWithMotors : MonoBehaviour
{
    [Header("Motors")]
    public SplineMotor frontMotor;
    public SplineMotor backMotor;

    [Header("Movement")]
    public TrackSegment spline;
    private float drivePower = 1;//30f;
    public float drag = 2f;
    public float gravity = 9.81f;

    private float speed = 0f;
    private float input = 0f;

    public void SetThrust(float thrust)
    {
        input = thrust;
    }

    void Start()
    {
        if (spline == null)
        {
            Debug.LogError("SplineContainer не назначен!");
            return;
        }

        // Инициализация моторных точек с локальными оффсетами
        frontMotor.SetSegment(spline, frontMotor.transform.localPosition);
        backMotor.SetSegment(spline, backMotor.transform.localPosition);

        // Snap один раз на старте
        frontMotor.SnapToSegmentAtStart(transform);
        backMotor.SnapToSegmentAtStart(transform);
    }


    // void Update()
    // {
    //     float dt = Time.deltaTime;

    //     Vector3 forwardDir = (frontMotor.EvaluatePosition() - backMotor.EvaluatePosition()).normalized;
    //     float slope = Vector3.Dot(Vector3.down, forwardDir);

    //     // --- ускорение от ввода и гравитации ---
    //     float accel = input * drivePower + gravity * slope;
    //     speed += accel * dt;

    //     // --- экспоненциальное затухание скорости ---
    //     speed *= Mathf.Exp(-drag * dt);

    //     // --- двигаем моторы ---
    //     frontMotor.MoveAlongSegment(speed * dt);
    //     backMotor.MoveAlongSegment(speed * dt);

    //     // --- позиция и ориентация платформы ---
    //     Vector3 posFront = frontMotor.EvaluatePosition();
    //     Vector3 posBack = backMotor.EvaluatePosition();
    //     Vector3 up = (frontMotor.EvaluateUp() + backMotor.EvaluateUp()).normalized;

    //     transform.position = (posFront + posBack) * 0.5f;
    //     transform.rotation = Quaternion.LookRotation(forwardDir, up);
    // }
    void Update()
    {
        float dt = Time.deltaTime;

        Vector3 forwardDir = (frontMotor.EvaluatePosition() - backMotor.EvaluatePosition()).normalized;
        float slope = Vector3.Dot(Vector3.down, forwardDir);

        float accel = input * drivePower + gravity * slope;
        speed += accel * dt;
        speed *= Mathf.Exp(-drag * dt);

        frontMotor.MoveAlongSegment(speed * dt);
        backMotor.MoveAlongSegment(speed * dt);

        Vector3 posFront = frontMotor.EvaluatePosition();
        Vector3 posBack = backMotor.EvaluatePosition();
        Vector3 up = (frontMotor.EvaluateUp() + backMotor.EvaluateUp()).normalized;

        transform.position = (posFront + posBack) * 0.5f;
        transform.rotation = Quaternion.LookRotation(forwardDir, up);
    }
}
