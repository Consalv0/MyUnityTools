using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EventCondition : MonoBehaviour {
	public bool _constantCheck = false;
	public bool constantCheck { get { return _constantCheck; } set { _constantCheck = value; }}
	public bool[] conditions = new bool[1];
	public ActivatedEvents satisfyed;
	public ActivatedEvents unsatisfyed;

	[HideInInspector]
	public bool isSatisfyed = false;

	int selectedCondition = 0;

	void Update() {
		if (constantCheck) {
			UpdateBehaviour();
		}
	}

	// Check if is satified or not
	public bool UpdateBehaviour() {
		for (int i = 0; i < conditions.Length; i++) {
			if (conditions[i] == false) {
				unsatisfyed.Invoke();
				return isSatisfyed = false;
			}
		}
		satisfyed.Invoke();
		return isSatisfyed = true;
	}

	public void SelectCondition(int index) {
		selectedCondition = Mathf.Clamp(index, 0, conditions.Length - 1);
	}
	public void SetCondition(bool value) {
		SetCondition(selectedCondition, value);
	}
	public void SetCondition(int index, bool value) {
		conditions[index] = value;
		if (!constantCheck) {
			UpdateBehaviour();
		}
	}
	public void SwitchCondition(int index) {
		conditions[index] = !conditions[index];
		if (!constantCheck) {
			UpdateBehaviour();
		}
	}
}