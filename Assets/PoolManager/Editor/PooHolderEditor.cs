using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Security.Cryptography.X509Certificates;

[CustomEditor(typeof(PoolHolder))]
public class PoolHolderEditor : Editor {
	SerializedProperty awakeSize;
	SerializedProperty poolPrefab;

	void OnEnable() {
		awakeSize = serializedObject.FindProperty("awakeSize");
		poolPrefab = serializedObject.FindProperty("poolPrefab");
	}

	public override void OnInspectorGUI() {
		DrawDefaultInspector();

		if (poolPrefab.objectReferenceValue == null) {
			EditorGUILayout.HelpBox("The pools needs a prefab", MessageType.Error);
		} else if (awakeSize.intValue > 1999) {
			EditorGUILayout.HelpBox("The 'Awake Size' is too big may take a while to load", MessageType.Warning);
		}
	}
}
