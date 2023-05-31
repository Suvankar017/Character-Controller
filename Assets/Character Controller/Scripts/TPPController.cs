using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;

public class TPPController : MonoBehaviour
{
    [Header("Public Member's")]
    public float TiltPower = 1.0f;
    public float Gravity = -9.81f;
    public float WalkSpeed = 5.0f;
    public float RunSpeed = 10.0f;
    public bool IsGrounded = false;
    public bool IsTouchingWall = false;
    public bool IsFirstRayIntersecting = false;
    public bool IsSecondRayIntersecting = false;
    public bool IsThirdRayIntersecting = false;
    public Vector3 GroundCheckPos = Vector3.zero;
    public LayerMask GroundCheckLayer = 0;
    public float RotationSmoothTime = 0.1f;
    public float GroundCheckOffset = 0.1f;
    public Transform CameraFollowTransform;
    public Vector3 WallCheckPos = Vector3.zero;
    public float WallCheckRadius = 0.5f;
    public LayerMask WallCheckLayer = 0;
    public Vector3 WallCheckRayOffset = Vector3.zero;
    public float WallCheckRayLength = 0.2f;
    public float JumpHeight = 2.0f;
    public OverrideTransform OverrideTransform;

    [Header("Keyboard Input")]
    public Vector2 Look;
    public Vector2 Move;
    public bool Jump;
    public bool Sprint;

    [Header("Private Member's")]
    [SerializeField]
    private Vector3 _Velocity = Vector3.zero;
    [SerializeField]
    private float _TargetRotation = 0.0f;
    [SerializeField]
    private float _RotationVelocity = 0.0f;
    [SerializeField]
    private Vector3 _FirstNormal = Vector3.zero;
    [SerializeField]
    private Vector3 _SecondNormal = Vector3.zero;
    [SerializeField]
    private Vector3 _WallNormal = Vector3.zero;
    [SerializeField]
    private Vector3 _PrevVelocity = Vector3.zero;
    [SerializeField]
    private Vector3 _Accleration = Vector3.zero;
    [SerializeField]
    private float _Speed = 0.0f;

    private Vector2 _CameraRotation = Vector2.zero;
    private CharacterController _Controller = null;
    private Transform _CameraTransform = null;

    private void Awake()
    {
        _Controller = GetComponent<CharacterController>();
    }

    private void Start()
    {
        _CameraTransform = Camera.main.transform;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        IsGrounded = Physics.CheckSphere(transform.localPosition + GroundCheckPos, _Controller.radius, GroundCheckLayer);
        IsTouchingWall = Physics.CheckSphere(transform.localPosition + _Controller.center + WallCheckPos, WallCheckRadius, WallCheckLayer);

        if (Physics.Raycast(transform.localPosition + new Vector3(0.0f, _Controller.radius), Vector3.down, out RaycastHit hit1, _Controller.radius + GroundCheckOffset, GroundCheckLayer))
        {
            IsFirstRayIntersecting = true;
            _FirstNormal = hit1.normal;
        }
        else
        {
            IsFirstRayIntersecting = false;
            _FirstNormal = Vector3.up;
        }

        if (Physics.Raycast(transform.localPosition + new Vector3(0.0f, _Controller.radius) + (transform.forward * _Controller.radius), Vector3.down, out RaycastHit hit2, _Controller.radius + GroundCheckOffset, GroundCheckLayer))
        {
            IsSecondRayIntersecting = true;
            _SecondNormal = hit2.normal;
        }
        else
        {
            IsSecondRayIntersecting = false;
            _SecondNormal = Vector3.up;
        }

        if (IsGrounded)
        {
            float targetSpeed = Sprint ? RunSpeed : WalkSpeed;

            if (Move != Vector2.zero)
            {
                Vector3 moveDir = new Vector3(Move.x, 0.0f, Move.y).normalized;
                _TargetRotation = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg + _CameraTransform.localEulerAngles.y;
                float yRot = Mathf.SmoothDampAngle(transform.localEulerAngles.y, _TargetRotation, ref _RotationVelocity, RotationSmoothTime);
                transform.rotation = Quaternion.Euler(0.0f, yRot, 0.0f);
            }
            else
            {
                targetSpeed = 0.0f;
            }

            _Speed = Mathf.Lerp(_Speed, targetSpeed, Time.deltaTime);

            Vector3 targetDir = Quaternion.Euler(0.0f, _TargetRotation, 0.0f) * Vector3.forward;

            Vector3 targetDir1 = Vector3.ProjectOnPlane(targetDir, _FirstNormal.normalized);
            Vector3 targetDir2 = Vector3.ProjectOnPlane(targetDir, _SecondNormal.normalized);

            targetDir = (0.5f * (targetDir1 + targetDir2)).normalized;

            Debug.DrawRay(transform.localPosition, targetDir);

            _Velocity = targetDir * _Speed;

            if (Jump)
            {
                _Velocity.y += Mathf.Sign(-Gravity) * Mathf.Sqrt(2.0f * JumpHeight * Mathf.Abs(Gravity));
            }
        }
        else
        {
            Jump = false;
        }

        if (IsTouchingWall)
        {
            if (Physics.Raycast(transform.localPosition + WallCheckRayOffset, transform.forward, out RaycastHit hit3, _Controller.radius + WallCheckRayLength, WallCheckLayer))
            {
                IsThirdRayIntersecting = true;
                _WallNormal = hit3.normal;

                if (Vector3.Dot(-transform.forward, _WallNormal) > 0.9f)
                {
                    if (IsGrounded)
                    {
                        if (!Jump)
                            _Velocity = Vector3.zero;
                    }
                    else
                    {
                        _Velocity.x = _Velocity.z = 0.0f;
                    }
                }
                else
                {
                    if (IsGrounded)
                    {
                        float speed = _Velocity.magnitude;
                        Vector3 targetDir = Vector3.ProjectOnPlane(_Velocity, _WallNormal).normalized;
                        _Velocity = targetDir * speed * (1.0f - Vector3.Dot(-transform.forward, _WallNormal));
                    }
                    else
                    {
                        float speed = _Velocity.magnitude;
                        Vector3 targetDir = Vector3.ProjectOnPlane(_Velocity, _WallNormal).normalized;
                        _Velocity = targetDir * speed;
                    }
                }
            }
            else
            {
                IsThirdRayIntersecting = false;
            }
        } 

        _Velocity.y += Gravity * Time.deltaTime;
        _Controller.Move(_Velocity * Time.deltaTime);

        _Accleration = (_Velocity - _PrevVelocity) / Time.deltaTime;
        _PrevVelocity = _Controller.velocity;

        TiltToAccleration();
    }

    private Vector3 CalculateTilt(Vector3 acceleration)
    {
        acceleration.y = 0.0f;
        Vector3 tiltAxis = Vector3.Cross(acceleration, Vector3.up);
        float angle = Mathf.Clamp(-acceleration.magnitude, -10.0f, 10.0f);
        Quaternion targetRotation = Quaternion.AngleAxis(angle, tiltAxis) * transform.rotation;
        return targetRotation.eulerAngles;
    }

    private void TiltToAccleration()
    {
        ref OverrideTransformData data = ref OverrideTransform.data;

        Vector3 tilt = CalculateTilt(_Accleration);
        tilt.y = 0.0f;
        Quaternion currentRotation = Quaternion.Euler(data.rotation);
        Quaternion targetRotation = Quaternion.Euler(tilt);

        data.rotation = Quaternion.Slerp(currentRotation, targetRotation, 10.0f * Time.deltaTime).eulerAngles;
    }

    private void LateUpdate()
    {
        CameraRotation();
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;

        if (!enabled)
            return;

        Gizmos.color = (IsGrounded ? Color.green : Color.red) * 0.5f;
        Gizmos.DrawSphere(transform.localPosition + GroundCheckPos, _Controller.radius);

        Gizmos.color = (IsTouchingWall ? Color.green : Color.red) * 0.5f;
        Gizmos.DrawSphere(transform.localPosition + _Controller.center + WallCheckPos, WallCheckRadius);

        if (IsTouchingWall)
        {
            Gizmos.color = IsThirdRayIntersecting ? Color.green : Color.blue;
            Gizmos.DrawRay(transform.localPosition + WallCheckRayOffset, transform.forward * (_Controller.radius + WallCheckRayLength));
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.localPosition, _Velocity);

        Gizmos.color = IsFirstRayIntersecting ? Color.green : Color.red;
        Gizmos.DrawRay(transform.localPosition + new Vector3(0.0f, _Controller.radius), Vector3.down * (_Controller.radius + GroundCheckOffset));

        Gizmos.color = IsSecondRayIntersecting ? Color.green : Color.red;
        Gizmos.DrawRay(transform.localPosition + new Vector3(0.0f, _Controller.radius) + (transform.forward * _Controller.radius), Vector3.down * (_Controller.radius + GroundCheckOffset));

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.localPosition, _Accleration);
    }

    private void CameraRotation()
    {
        if (Look.sqrMagnitude > 0.01f)
        {
            _CameraRotation.x += Look.y;
            _CameraRotation.y += Look.x;
        }

        _CameraRotation.x = ClampAngle(_CameraRotation.x, -30.0f, 70.0f);
        _CameraRotation.y = ClampAngle(_CameraRotation.y, float.MinValue, float.MaxValue);

        CameraFollowTransform.rotation = Quaternion.Euler(_CameraRotation.x, _CameraRotation.y, 0.0f);
    }

    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360.0f) angle += 360.0f;
        else if (angle > 360.0f) angle -= 360.0f;
        return Mathf.Clamp(angle, min, max);
    }

    private void OnLook(InputValue value)
    {
        Look = value.Get<Vector2>();
    }

    private void OnMove(InputValue value)
    {
        Move = value.Get<Vector2>();
    }

    private void OnJump(InputValue value)
    {
        Jump = value.isPressed;
    }

    private void OnSprint(InputValue value)
    {
        Sprint = value.isPressed;
    }
}
