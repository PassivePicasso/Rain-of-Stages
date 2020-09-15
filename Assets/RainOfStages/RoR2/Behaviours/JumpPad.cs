#if THUNDERKIT_CONFIGURED
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RainOfStages.Behaviours
{
    [RequireComponent(typeof(Collider))]
    public class JumpPad : MonoBehaviour
    {
        [SerializeField]
        public Vector3 jumpVelocity;

        public float time;

        [SerializeField, HideInInspector]
        public Vector3 destination;
        private Vector3 origin => transform.position;

        public string jumpSoundString;

        public void OnTriggerEnter(Collider other)
        {
            RoR2.CharacterMotor motor = other.GetComponent<CharacterMotor>();
            if (!motor || !motor.hasEffectiveAuthority) return;

            if (!motor.disableAirControlUntilCollision)
            {
                _ = Util.PlaySound(jumpSoundString, gameObject);
            }

            motor.disableAirControlUntilCollision = true;
            motor.velocity = GetVelocity(time);
            motor.Motor.ForceUnground();
        }

        void OnDrawGizmos()
        {
            //DrawArc();
            Gizmos.color = Color.white;

            foreach (var point in Trajectory())
                Gizmos.DrawSphere(point, 0.5f);

            var upPoint = transform.position + Vector3.up;
            var plane = new Plane(transform.position, upPoint, destination);

            var planarTargetPosition = new Vector3(destination.x, origin.y, destination.z);
            transform.forward = planarTargetPosition - origin;

            Gizmos.DrawSphere(destination, 3);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(origin, origin + jumpVelocity);
            Gizmos.color = Color.white;
        }

        public IEnumerable<Vector3> Trajectory()
        {
            var to = transform.position;
            var tf = time * 1.75f;
            var velocity = GetVelocity(tf);
            var timeStep = Time.fixedDeltaTime;
            for (float f = tf; f > 0; f -= timeStep)
            {
                var from = to;
                var delta = velocity * timeStep;
                to = from + delta;

                var ray = new Ray(from, velocity.normalized);
                var impact = Physics.Raycast(ray, out RaycastHit hit, delta.magnitude * timeStep);
                velocity += Physics.gravity * timeStep;

                yield return impact ? hit.point : to;
            }
        }

        public Vector3 GetVelocity(float time)
        {
            var (displacement3d, displacementXZ, direction) = LoadVariables();

            float planarVelocity = RoR2.Trajectory.CalculateGroundSpeed(time, displacementXZ.magnitude);
            float verticalVelocity = RoR2.Trajectory.CalculateInitialYSpeed(time, displacement3d.y);

            return new Vector3(direction.x * planarVelocity, verticalVelocity, direction.z * planarVelocity);
        }

        (Vector3 offset, Vector3 planarOffset, Vector3 normalPlanarOffset) LoadVariables()
        {
            var displacement3d = destination - origin;
            var displacementXZ = Vector3.ProjectOnPlane(displacement3d, Vector3.up);
            var directionNormalized = displacementXZ.normalized;

            return (displacement3d, displacementXZ, directionNormalized);
        }
    }
}
#endif
