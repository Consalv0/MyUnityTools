using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class OldThirdPersonCamera : MonoBehaviour {
	public Transform target;
	public Vector3 targetOffset;
	[SerializeField] float zoomSpeed = 10;
	[SerializeField] float maxDistance = 20; // Posible Distances form the target to the camera
	[SerializeField] float minDistance = 5;
	public float distance; // The actual distance
	float collisionDistance;	// Distance collide if there's a wall
	Vector3 vectorToCam;

	public float moveSmoothTime = 0.06f; // Move smooth factor
	Vector3 moveSmoothVelocity;	
	Vector3 currentPosition;

	public Vector2 rotationSpeed = new Vector2(10, 5); // Rotation max speed
	[SerializeField] float rotateSmoothTime = 0.12f; // Rotation smooth factor
	Vector3 rotateSmoothVelocity;
	Vector3 currentRotation;
	[SerializeField] float maxPitch = 85; // Range of angles in the X axis
	[SerializeField] float minPitch = -40;
	float yaw;
	float pitch;

	void OnEnable() {
  	distance = minDistance;
	}

	void LateUpdate() {
		/* If there's a target, you can move the camera */
		if (target) {
			/* Measure the distance between the target and the camera, then cas a Ray and if there's collision measure 
			 * the distance and modify the current distance acordly to the MaxMin limits, otherwise only take the distance and clamp them*/
			var targetPos = (target.position + targetOffset);
			vectorToCam = transform.position - targetPos;
			RaycastHit hit;
			Debug.DrawRay(targetPos, vectorToCam.normalized * collisionDistance, Color.red);
			if (Physics.Raycast(targetPos, vectorToCam.normalized, out hit, distance, 1 << LayerMask.NameToLayer("Terrain"))) {
				collisionDistance = (hit.point - targetPos).magnitude - 0.5f;
				collisionDistance = collisionDistance < 0 ? 0 : collisionDistance;
			} else {
				collisionDistance = distance;
			}
			distance += Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
			distance = Mathf.Clamp(distance, minDistance, maxDistance);

			/* Get the inputs and rotate with the given directions, the rotation is clamped and the aplied the rotation with smoothing,
			 * then move the camera in the direction between the camera and the target multipled by the previusly calculated direction */

#if UNITY_STANDALONE_WIN
			yaw += (Input.GetAxis("Right Horizontal") + Input.GetAxis("Mouse Horizontal")) * rotationSpeed.x;
			pitch -= (Input.GetAxis("Right Vertical") + Input.GetAxis("Mouse Vertical")) * rotationSpeed.y;
#else
			yaw += (Input.GetAxis("MacRight Horizontal") + Input.GetAxis("Mouse Horizontal")) * rotationSpeed.x;
			pitch -= (Input.GetAxis("MacRight Vertical") + Input.GetAxis("Mouse Vertical")) * rotationSpeed.y;
#endif
			pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

			currentPosition = Vector3.SmoothDamp(currentPosition, targetPos - transform.forward * collisionDistance, ref moveSmoothVelocity, moveSmoothTime);
			transform.position = currentPosition;
			
			currentRotation = Vector3.SmoothDamp(currentRotation, new Vector3(pitch, yaw), ref rotateSmoothVelocity, rotateSmoothTime);
			transform.eulerAngles = currentRotation;
		}
  }
}

/* Make a Custom GUI Editor Layout, the limits are clamped here */
#if UNITY_EDITOR
[CustomEditor(typeof(OldThirdPersonCamera))]
public class ThirdPersonCameraEditor : Editor {
	SerializedProperty zoomSpeed;
	SerializedProperty maxDistance;
	SerializedProperty minDistance;
	float maxDistanceLimit = 100; // Limit of the posible distances
	float minDistanceLimit = 0.01f;
	SerializedProperty rotateSmoothTime;
	SerializedProperty maxPitch;
	SerializedProperty minPitch;
	float maxPitchLimit = 180; // Limit of the posiible pitch
	float minPitchLimit = -180;

	OldThirdPersonCamera script;

	void OnEnable() {
		script = serializedObject.targetObject as OldThirdPersonCamera;
		zoomSpeed = serializedObject.FindProperty("zoomSpeed");
		maxDistance = serializedObject.FindProperty("maxDistance");
		minDistance = serializedObject.FindProperty("minDistance");
		rotateSmoothTime = serializedObject.FindProperty("rotateSmoothTime");
		maxPitch = serializedObject.FindProperty("maxPitch");
		minPitch = serializedObject.FindProperty("minPitch");
	}

	public override void OnInspectorGUI() {
		script.target = EditorGUILayout.ObjectField("Target", script.target, typeof(Transform), true) as Transform;
		script.targetOffset = EditorGUILayout.Vector3Field("Target Offset", script.targetOffset);
		script.moveSmoothTime = EditorGUILayout.Slider("Move Smoothing", script.moveSmoothTime, 0.001f, 0.1f);

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Zoom", EditorStyles.boldLabel);
		zoomSpeed.floatValue = EditorGUILayout.Slider("Zoom Speed", zoomSpeed.floatValue, 0, 25);
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Camera Dist", GUILayout.MaxWidth(100.0f), GUILayout.MinWidth(12.0f));
		GUILayout.FlexibleSpace();
		EditorGUIUtility.labelWidth = 32;
		minDistance.floatValue = EditorGUILayout.FloatField("Min:", minDistance.floatValue, GUILayout.MinWidth(62));
		GUILayout.FlexibleSpace();
		maxDistance.floatValue = EditorGUILayout.FloatField("Max:", maxDistance.floatValue, GUILayout.MinWidth(62));
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.BeginHorizontal();

		var _minDistance = minDistance.floatValue;
		var _maxDistance = maxDistance.floatValue;
		EditorGUIUtility.labelWidth = 1;
		EditorGUILayout.LabelField(minDistanceLimit.ToString(), GUILayout.MaxWidth(30));
		EditorGUILayout.MinMaxSlider(ref _minDistance, ref _maxDistance, minDistanceLimit, maxDistanceLimit);
		EditorGUIUtility.labelWidth = 1;
		EditorGUILayout.LabelField(maxDistanceLimit.ToString(), GUILayout.MaxWidth(30));
		EditorGUIUtility.labelWidth = 0;
		EditorGUILayout.EndHorizontal();
		minDistance.floatValue = _minDistance;
		maxDistance.floatValue = _maxDistance;

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Rotation", EditorStyles.boldLabel);
		script.rotationSpeed = EditorGUILayout.Vector2Field("Rotation Speed", script.rotationSpeed);
		rotateSmoothTime.floatValue = EditorGUILayout.Slider("Rotation Smoothing", rotateSmoothTime.floatValue, 0, 0.8f);
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Camera Angle", GUILayout.MaxWidth(100.0f), GUILayout.MinWidth(12.0f));
		GUILayout.FlexibleSpace();
		EditorGUIUtility.labelWidth = 32;
		minPitch.floatValue = EditorGUILayout.FloatField("Min:", minPitch.floatValue, GUILayout.MinWidth(62));
		GUILayout.FlexibleSpace();
		maxPitch.floatValue = EditorGUILayout.FloatField("Max:", maxPitch.floatValue, GUILayout.MinWidth(62));
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.BeginHorizontal();

		var _minPitch = minPitch.floatValue;
		var _maxPitch = maxPitch.floatValue;
		EditorGUIUtility.labelWidth = 1;
		EditorGUILayout.LabelField(minPitchLimit.ToString(), GUILayout.MaxWidth(30));
		EditorGUILayout.MinMaxSlider(ref _minPitch, ref _maxPitch, minPitchLimit, maxPitchLimit);
		EditorGUIUtility.labelWidth = 1;
		EditorGUILayout.LabelField(maxPitchLimit.ToString(), GUILayout.MaxWidth(30));
		EditorGUIUtility.labelWidth = 0;
		EditorGUILayout.EndHorizontal();
		minPitch.floatValue = _minPitch;
		maxPitch.floatValue = _maxPitch;

		serializedObject.ApplyModifiedProperties();
	}
}
#endif
