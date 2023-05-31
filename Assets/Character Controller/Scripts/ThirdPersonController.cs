using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ThirdPersonCharacter
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(ThirdPersonInput))]
    [RequireComponent(typeof(ThirdPersonCamera))]
    public class ThirdPersonController : MonoBehaviour
    {

        [Serializable]
        public struct BooleanColor
        {
            public Color TrueColor;
            public Color FalseColor;

            public BooleanColor(Color trueColor, Color falseColor)
            {
                TrueColor = trueColor;
                FalseColor = falseColor;
            }
        }

        [Header("Visual Fields")]
        public float DeltaRotation;
        public bool IsAttacking;
        public bool CanPerformNextAttack;
        public Vector3 Accleration;
        public float AccelerationMagnitude;
        public Vector3 EulerAngles;

        [Header("Movement")]
        public float RunSpeed = 5.0f;
        public float SprintSpeed = 10.0f;
        public float SpeedChangeRate = 10.0f;
        public float RotationSmoothTime = 0.15f;
        public float JumpHeight = 2.0f;
        public float JumpTimeout = 0.05f;

        [Header("Physics")]
        public float Gravity = -9.81f;
        public float WallSlidingThreshold = 0.1f;
        public float MaxTiltAngle = 5.0f;
        public float TiltSmoothTime = 10.0f;
        public float FallTimeout = 0.1f;

        [Header("Checks")]
        public bool IsGrounded = false;
        public Vector3 GroundCheckPosition = Vector3.zero;
        public float GroundCheckRadius = 1.0f;
        public LayerMask GroundCheckLayer = 0;
        public bool IsTouchingWall = false;
        public Vector3 WallCheckPosition = Vector3.zero;
        public float WallCheckRadius = 1.0f;
        public float WallCheckRayLength = 0.3f;
        public LayerMask WallCheckLayer = 0;

        [Header("References")]
        public Animator Animator = null;
        public Transform COMTransform = null;

        [Header("Gizmos Settings")]
        public Color VelocityColor = Color.blue;
        public Color AcclerationColor = Color.magenta;
        public BooleanColor GroundCheckSphereColor = new BooleanColor(Color.green, Color.red);
        public BooleanColor WallCheckSphereColor = new BooleanColor(Color.green, Color.red);

        private float _AnimTargetSpeed;
        private float _AnimSpeedBlend;

        private List<Vector3> _MovementPoints = new List<Vector3>();
        private Quaternion _PrevRotation;
        private float _Speed;
        private float _TargetYRotation;
        private float _CurrentRotationYVelocity;
        private float _JumpTimeoutDelta;
        private float _FallTimeoutDelta;
        private Vector3[] _GroundNormals;
        private Vector3 _WallNormal;
        private Vector3 _Accleration;
        private Vector3 _Velocity;
        private Vector3 _PrevVelocity;
        private Transform _CameraTransform;
        private Transform _Transform;
        private CharacterController _Controller;
        private ThirdPersonInput _Input;

        private void Awake()
        {
            _Controller = GetComponent<CharacterController>();
            _Input = GetComponent<ThirdPersonInput>();
        }

        private void Start()
        {
            _CameraTransform = Camera.main.transform;
            _Transform = transform;
            _GroundNormals = new Vector3[2];

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            Accleration = _Accleration;
            AccelerationMagnitude = _Accleration.magnitude;

            DoChecks();

            Animator.SetBool("Grounded", IsGrounded);

            Movement();
            HandleWallMovement();
            HandleJump();
            AcclerationTilt();

            _Velocity.y += Gravity * Time.deltaTime;
            _Accleration = (_Velocity - _PrevVelocity) / Time.deltaTime;
            _PrevVelocity = _Velocity;

            _Controller.Move(_Velocity * Time.deltaTime);

            _AnimSpeedBlend = Mathf.Lerp(_AnimSpeedBlend, _AnimTargetSpeed, SpeedChangeRate * Time.deltaTime);
            if (_AnimSpeedBlend < 0.01f)
                _AnimSpeedBlend = 0.0f;

            Animator.SetFloat("Speed", _AnimSpeedBlend);

            DeltaRotation = Quaternion.Angle(_PrevRotation, _Transform.rotation);
            Debug.Log(DeltaRotation);
            _PrevRotation = _Transform.rotation;

            _MovementPoints.Add(_Transform.position);

            for (int i = 0; i < _MovementPoints.Count - 1; i++)
                Debug.DrawLine(_MovementPoints[i], _MovementPoints[i + 1], Color.red);
        }

        private void OnAttack(InputValue value)
        {
            if (!IsGrounded)
                return;

            if (!IsAttacking)
            {
                IsAttacking = true;
                Animator.SetBool("Attack", IsAttacking);
            }

            if (CanPerformNextAttack)
            {
                CanPerformNextAttack = false;
                Animator.SetBool("NextAttack", true);
            }
        }

        public void ResetAttacking()
        {
            IsAttacking = false;
            CanPerformNextAttack = false;
            Animator.SetBool("Attack", IsAttacking);
        }

        private void OnDrawGizmos()
        {
            if (!(Application.isPlaying && enabled))
                return;

            Gizmos.color = VelocityColor;
            Gizmos.DrawRay(_Transform.localPosition, _Velocity);

            Gizmos.color = AcclerationColor;
            Gizmos.DrawRay(_Transform.localPosition, _Accleration);

            Gizmos.color = IsGrounded ? GroundCheckSphereColor.TrueColor : GroundCheckSphereColor.FalseColor;
            Gizmos.DrawSphere(_Transform.localPosition + GroundCheckPosition, GroundCheckRadius);

            Gizmos.color = IsTouchingWall ? WallCheckSphereColor.TrueColor : WallCheckSphereColor.FalseColor;
            Gizmos.DrawSphere(_Transform.localPosition + _Controller.center + WallCheckPosition, WallCheckRadius);


        }

        private void OnDrawGizmosSelected()
        {
            if (Application.isPlaying || !enabled)
                return;

            Gizmos.color = IsGrounded ? GroundCheckSphereColor.TrueColor : GroundCheckSphereColor.FalseColor;
            Gizmos.DrawSphere(transform.localPosition + GroundCheckPosition, GroundCheckRadius);

            Gizmos.color = IsTouchingWall ? WallCheckSphereColor.TrueColor : WallCheckSphereColor.FalseColor;
            Gizmos.DrawSphere(transform.localPosition + GetComponent<CharacterController>().center + WallCheckPosition,
                WallCheckRadius);
        }

        private void DoChecks()
        {
            float controllerRadius = _Controller.radius;
            Vector3 groundRayOrigin = _Transform.localPosition + new Vector3(0.0f, controllerRadius);

            IsGrounded = Physics.CheckSphere(_Transform.localPosition + GroundCheckPosition, GroundCheckRadius,
                GroundCheckLayer, QueryTriggerInteraction.Ignore);

            IsTouchingWall = Physics.CheckSphere(_Transform.localPosition + _Controller.center + WallCheckPosition,
                WallCheckRadius, WallCheckLayer, QueryTriggerInteraction.Ignore);

            Physics.Raycast(groundRayOrigin, Vector3.down, out RaycastHit hit, Mathf.Infinity, GroundCheckLayer);
            _GroundNormals[0] = hit.normal;
            Physics.Raycast(groundRayOrigin + (_Transform.forward * controllerRadius), Vector3.down, out hit,
                Mathf.Infinity, GroundCheckLayer);
            _GroundNormals[1] = hit.normal;

            Physics.Raycast(_Transform.localPosition + _Controller.center, transform.forward, out hit,
                controllerRadius + WallCheckRayLength, WallCheckLayer);
            _WallNormal = hit.normal;
        }

        private void Movement()
        {
            if (!IsGrounded)
                return;

            float targetSpeed = _Input.IsSprint ? SprintSpeed : RunSpeed;
            if (_Input.Move == Vector2.zero)
                targetSpeed = 0.0f;

            if (_Speed < targetSpeed - 0.1f || _Speed > targetSpeed + 0.1f)
            {
                _Speed = Mathf.Lerp(_Speed, targetSpeed, SpeedChangeRate * Time.deltaTime);
                _Speed = Mathf.Round(_Speed * 1000.0f) * 0.001f;
            }
            else
            {
                _Speed = targetSpeed;
            }

            _AnimTargetSpeed = targetSpeed;

            if (_Input.Move != Vector2.zero)
            {
                Vector2 moveDir = _Input.Move.normalized;
                _TargetYRotation = Mathf.Atan2(moveDir.x, moveDir.y) * Mathf.Rad2Deg + _CameraTransform.localEulerAngles.y;
                float y = Mathf.SmoothDampAngle(_Transform.localEulerAngles.y, _TargetYRotation,
                    ref _CurrentRotationYVelocity, RotationSmoothTime * Time.deltaTime);
                _Transform.localRotation = Quaternion.Euler(0.0f, y, 0.0f);
            }

            Vector3 targetDir = Quaternion.Euler(0.0f, _TargetYRotation, 0.0f) * Vector3.forward;

            Vector3 movementDir = Vector3.zero;
            foreach (Vector3 normal in _GroundNormals)
                movementDir += Vector3.ProjectOnPlane(targetDir, normal);
            movementDir = (movementDir / _GroundNormals.Length).normalized;

            _Velocity = movementDir * _Speed;
        }

        private void HandleWallMovement()
        {
            if (!IsTouchingWall)
                return;

            float dot = 1.0f - Vector3.Dot(_Transform.forward, -_WallNormal);
            bool canWallSlide = dot > WallSlidingThreshold;

            if (canWallSlide)
            {
                float slidingResistance = IsGrounded ? dot : 1.0f;
                Vector3 movementDir = Vector3.ProjectOnPlane(_Velocity, _WallNormal).normalized;
                _Velocity = _Speed * slidingResistance * movementDir;

                _AnimTargetSpeed = _Speed * slidingResistance;
            }
            else
            {
                _Velocity.x = _Velocity.z = 0.0f;
                _Velocity.y = IsGrounded ? 0.0f : _Velocity.y;

                _AnimTargetSpeed = 0.0f;
            }
        }

        private void HandleJump()
        {
            if (IsGrounded)
            {
                _FallTimeoutDelta = FallTimeout;

                Animator.SetBool("Jump", false);
                Animator.SetBool("Fall", false);

                if (_Input.IsJump && _JumpTimeoutDelta <= 0.0f)
                {
                    if (!IsTouchingWall)
                    {
                        float jumpVelocity = Mathf.Sign(-Gravity) * Mathf.Sqrt(2.0f * JumpHeight * Mathf.Abs(Gravity));

                        if (_Velocity.y > 0.0f)
                            _Velocity.y += jumpVelocity;
                        else
                            _Velocity.y = jumpVelocity;

                        Animator.SetBool("Jump", true);
                    }
                    else
                        _Input.IsJump = false;
                }

                if (_JumpTimeoutDelta >= 0.0f)
                    _JumpTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                if (_FallTimeoutDelta > 0.0f)
                {
                    _FallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    if (_Velocity.y < 0.0f)
                        Animator.SetBool("Fall", true);
                }
                
                _JumpTimeoutDelta = JumpTimeout;
                _Input.IsJump = false;
            }
        }

        private Quaternion CalculateTilt(Vector3 acceleration)
        {
            acceleration.y = 0.0f;
            Vector3 tiltAxis = Vector3.Cross(acceleration, Vector3.up);
            float angle = Mathf.Clamp(-acceleration.magnitude, -MaxTiltAngle, MaxTiltAngle);
            Quaternion targetRotation = Quaternion.AngleAxis(angle, tiltAxis) * _Transform.rotation;
            return targetRotation;
        }

        private void AcclerationTilt()
        {
            Quaternion targetRotation = CalculateTilt(_Accleration);
            COMTransform.rotation = Quaternion.Slerp(COMTransform.rotation, targetRotation, TiltSmoothTime * Time.deltaTime);
        }

    }
}
