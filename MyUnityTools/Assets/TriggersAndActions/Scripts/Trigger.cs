using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UtilityTools;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

public class Trigger : MonoBehaviour {
	public ActivatedAction test;
	[HideInInspector]
	public List<ActivatedActionOld> links = new List<ActivatedActionOld>();

	List<Action> actions = new List<Action>(0);
	BindingFlags flagsToSearch = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	void Update() {
		if (Input.GetMouseButton(0)) {
			Pull();
		}
	}

	protected void Awake() {
		List<MethodInfo> methodsInfo = new List<MethodInfo> {
			GeneralTool.GetMethodInfo(this, "OnPull", flagsToSearch),
			GeneralTool.GetMethodInfo(this, "OnCallLinks", flagsToSearch),
			GeneralTool.GetMethodInfo(this, "OnRelease", flagsToSearch)
		};
		for (int i = 0; i < methodsInfo.Count(); i++) {
			if (methodsInfo[i] != null) {
				actions.Add((Action)Delegate.CreateDelegate(typeof(Action), this, methodsInfo[i]));
			} else {
				actions.Add(null);
			}
		}
		foreach (var link in links) {
			link.UpdateMethods();
		}
	}

	/// <summary>
	/// Pull this trigger. It will call OnPull, OnCallLink, OnRelease methods in this order.
	/// </summary>
	public void Pull() {
		test.Invoke();
		if (actions[0] != null) {
			actions[0].Invoke();
		}
		foreach (var link in links) {
			if (link != null) {
				if (actions[1] != null) {
					actions[1].Invoke();
				}
				link.Activate();
			}
		}
		if (actions[2] != null) {
			actions[2].Invoke();
		}
	}

	public ActivatedActionOld[] GetLinks() {
		return links.ToArray();
	}
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Trigger), true)]
public class TriggerEditor : Editor {
	ReorderableList linksList;

	void OnEnable() {
		linksList = new ReorderableList(serializedObject, serializedObject.FindProperty("links"), true, true, true, true) {
			drawHeaderCallback = (Rect rect) => {
				EditorGUI.LabelField(rect, "Linked Activated Actions");
			}
		};
		linksList.elementHeightCallback = (int index) => {
			var element = linksList.serializedProperty.GetArrayElementAtIndex(index);
			return EditorGUI.GetPropertyHeight(element);
		};
		linksList.drawElementCallback =
		(Rect rect, int index, bool isActive, bool isFocused) => {
			var element = linksList.serializedProperty.GetArrayElementAtIndex(index);
			rect.y += 2;
			rect.height -= 5;
			EditorGUI.PropertyField(rect, element, new GUIContent("Activated Action: " + (index + 1)));
		};
	}

	public override void OnInspectorGUI() {
		DrawDefaultInspector();
		linksList.DoLayoutList();
		serializedObject.ApplyModifiedProperties();
	}
}
#endif