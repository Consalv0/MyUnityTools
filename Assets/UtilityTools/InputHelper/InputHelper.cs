using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityTools;

namespace UtilityTools {
	public class InputHelper : MonoBehaviour {
		public static float inactiveTime = 1;

		public static bool _countTime;
		public bool countTime { 
			get { return _countTime; }
			set { 
				if (value && _countTime == false) { 
					_countTime = true;
					StartCoroutine(InactiveCounter());
				}
				else if (value == false) {
					StopCoroutine(InactiveCounter());
					_countTime = false;
				}
			}
		}

		[HideInInspector] public List<string> axes = new List<string>(10);

		void Awake() {
			countTime = true;
		}

		[SerializeField] string _horizontal = "Horizontal";
		[SerializeField] string _vertical = "Vertical";
		[SerializeField] string _mouseHorizontal = "Mouse X";
		[SerializeField] string _mouseVertical = "Mouse Y";
		[SerializeField] string _scrollWheel = "Mouse ScrollWheel";
		[SerializeField] string _fire1 = "Fire1";
		[SerializeField] string _fire2 = "Fire2";
		[SerializeField] string _fire3 = "Fire3";
		[SerializeField] string _jump = "Jump";
		[SerializeField] string _submit = "Submit";
		[SerializeField] string _cancel = "Cancel";

		public string horizontal { get { SetAxeAtIndex(0, _horizontal); return _horizontal; } set { _horizontal = value; SetAxeAtIndex(0, value); } }
		public string vertical { get { SetAxeAtIndex(1, _vertical); return _vertical; } set { _vertical = value; SetAxeAtIndex(1, value); } }
		public string mouseHorizontal { get { SetAxeAtIndex(2, _mouseHorizontal); return _mouseHorizontal; } set { _mouseHorizontal = value; SetAxeAtIndex(2, value); } }
		public string mouseVertical { get { SetAxeAtIndex(3, _mouseVertical); return _mouseVertical; } set { _mouseVertical = value; SetAxeAtIndex(3, value); } }
		public string scrollWheel { get { SetAxeAtIndex(4, _scrollWheel); return _scrollWheel; } set { _scrollWheel = value; SetAxeAtIndex(4, value); } }
		public string fire1 { get { SetAxeAtIndex(5, _fire1); return _fire1; } set { _fire1 = value; SetAxeAtIndex(5, value); } }
		public string fire2 { get { SetAxeAtIndex(6, _fire2); return _fire2; } set { _fire2 = value; SetAxeAtIndex(6, value); } }
		public string fire3 { get { SetAxeAtIndex(7, _fire3); return _fire3; } set { _fire3 = value; SetAxeAtIndex(7, value); } }
		public string jump { get { SetAxeAtIndex(8, _jump); return _jump; } set { _jump = value; SetAxeAtIndex(8, value); } }
		public string submit { get { SetAxeAtIndex(9, _submit); return _submit; } set { _submit = value; SetAxeAtIndex(9, value); } }
		public string cancel { get { SetAxeAtIndex(10, _cancel); return _cancel; } set { _cancel = value; SetAxeAtIndex(10, value); } }

		public void SetAxeAtIndex(int index, string axe) {
			if (axes == null) return;
			if (index <= -1) return;
			if (index >= axes.Count) {
				axes.Add(axe);
				return;
			}
			axes[index] = axe;
		}

		public string GetAxeAtIndex(int index, bool resetInactivity = false) {
			if (axes == null) return "";
			if (index >= axes.Count) return "";
			if (index <= -1) return "";
			if (axes[index] == null) return "";
			if (resetInactivity) inactiveTime = 0;
			return axes[index];
		}

		IEnumerator InactiveCounter() {
			while(_countTime) {
				yield return new WaitForSeconds(1);
				inactiveTime += 1;
			}
			yield return null;
		}
	}
}

#if UNITY_EDITOR
namespace UtilityToolsEditor {
	using UnityEditor;
	[CustomEditor(typeof(InputHelper))]
	public class InputHelperDrawer : Editor {
		public override void OnInspectorGUI() {
			DrawDefaultInspector();
			bool GUIenabled = GUI.enabled;
			GUI.enabled = false;
			EditorGUILayout.FloatField(new GUIContent("Inactive Time"), InputHelper.inactiveTime);
			GUI.enabled = GUIenabled;
			serializedObject.ApplyModifiedProperties();
		}
	}
}
#endif