using UnityEngine;

namespace NekoLegends
{
    public class SwayAnimator : MonoBehaviour
    {
        public enum Axis { X, Y, Z, Custom }
        public enum AxisSpace { Local, World } // World == parent space for localRotation

        [Header("Sway Settings")]
        public bool _isEnabled = false;
        [Tooltip("Cycles per second")]
        public float speed = 0.2f;
        [Tooltip("Sway range in degrees")]
        public float range = 20f;
        [Tooltip("Legacy fixed X angle when not preserving base")]
        public float _angle = 0f;

        [Header("Advanced")]
        [Tooltip("Which axis to sway around")]
        public Axis swayAxis = Axis.Y;
        [Tooltip("Apply sway around object's Local axes (old behavior) or World/Parent axes")]
        public AxisSpace axisSpace = AxisSpace.Local;
        [Tooltip("Additive around cached base rotation instead of overwriting")]
        public bool preserveBaseRotation = false;
        [Tooltip("When disabled, return to cached base if preserving")]
        public bool resetToBaseOnDisable = true;
        [Tooltip("Used only if Axis = Custom")]
        public Vector3 customAxis = Vector3.up;

        [Header("Base Caching")]
        [Tooltip("Cache base rotation once at Awake (recommended when objects are toggled active/inactive).")]
        public bool cacheBaseOnceAtAwake = true;

        private float localTime = 0f;
        private bool firstEnable = true;
        private Quaternion _baseLocalRot;
        private bool _baseCached = false;

        void Awake()
        {
            if (cacheBaseOnceAtAwake)
            {
                _baseLocalRot = transform.localRotation;
                _baseCached = true;
            }
        }

        void OnEnable()
        {
            // Only cache here if we didn't already cache at Awake (or you turned that off)
            if (!_baseCached)
            {
                _baseLocalRot = transform.localRotation;
                _baseCached = true;
            }

            firstEnable = true;
        }

        void OnDisable()
        {
            // Optional: snap back to base when turning this object off
            if (preserveBaseRotation && resetToBaseOnDisable && _baseCached)
                transform.localRotation = _baseLocalRot;
        }

        void Update()
        {
            if (_isEnabled)
            {
                if (firstEnable)
                {
                    localTime = 0f;
                    firstEnable = false;
                    // NOTE: do NOT recache base here, to avoid capturing mid-sway as new base
                }

                localTime += Time.deltaTime;
                float swayAngle = Mathf.Sin(localTime * speed * Mathf.PI * 2f) * range;

                if (preserveBaseRotation)
                {
                    // ADDITIVE (preserve) MODE
                    if (axisSpace == AxisSpace.Local)
                    {
                        Vector3 ax = (swayAxis == Axis.Custom) ? SafeAxis(customAxis) : AxisVectorLocal(swayAxis);
                        Quaternion delta = Quaternion.AngleAxis(swayAngle, ax);
                        transform.localRotation = _baseLocalRot * delta;   // rotate around base's LOCAL axis
                    }
                    else // World/Parent space
                    {
                        Vector3 ax;
                        if (swayAxis == Axis.Custom)
                        {
                            Vector3 local = SafeAxis(customAxis);
                            ax = transform.parent ? transform.parent.TransformDirection(local) : local;
                        }
                        else
                        {
                            ax = AxisVectorWorld(swayAxis);
                        }
                        Quaternion delta = Quaternion.AngleAxis(swayAngle, ax);
                        transform.localRotation = delta * _baseLocalRot;   // rotate around WORLD/PARENT axis
                    }
                }
                else
                {
                    // LEGACY (overwrite) MODE — preserves original behavior
                    switch (swayAxis)
                    {
                        case Axis.X:
                            transform.localRotation = Quaternion.Euler(swayAngle, 0f, 0f);
                            break;
                        case Axis.Y:
                            transform.localRotation = Quaternion.Euler(_angle, swayAngle, 0f);
                            break;
                        case Axis.Z:
                            transform.localRotation = Quaternion.Euler(_angle, 0f, swayAngle);
                            break;
                        case Axis.Custom:
                            {
                                Vector3 ax = SafeAxis(customAxis);
                                if (axisSpace == AxisSpace.Local)
                                    transform.localRotation = Quaternion.AngleAxis(swayAngle, ax);
                                else
                                    transform.localRotation = Quaternion.AngleAxis(swayAngle, ax) * Quaternion.Euler(_angle, 0f, 0f);
                            }
                            break;
                    }
                }
            }
            else
            {
                if (!firstEnable)
                {
                    if (preserveBaseRotation && resetToBaseOnDisable && _baseCached)
                        transform.localRotation = _baseLocalRot;
                    else
                        transform.localRotation = Quaternion.Euler(_angle, 0f, 0f); // legacy reset
                }
                firstEnable = true;
            }
        }

        private static Vector3 AxisVectorLocal(Axis a)
        {
            switch (a)
            {
                case Axis.X: return Vector3.right;   // local X
                case Axis.Y: return Vector3.up;      // local Y
                case Axis.Z: return Vector3.forward; // local Z
                default: return Vector3.up;
            }
        }

        private static Vector3 AxisVectorWorld(Axis a)
        {
            switch (a)
            {
                case Axis.X: return Vector3.right;   // world/parent X
                case Axis.Y: return Vector3.up;      // world/parent Y
                case Axis.Z: return Vector3.forward; // world/parent Z
                default: return Vector3.up;
            }
        }

        private static Vector3 SafeAxis(Vector3 v)
        {
            if (v.sqrMagnitude < 1e-8f) return Vector3.up;
            return v.normalized;
        }

        // Optional helpers if you ever need them from UI/buttons:
        [ContextMenu("Set Base To Current")]
        public void SetBaseToCurrent()
        {
            _baseLocalRot = transform.localRotation;
            _baseCached = true;
        }

        [ContextMenu("Reset To Base Now")]
        public void ResetToBaseNow()
        {
            if (_baseCached) transform.localRotation = _baseLocalRot;
        }
    }
}
