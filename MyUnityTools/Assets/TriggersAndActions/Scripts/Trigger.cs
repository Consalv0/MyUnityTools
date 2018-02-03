using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UtilityTools;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Trigger : MonoBehaviour {
	public ActivatedEvent links;

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
		if (links != null)
		foreach (var action in links.actions) {
			action.Initialize();
		}
	}

	/// <summary>
	/// Pull this trigger. It will call OnPull, OnCallLink, OnRelease methods in this order.
	/// </summary>
	public void Pull() {
		if (actions[0] != null) {
			actions[0].Invoke();
		}
		foreach (var link in links.actions) {
			if (link != null) {
				if (actions[1] != null) {
					actions[1].Invoke();
				}
				link.Invoke();
			}
		}
		if (actions[2] != null) {
			actions[2].Invoke();
		}
	}
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Trigger), true)]
public class TriggerEditor : Editor {

	void OnEnable() {
		
	}

	public override void OnInspectorGUI() {
		DrawDefaultInspector();
		serializedObject.ApplyModifiedProperties();
	}
}
#endif