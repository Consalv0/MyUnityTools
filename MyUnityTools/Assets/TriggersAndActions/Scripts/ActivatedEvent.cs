using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UtilityTools;

// #if UNITY_EDITOR using UnityEditor; using UtilityToolsEditor;
// #endif 
public class ActivatedEvent {
	[SerializeField] [HideInInspector]
	List<ActivatedAction> actions = new List<ActivatedAction>();

	public void Invoke() {
		foreach (var action in actions) {
			action.Invoke();
		}
	}
}

// #if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ActivatedEvent), true)] public class ActivatedEventDrawer : PropertyDrawer {
	SerializedProperty actions;
	Rect body;
	Rect[] actionRects;

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
		FindPropertiesAndRects(new Rect(), property, label);
	}  	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
	}  	void FindPropertiesAndRects(Rect position, SerializedProperty property, GUIContent label) {

	}
}
// #endif