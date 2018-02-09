using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UtilityToolsEditor;
#endif

public class EventConditions : MonoBehaviour {
	public Condition[] conditions;
	public ActivatedEvent satisfiedEvent;
	public ActivatedEvent unsatisfiedEvent;
	[SerializeField] bool _constantCheck = false;
	[SerializeField] float _checkRate = 0.2f;

	bool isSatisfyed = false;
	int selectedCondition = 0;

	public bool constantCheck {
		get { return _constantCheck; }
		set {
			CancelInvoke();
			if (value) {
				InvokeRepeating("CheckCondition", 0, _checkRate);
			}
			_constantCheck = value;
		}
	}

	public float checkRate {
		get { return _checkRate; }
		set {
			CancelInvoke();
			if (value > 0) {
				InvokeRepeating("CheckCondition", 0, value);
			}
		}
	}

	void Awake() {
		if (_constantCheck) {
			InvokeRepeating("CheckCondition", 0, _checkRate);
		}
		if (isSatisfyed && satisfiedEvent != null)
		foreach (var action in satisfiedEvent.actions) {
			action.Initialize();
			}
		if (!isSatisfyed && unsatisfiedEvent != null)
		foreach (var action in unsatisfiedEvent.actions) {
			action.Initialize();
		}
	}

	// Check if is satified or not
	public bool CheckCondition() {
		for (int i = 0; i < conditions.Length; i++) {
			if (conditions[i].boolean == false) {
				unsatisfiedEvent.Invoke();
				return isSatisfyed = false;
			}
		}
		satisfiedEvent.Invoke();
		return isSatisfyed = true;
	}

	public void SelectCondition(string name) {
		for (int i = 0; i < conditions.Length; i++) {
			if (conditions[i].name.Equals(name, System.StringComparison.OrdinalIgnoreCase)) {
				selectedCondition = i;
				break;
			}
		}
	}
	public void SelectCondition(int index) {
		selectedCondition = Mathf.Clamp(index, 0, conditions.Length - 1);
	}
	public void SetCondition(bool value) {
		conditions[selectedCondition].boolean = value;
	}
	public void SetCondition(int index, bool value) {
		SelectCondition(index);
		SetCondition(value);
		if (!_constantCheck) {
			CheckCondition();
		}
	}
	public void SetCondition(string name, bool value) {
		SelectCondition(name);
		SetCondition(value);
		if (!_constantCheck) {
			CheckCondition();
		}
	}
	public void SwitchCondition(int index) {
		SelectCondition(index);
		conditions[selectedCondition].boolean = !conditions[selectedCondition].boolean;
		if (!_constantCheck) {
			CheckCondition();
		}
	}
	public void SwitchCondition(string name) {
		SelectCondition(name);
		conditions[selectedCondition].boolean = !conditions[selectedCondition].boolean;
		if (!_constantCheck) {
			CheckCondition();
		}
	}

	[System.Serializable]
	public struct Condition {
		public string name;
		public bool boolean;
	}
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(EventConditions.Condition))]
public class EventConditionDrawer : PropertyDrawer {
	SerializedProperty name, boolean;
	Rect nameRect, booleanRect;

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
		name = property.FindPropertyRelative("name");
		boolean = property.FindPropertyRelative("boolean");

		nameRect = position;
		nameRect.height = EditorGUIUtility.singleLineHeight;
		nameRect.x += 25;
		nameRect.width -= 25;
		booleanRect = position;
		booleanRect.width = 25;

		EditorGUI.PropertyField(booleanRect, boolean, GUIContent.none);
		EditorGUI.DelayedTextField(nameRect, name, GUIContent.none);
	}
}

[CustomEditor(typeof(EventConditions))]
[CanEditMultipleObjects]
public class EventConditionsEditor : Editor {
	SerializedProperty constantCheck, checkRate, conditions, satisfiedEvent, unsatisfiedEvent;
	ReorderableList conditionList;
	bool collapseConditions;

	void OnEnable() {
		constantCheck = serializedObject.FindProperty("_constantCheck");
		checkRate = serializedObject.FindProperty("_checkRate");
		conditions = serializedObject.FindProperty("conditions");
		satisfiedEvent = serializedObject.FindProperty("satisfiedEvent");
		unsatisfiedEvent = serializedObject.FindProperty("unsatisfiedEvent");

		conditionList = new ReorderableList(serializedObject, conditions, false, true, true, true) {
			drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
				rect.y += 2;
				rect.x += 30;
				rect.width -= 30;
				EditorGUI.PropertyField(rect, conditions.GetArrayElementAtIndex(index));
				if (GUI.changed) {
					conditions.GetArrayElementAtIndex(index).FindPropertyRelative("name").stringValue =
						conditions.GetArrayElementAtIndex(index).FindPropertyRelative("name").stringValue.Trim();
					if (conditions.GetArrayElementAtIndex(index).FindPropertyRelative("name").stringValue.Length == 0)
						serializedObject.Update();

					for (int j = 0; j < conditions.arraySize; j++) {
						if (index == j) continue;
						if (conditions.GetArrayElementAtIndex(index).FindPropertyRelative("name").stringValue
							== conditions.GetArrayElementAtIndex(j).FindPropertyRelative("name").stringValue) {
							serializedObject.Update();
						}
					}
				}
				rect.x -= 25;
				rect.width = 30;
				EditorGUI.LabelField(rect, index.ToString());
			},
			onAddCallback = (ReorderableList list) => {
				list.serializedProperty.InsertArrayElementAtIndex(list.count == 0 ? 0 : list.count - 1);
				SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(list.count - 1);
				element.FindPropertyRelative("name").stringValue = "Condition" + list.count;
				element.FindPropertyRelative("boolean").boolValue = false;
			},
			drawHeaderCallback = (Rect position) => {
				position.x += 30;
				position.width -= 30;
				position.x += EditorGUIUtility.labelWidth;
				position.width -= EditorGUIUtility.labelWidth;
				EditorGUI.LabelField(position, "Values");
				position.x -= 30 + EditorGUIUtility.labelWidth;
				position.width = 30;
				EditorGUI.LabelField(position, "Idx");
			}
		};
	}

	public override void OnInspectorGUI() {
		EditorGUI.BeginDisabledGroup(true);
		EditorGUILayout.ObjectField(new GUIContent("Script"), MonoScript.FromMonoBehaviour((EventConditions)target), typeof(EventConditions), true);
		EditorGUI.EndDisabledGroup();
		serializedObject.Update();
		EditorGUILayout.PropertyField(constantCheck);
		if (GUI.changed && EditorApplication.isPlaying) {
			serializedObject.ApplyModifiedProperties();
			(serializedObject.targetObject as EventConditions).constantCheck = constantCheck.boolValue;
			GUI.changed = false;
		}
		EditorGUILayout.DelayedFloatField(checkRate);
		if (GUI.changed && EditorApplication.isPlaying) {
			serializedObject.ApplyModifiedProperties();
			(serializedObject.targetObject as EventConditions).checkRate = checkRate.floatValue;
			GUI.changed = false;
		}
		GUILayout.Space(2);
		conditionList.DoLayoutList();
		EditorTool.AddSpecialSpace(0);
		EditorGUILayout.PropertyField(satisfiedEvent);
		EditorGUILayout.PropertyField(unsatisfiedEvent);
		serializedObject.ApplyModifiedProperties();
	}
}
#endif