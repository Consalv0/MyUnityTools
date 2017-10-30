using UnityEngine;
using System.Collections.Generic;
 #if UNITY_EDITOR using UnityEditor;
using UnityEditorInternal;
#endif 
[System.Serializable]
public class ActivatedEvent {
	public List<ActivatedAction> actions = new List<ActivatedAction>();

	public void Invoke() {
		foreach (var action in actions) {
			action.Invoke();
		}
	}
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ActivatedEvent), true)] public class ActivatedEventDrawer : PropertyDrawer {
	ReorderableList actionsList;
	SerializedProperty actions;
	bool isEnabled = false;

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
		OnEnable(property);
		float height = 0;
		if (actions.arraySize == 0) height += EditorGUIUtility.singleLineHeight + 6;
		for (int i = 0; i < actions.arraySize; i++) {
			height += EditorGUI.GetPropertyHeight(actions.GetArrayElementAtIndex(i));
		}
		height += EditorGUIUtility.singleLineHeight * 2 + 6;
		return height;
	}  	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
		EditorGUI.BeginProperty(position, label, property);
		actionsList.DoList(position);
		EditorGUI.EndProperty();
	}

	void OnEnable(SerializedProperty property) {
		if (!isEnabled) {
			actions = property.FindPropertyRelative("actions");
			actionsList = new ReorderableList(property.serializedObject, actions, true, true, true, true) {
				drawHeaderCallback = (Rect rect) => {
					EditorGUI.LabelField(rect, property.displayName + "()");
				},

				elementHeightCallback = (int index) => {
					return EditorGUI.GetPropertyHeight(actions.GetArrayElementAtIndex(index));
				},

				drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
				EditorGUI.PropertyField(rect, actions.GetArrayElementAtIndex(index), new GUIContent("Action[" + index + "]"));
				},
				onAddCallback = (ReorderableList list) => {
					SerializedProperty element;
					if (list.count == 0) {
						list.serializedProperty.InsertArrayElementAtIndex(list.count);
						element = list.serializedProperty.GetArrayElementAtIndex(list.count - 1);
						element.FindPropertyRelative("callMode").enumValueIndex = 2;
					} else {
						list.serializedProperty.InsertArrayElementAtIndex(list.count - 1);
						element = list.serializedProperty.GetArrayElementAtIndex(list.count - 1);
					}

					var parameters = element.FindPropertyRelative("parameters");
					for (int i = parameters.arraySize; i < 3; i++) {
						parameters.InsertArrayElementAtIndex(0);
					}
				}
			};
			isEnabled = true;
		}
	}
}
#endif