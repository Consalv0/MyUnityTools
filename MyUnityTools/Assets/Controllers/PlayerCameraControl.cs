using UnityEngine;
using System.Linq;
using UtilityTools;
#if UNITY_EDITOR
using UnityEditor;
using UtilityToolsEditor;
#endif

[DisallowMultipleComponent]
public class PlayerCameraControl : MonoBehaviour {
	public Transform objective;
	public Vector3 objectiveOffset;
	public Transform objectiveBase;
	public Vector3 objectiveBaseOffset;
	[DisplayProperties] public InputHelper inputs;
	[HideInInspector] [SerializeField] bool _lockCursor;
	[HideInInspector] public bool lockToTransform;
	[HideInInspector] public float currentDistance = 5;
	[HideInInspector] public float zoomSpeed = 10;
	[HideInInspector] public float collisionMargin = 2;
	[HideInInspector] public LayerMask collisionMask;
	[HideInInspector] [SerializeField] float maxDistance = 10;
	[HideInInspector] [SerializeField] float minDistance = 0;
	[HideInInspector] [SerializeField] float movementSmoothness = 0.02f;
	[HideInInspector] [SerializeField] float maxPitch = 85;
	[HideInInspector] [SerializeField] float minPitch = -70;
	[HideInInspector] [SerializeField] Vector3 rotationSpeed = new Vector3(4, 3.5f, 1);
	[HideInInspector] [SerializeField] float pitch, yaw, roll;
	[HideInInspector] [SerializeField] float rotationSmoothness = 0.01f;

	public bool lockCursor {
		get { return _lockCursor; }
		set { SetCursorMode(value); }
	}

	Vector3 rotateSmoothVelocity;
	Vector3 currentRotation;
	Vector3 moveSmoothVelocity;
	Vector3 currentPosition;
	Vector3 targetPosition;
	Vector3 basePosition;
	Vector3 toCamera;
	float collisionDistance;
	RaycastHit rayHit;
	float t;

	protected void Awake() {
		lockCursor = _lockCursor;
		if (FindObjectOfType<InputHelper>()) {
			inputs = FindObjectOfType<InputHelper>();
		}
		if (GameObject.FindWithTag("Player")) {
			objective = !objective ? GameObject.FindWithTag("Player").transform : objective;
		}
		if (objective != null) {
			transform.rotation = Quaternion.Euler(pitch, yaw, roll);
			transform.position = objective.position + objectiveOffset - transform.forward * currentDistance;
		} else {
			Debug.LogWarning("Objetive 'Transform' not found", this);
		}
		if (inputs == null) {
			Debug.LogWarning("Inputs 'InputHelper' not found", this);
		}
	}

	protected void FixedUpdate() {
		if (objective == null || inputs == null) return;
		// Inputs ~
		currentDistance += Input.GetAxis(inputs.scrollWheel) * zoomSpeed;
		currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
		yaw += Input.GetAxis(inputs.mouseHorizontal) * rotationSpeed.x;
		pitch -= Input.GetAxis(inputs.mouseVertical) * rotationSpeed.y;

		t = (currentDistance - minDistance) / (maxDistance - minDistance);
		targetPosition = objective.position;
		basePosition = objectiveBase != null ? objectiveBase.position : objective.position;
		basePosition += lockToTransform ? (objectiveBase != null ? objectiveBase.rotation : objective.rotation) * objectiveBaseOffset : objectiveBaseOffset;
		targetPosition += lockToTransform ? objective.rotation * objectiveOffset : objectiveOffset;
		targetPosition = Vector3.Lerp(targetPosition, basePosition, t);

		toCamera = transform.position - targetPosition; // vector to the camera
		toCamera = toCamera.magnitude < 0.0001f ? transform.forward : toCamera; // Fix objetive too close

		// Debug.DrawRay(targetPosition, toCamera.normalized * (currentDistance + collisionMargin), Color.red);
		if (Physics.Raycast(targetPosition, toCamera.normalized, out rayHit, currentDistance + collisionMargin, collisionMask)) {
			collisionDistance = (rayHit.point - targetPosition).magnitude - collisionMargin;
			collisionDistance = collisionDistance < 0 ? 0 : collisionDistance;
		} else {
			collisionDistance = currentDistance;
		}

		if (maxPitch < 180 && minPitch > -180) pitch = Mathf.Clamp(pitch, minPitch, maxPitch); // Clamp the angle
		if (Input.GetAxis(inputs.mouseHorizontal).Equals(0)) {
			if (InputHelper.inactiveTime > 60) {  //Reset the angle number when inactive to avoid jiggering
				yaw += 360; yaw %= 360; yaw -= yaw > 180 ? 360 : 0;
				currentRotation.y = yaw;
			}
		} else {
			InputHelper.inactiveTime = 0;
		}

		currentRotation = Vector3.SmoothDamp(currentRotation, new Vector3(pitch, yaw), ref rotateSmoothVelocity, movementSmoothness);
		transform.eulerAngles = currentRotation;

		currentPosition = Vector3.SmoothDamp(currentPosition, targetPosition - transform.forward * collisionDistance, ref moveSmoothVelocity, rotationSmoothness);
		transform.position = currentPosition;
	}


	void SetCursorMode(bool value) {
		_lockCursor = value;
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
#if UNITY_EDITOR
		if (EditorApplication.isPlaying) {
#endif
			Cursor.lockState = _lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
#if UNITY_EDITOR
		}
#endif
	}
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(PlayerCameraControl))]
public class PlayerCameraControlEditor : Editor {
	SerializedProperty objective, objectiveOffset;
	SerializedProperty _lockCursor, lockToTransform;
	SerializedProperty currentDistance, collisionMargin, collisionMask, maxDistance, minDistance;
	SerializedProperty maxPitch, minPitch, rotationSpeed, roll, yaw, pitch;
	SerializedProperty zoomSpeed, rotationSmoothness, movementSmoothness;
	//SerializedProperty inputs;

	PlayerCameraControl serializedTarget;

	void OnEnable() {
		objective = serializedObject.FindProperty("objective");
		objectiveOffset = serializedObject.FindProperty("objectiveOffset");
		_lockCursor = serializedObject.FindProperty("_lockCursor");
		lockToTransform = serializedObject.FindProperty("lockToTransform");
		currentDistance = serializedObject.FindProperty("currentDistance");
		collisionMargin = serializedObject.FindProperty("collisionMargin");
		collisionMask = serializedObject.FindProperty("collisionMask");
		maxDistance = serializedObject.FindProperty("maxDistance");
		minDistance = serializedObject.FindProperty("minDistance");
		movementSmoothness = serializedObject.FindProperty("movementSmoothness");
		maxPitch = serializedObject.FindProperty("maxPitch");
		minPitch = serializedObject.FindProperty("minPitch");
		rotationSpeed = serializedObject.FindProperty("rotationSpeed");
		roll = serializedObject.FindProperty("roll");
		yaw = serializedObject.FindProperty("yaw");
		pitch = serializedObject.FindProperty("pitch");
		zoomSpeed = serializedObject.FindProperty("zoomSpeed");
		rotationSmoothness = serializedObject.FindProperty("rotationSmoothness");
		serializedTarget = serializedObject.targetObject as PlayerCameraControl;
	}

	string testGUISkin = "";
	public override void OnInspectorGUI() {
		DrawDefaultInspector();

		GUILayout.BeginHorizontal();
		EditorGUILayout.PropertyField(_lockCursor, new GUIContent("Lock Cursor"));
		if (GUI.changed) {
			serializedTarget.lockCursor = _lockCursor.boolValue;
		}
		EditorGUILayout.PropertyField(lockToTransform);
		GUILayout.EndHorizontal();
		EditorTool.AddSpecialSpace(0);
		EditorGUILayout.LabelField("Movement Settings", new GUIStyle("BoldLabel"));
		EditorGUI.indentLevel++;
		EditorTool.MakeMinMaxSlider(minDistance, maxDistance, 0, 100, "Distance Clamp");
		EditorGUILayout.Slider(currentDistance, minDistance.floatValue, maxDistance.floatValue);
		EditorGUILayout.Slider(collisionMargin, -(maxDistance.floatValue - minDistance.floatValue) / 2 , (maxDistance.floatValue - minDistance.floatValue) / 2);
		EditorGUILayout.PropertyField(collisionMask);
		EditorGUILayout.Slider(zoomSpeed, -25, 25);
		EditorGUILayout.Slider(movementSmoothness, 0f, 1, "Smoothness");
		EditorGUI.indentLevel--;
		EditorTool.AddSpecialSpace(0);
		EditorGUILayout.LabelField("Rotation Settings", new GUIStyle("BoldLabel"));
		Vector3 rotations = new Vector3(pitch.floatValue, yaw.floatValue, roll.floatValue);
		Rect buttonRect = GUILayoutUtility.GetLastRect();
		buttonRect.x += EditorGUIUtility.labelWidth; buttonRect.width -= EditorGUIUtility.labelWidth;
		if (GUI.Button(buttonRect, "Calculate Initial Rotation")) {
			if (objective.objectReferenceValue != null) {
				var objectiveTransform = objective.objectReferenceValue as Transform;
				var rotation = serializedTarget.transform.rotation;
				serializedTarget.transform.LookAt(objectiveTransform.position + objectiveOffset.vector3Value);
				rotations = serializedTarget.transform.rotation.eulerAngles;
				serializedTarget.transform.rotation = rotation;
				pitch.floatValue = rotations.x; yaw.floatValue = rotations.y; roll.floatValue = rotations.z;
			}
		}
		EditorGUI.indentLevel++;
		EditorTool.MakeMinMaxSlider(minPitch, maxPitch, -180, 180, "Pitch Clamp");
		rotationSpeed.vector3Value = EditorGUILayout.Vector3Field(rotationSpeed.displayName, rotationSpeed.vector3Value);
		rotations = EditorGUILayout.Vector3Field("Rotations", rotations);
		if (GUI.changed) {
			pitch.floatValue = rotations.x; yaw.floatValue = rotations.y; roll.floatValue = rotations.z;
		}
		EditorGUILayout.Slider(rotationSmoothness, 0f, 0.5f, "Smoothness");
		GUI.enabled = objective.objectReferenceValue;
		GUI.enabled = true;
		EditorGUI.indentLevel--;

		serializedObject.ApplyModifiedProperties();
	}
}
#endif
