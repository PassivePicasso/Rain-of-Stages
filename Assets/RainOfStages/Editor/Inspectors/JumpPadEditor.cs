#if THUNDERKIT_CONFIGURED
#if UNITY_EDITOR
using RainOfStages.Behaviours;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PassivePicasso.RainOfStages.Editor
{
    [CustomEditor(typeof(JumpPad)), CanEditMultipleObjects]
    public class JumpPadEditor : UnityEditor.Editor
    {
        Vector3[] trajectoryPoints;
        Vector3 peak;
        float impactVelocity;
        float verticalImpactVelocity;
        Vector3 lastPosition;
        float lastTime;

        private void OnSceneGUI()
        {
            JumpPad jumpPad = (JumpPad)target;
            bool updateTrajectory = false;

            EditorGUI.BeginChangeCheck();
            Vector3 newTargetPosition = Handles.PositionHandle(jumpPad.destination, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(jumpPad, "Change JumpPad Target Position");
                jumpPad.destination = newTargetPosition;
                updateTrajectory = true;
            }
            if (updateTrajectory || lastPosition != jumpPad.transform.position || lastTime != jumpPad.time)
            {
                lastPosition = jumpPad.transform.position;
                lastTime = jumpPad.time;
                trajectoryPoints = jumpPad.Trajectory().ToArray();
                peak = trajectoryPoints.OrderBy(v => v.y).Last();
                var velocityPick = trajectoryPoints.Skip(trajectoryPoints.Length - 3).Take(2).ToArray();
                impactVelocity = (velocityPick[1] - velocityPick[0]).magnitude / Time.fixedDeltaTime;
                verticalImpactVelocity = Mathf.Abs((velocityPick[1].y - velocityPick[0].y) / Time.fixedDeltaTime);
            }

            Handles.DotHandleCap(0, peak, Quaternion.identity, 0.75F, EventType.Repaint);

            Handles.BeginGUI();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField($"Pad: {jumpPad.transform.position}");
            EditorGUILayout.LabelField($"Peak: {peak}");
            EditorGUILayout.LabelField($"Target: {newTargetPosition}");
            EditorGUILayout.LabelField($"Impact Velocity: {impactVelocity }");
            EditorGUILayout.LabelField($"Vertical Impact Velocity: {verticalImpactVelocity }");

            var impactDamage = CalculateCollisionDamage(verticalImpactVelocity);
            if (impactDamage > 0) GUI.contentColor = Color.red;

            EditorGUILayout.LabelField($"Impact Damage Base: {impactDamage}");

            EditorGUILayout.EndVertical();
            Handles.EndGUI();
        }

        private float CalculateCollisionDamage(float velocity)
        {
            float baseCapableVelocity = 28f;
            if ((double)velocity < (double)baseCapableVelocity)
                return 0;
            float damageMultiplier = (float)((double)velocity / (double)baseCapableVelocity * 0.0700000002980232);
            return Mathf.Min(131f, 131f * damageMultiplier);
        }
    }
}
#endif
#endif