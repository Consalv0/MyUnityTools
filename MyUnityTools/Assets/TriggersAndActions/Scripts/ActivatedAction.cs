using System.Reflection;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UtilityTools;

#if UNITY_EDITOR
using UnityEditor;
using UtilityToolsEditor;
#endif

[System.Serializable]
public class ActivatedAction {
	[SerializeField] InvokeMode callMode = InvokeMode.RuntimeAndEditor;
	[SerializeField] Object target;
	[SerializeField] Parameter[] parameters = new Parameter[3];
	[SerializeField] InvokeMask invokeMask = 0; // Remove SerializeField

	bool isEnabled = false;
	MethodInfo selectedMethodInfo;
	public string selectedMethodName;
	int numberOfParameters;
	public MethodInfo[] methodsInfo;
	string[] methodNames;

	System.Action action0;
	System.Action<object> action1;

	public void Initialize() {
		if (!isEnabled) {
			UpdateMethodsInfo();
			UpdateMethodNames();
			SelectMethodInfo();
			UpdateParameters();
			UpdateParameterValue();
			UpdateDelegate();
			isEnabled = true;
		}
	}

	public void Invoke() {
		Initialize();
		if (callMode == InvokeMode.RuntimeOnly && Application.isEditor) return;
		if (callMode == InvokeMode.Off || selectedMethodInfo == null) return;
		if ((invokeMask & InvokeMask.NoInvoke) > 0) return;
		if ((invokeMask & InvokeMask.MethodIsGeneric) > 0) {
			if ((invokeMask & InvokeMask.NoParameters) > 0) {
				selectedMethodInfo.Invoke(target, null);
			}  else {
				selectedMethodInfo.Invoke(target, GetParametersCurrentValue(true));
			}
		}  else if ((invokeMask & InvokeMask.NoParameters) > 0) {
			if ((invokeMask & InvokeMask.ReturnsVoid) > 0) {
				action0.Invoke();
			}  else {
				selectedMethodInfo.Invoke(target, null);
			}
		}  else if ((invokeMask & InvokeMask.OneParameter) > 0) {
			if ((invokeMask & InvokeMask.ReturnsVoid) > 0) {
				if ((invokeMask & InvokeMask.ParameterSubclassOfObject) > 0) {
					selectedMethodInfo.Invoke(target, GetParametersCurrentValue());
				}  else {
					action1.Invoke(parameters[0].currentValue);
				}
			}  else {
				selectedMethodInfo.Invoke(target, GetParametersCurrentValue());
			}
		}  else {
			selectedMethodInfo.Invoke(target, GetParametersCurrentValue());
		}
	}

	object[] GetParametersCurrentValue(bool isGeneric = false) {
		object[] parameterValues = new object[numberOfParameters];
		for (int i = 0; i < numberOfParameters; i++) {
			parameterValues[i] = parameters[isGeneric ? i + 1 : i].currentValue;
		}
		return parameterValues;
	}

	void UpdateMethodsInfo() {
		methodsInfo = new MethodInfo[0];
		if (target == null) return;
		var allMethodsInfo = target.GetMethodsInfo(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public).ToList();
		List<MethodInfo> newMethodsInfo = new List<MethodInfo>();
		foreach (var method in allMethodsInfo) {
			var methodParameters = method.GetParameters();
			var methodname = method.ToString();
			if (method.IsGenericMethod && methodParameters.Count() >= 3 || method.GetGenericArguments().Count() > 1) continue;
			if (methodParameters.Count() <= 3
				&& methodname.IndexOf(" get_", System.StringComparison.OrdinalIgnoreCase) <= 0
				&& methodname.IndexOf(" internal_", System.StringComparison.OrdinalIgnoreCase) <= 0
				&& methodname.IndexOf(" obj_", System.StringComparison.OrdinalIgnoreCase) <= 0
				&& methodname.IndexOf(" MemberwiseClone()", System.StringComparison.OrdinalIgnoreCase) <= 0
				&& methodname.IndexOf(" Finalize()", System.StringComparison.OrdinalIgnoreCase) <= 0
				&& methodname.IndexOf(" Update()", System.StringComparison.Ordinal) <= 0
				&& methodname.IndexOf(" Is", System.StringComparison.Ordinal) <= 0
				&& methodname.IndexOf(" Get", System.StringComparison.Ordinal) <= 0
				&& methodname.IndexOf(" CompareTag(", System.StringComparison.Ordinal) <= 0
				&& methodname.IndexOf(" ToString()", System.StringComparison.Ordinal) <= 0
				&& methodname.IndexOf(" Find", System.StringComparison.Ordinal) <= 0) {

				if (methodParameters.Count() <= 3) {
					bool isAcceptable = true;
					foreach (var parameter in methodParameters) {
						var paramType = parameter.ParameterType;
						if (!isAcceptable) continue;
						isAcceptable = (paramType.Equals(typeof(int)) || paramType.Equals(typeof(float)) || paramType.Equals(typeof(double))
														|| paramType.Equals(typeof(bool)) || paramType.Equals(typeof(Vector3)) || paramType.Equals(typeof(Vector2))
						                || paramType.Equals(typeof(string)) || paramType.Equals(typeof(Object)) || paramType.IsSubclassOf(typeof(Object))
						                || paramType.Equals(typeof(Color)));
					}
					if (isAcceptable) newMethodsInfo.Add(method);
				}
			}
		}
		newMethodsInfo = newMethodsInfo.OrderBy(o => o.GetParameters().Count()).ToList();
		newMethodsInfo = newMethodsInfo.OrderBy(o => o.ToString().IndexOf(" set_", System.StringComparison.Ordinal)).ToList();
		methodsInfo = newMethodsInfo.ToArray();
		isEnabled = false;
	}

	void UpdateMethodNames() {
		methodNames = new string[0];
		if (!methodsInfo.Any()) return;

		methodNames = new string[methodsInfo.Count()];
		for (int i = 0; i < methodsInfo.Count(); i++) {
			var methodParameters = methodsInfo[i].GetParameters();
			var name = methodsInfo[i].ToString();
			List<int> indx = new List<int>();
			name = name.Substring(name.IndexOf(' ') + 1);
			if (methodsInfo[i].GetParameters().Any()) {
				for (int j = name.IndexOf(','); j > -1; j = name.IndexOf(',', j + 1)) {
					indx.Add(j);
				}
				for (int j = methodParameters.Count() - 2; j >= 0; j--) {
					name = name.Insert(indx[j], " " + methodParameters[j].Name);
				}
				indx.Add(name.IndexOf(")", System.StringComparison.Ordinal));
				name = name.Insert(indx.Last(), " " + methodParameters[methodParameters.Count() - 1].Name);
			}
			if (name.Contains("set_")) {
				name = name.Remove(0, 4);
				name = name.First().ToString().ToUpper() + name.Substring(1);
				name = name.Insert(0, "Setter::");
			}
			name = name.Replace("Int32 ", "Int ");
			name = name.Replace("Single ", "Float ");
			name = name.Replace("UnityEngine.", "");
			name = name.Replace("System.", "");
			methodNames[i] = name;
		}
	}

	void SelectMethodInfo() {
		for (int i = 0; i < methodNames.Length; i++) {
			if (methodNames[i] == selectedMethodName) {
				selectedMethodInfo = methodsInfo[i];
				break;
			} 
		}
	}

	void UpdateParameters() {
		//parameterValues = new object[0];
		if (selectedMethodInfo == null) {
			for (int i = 0; i < parameters.Count(); i++) {
				parameters[i].Clear();
			}
			return;
		}
		if (!selectedMethodInfo.GetParameters().Any() && !selectedMethodInfo.IsGenericMethod) {
			for (int i = 0; i < parameters.Count(); i++) {
				parameters[i].Clear();
			}
			return;
		}
		if (selectedMethodInfo.IsGenericMethod) {
			for (int i = -1; i < parameters.Count() -1; i++) {
				parameters[i + 1].SetValues(selectedMethodInfo, i);
			}
		}  else {
			for (int i = 0; i < parameters.Count(); i++) {
				parameters[i].SetValues(selectedMethodInfo, i);
			}
		}
	}

	void UpdateParameterValue() {
		if (selectedMethodInfo == null) return;
		var parametersInfo = selectedMethodInfo.GetParameters();
		numberOfParameters = parametersInfo.Count();
		//parameterValues = new object[numberOfParameters];
		//for (int i = 0; i < numberOfParameters; i++) {
		//	parameterValues[i] = parameters[selectedMethodInfo.IsGenericMethod ? i + 1 : i]._currentValue;
		//}
	}

	void UpdateDelegate() {
		invokeMask = 0;
		if (target == null || selectedMethodInfo == null) { invokeMask = InvokeMask.NoInvoke; return; }
		if (selectedMethodInfo.ReturnType.Equals(typeof(void))) { invokeMask |= InvokeMask.ReturnsVoid; }
		if (selectedMethodInfo.IsGenericMethod) {
			if (parameters[0].stringParameter == "") { invokeMask = InvokeMask.NoInvoke; return; }
			var type = System.Type.GetType(parameters[0].stringParameter);
			if (type == null) { 
				if (!parameters[0].stringParameter.Contains("Assembly"))
					Debug.LogWarning("'" + parameters[0].stringParameter + "' Type does not exist, Try to use an assembly");
				else Debug.LogWarning("'" + parameters[0].stringParameter + "' Type does not exist");
				invokeMask = InvokeMask.NoInvoke; return; }
			MethodInfo genericFromObject = selectedMethodInfo.MakeGenericMethod(System.Type.GetType(parameters[0].stringParameter));
			selectedMethodInfo = genericFromObject;
			if (!selectedMethodInfo.GetParameters().Any()) { invokeMask |= InvokeMask.NoParameters; }
			invokeMask |= InvokeMask.MethodIsGeneric;
			return;
		}  else if (!selectedMethodInfo.GetParameters().Any()) {
			invokeMask |= InvokeMask.NoParameters;
			if (selectedMethodInfo.ReturnType.Equals(typeof(void))) {
				action0 = selectedMethodInfo.CreateAction(target);
			}
		}  else if (selectedMethodInfo.GetParameters().Count() == 1) {
			var type = selectedMethodInfo.GetParameters()[0].ParameterType;
			if (selectedMethodInfo.ReturnType.Equals(typeof(void))) {
				// if (type.Equals(typeof(Object))) { action1 = selectedMethodInfo.CreateAction<Object>(target); }
				if (type.Equals(typeof(Object))) {
					action1 = selectedMethodInfo.CreateAction<Object>(target); }
				else if (type.IsSubclassOf(typeof(Object))) { invokeMask |= InvokeMask.ParameterSubclassOfObject; }
				else if (type.Equals(typeof(Vector3))) { action1 = selectedMethodInfo.CreateAction<Vector3>(target); }
				else if (type.Equals(typeof(Vector2))) { action1 = selectedMethodInfo.CreateAction<Vector2>(target); }
				else if (type.Equals(typeof(Color))) { action1 = selectedMethodInfo.CreateAction<Color>(target); }
				else if (type.Equals(typeof(bool))) { action1 = selectedMethodInfo.CreateAction<bool>(target); }
				else if (type.Equals(typeof(string))) { action1 = selectedMethodInfo.CreateAction<string>(target); }
				else if (type.Equals(typeof(float))) { action1 = selectedMethodInfo.CreateAction<float>(target); }
				else if (type.Equals(typeof(double))) { action1 = selectedMethodInfo.CreateAction<double>(target); }
				else if (type.Equals(typeof(int))) { action1 = selectedMethodInfo.CreateAction<int>(target); }
			}
			invokeMask |= InvokeMask.OneParameter;
		}  else if (selectedMethodInfo.GetParameters().Count() > 1) {
			//if (selectedMethodInfo.ReturnType.Equals(typeof(void))) { }
			invokeMask |= InvokeMask.TwoOrMoreParameters;
		}  else {
			invokeMask = InvokeMask.NoInvoke;
		}
	}

	[System.Serializable]
	[System.Flags] public enum InvokeMask {
		NoInvoke = 1 << 0,
		ReturnsVoid = 1 << 1,
		MethodIsGeneric = 1 << 2,
		NoParameters = 1 << 3,
		OneParameter = 1 << 4,
		TwoOrMoreParameters = 1 << 5,
		ParameterSubclassOfObject = 1 << 6
	}

	[System.Serializable]
	public struct Parameter {
		[SerializeField] string name;
		[SerializeField] string type;
		public bool isGeneric;
		public Object objectParameter;
		public Color colorParameter;
		public Vector3 vectorParameter;
		public string stringParameter;
		public bool boolParameter;
		public bool isSwitch;
		public float floatParameter;
		public float doubleParameter;
		public int intParameter;
		ParameterInfo parameterInfo;
		public object _currentValue;

		public object currentValue { get {
				if (isSwitch) return boolParameter = isSwitch ? !boolParameter : boolParameter;
				UpdateParameterValue();
				return _currentValue;
			}
		}

		public string stringType {
			get { return type; }
		}

		public void Clear(bool all = false) {
			name = ""; type = ""; isGeneric = false; 
			parameterInfo = null; _currentValue = null;
			if (all) {
				objectParameter = null;
				colorParameter = Color.black;
				vectorParameter = Vector3.zero;
				stringParameter = System.String.Empty; 
				isSwitch = false;
				boolParameter = false;
				floatParameter = 0;
				doubleParameter = 0;
				intParameter = 0;
			}
		}

		public void SetValues(MethodInfo method, int parameterIndex) {
			ParameterInfo[] parameters = method.GetParameters();
			isGeneric = method.IsGenericMethod;
			if (isGeneric && parameterIndex == -1) {
				parameterInfo = null;
				name = "Type" + method.GetGenericArguments()[0];
				type = typeof(System.Type).AssemblyQualifiedName;
			}  else {
				if (parameterIndex >= parameters.Count()) { Clear(); return; }
				if (parameterInfo != parameters[parameterIndex]) Clear();
				parameterInfo = parameters[parameterIndex];
				name = parameterInfo.ToString();
				type = parameterInfo.ParameterType.AssemblyQualifiedName;
			}
			name = name.Split(' ').Last();
			name = name.First().ToString().ToUpper() + name.Substring(1);
			name = System.Text.RegularExpressions.Regex.Replace(name, @"((?<=\p{Ll})\p{Lu})|((?!\A)\p{Lu}(?>\p{Ll}))", " $0");
			UpdateParameterValue();
		}

		void UpdateParameterValue() {
			System.Type t = System.Type.GetType(type);
			_currentValue = null;

			if (t == null) return;
			isSwitch = t.Equals(typeof(bool)) && isSwitch;
			if (t.IsSubclassOf(typeof(Object)) | type.Equals(typeof(Object))) { _currentValue = objectParameter; }
			else if (t.Equals(typeof(Vector3))) { _currentValue = vectorParameter; }
			else if (t.Equals(typeof(Vector2))) { _currentValue = (Vector2)vectorParameter; }
			else if (t.Equals(typeof(Color))) { _currentValue = colorParameter; }
			else if (t.Equals(typeof(bool))) { _currentValue = boolParameter; }
			else if (t.Equals(typeof(string))) { _currentValue = stringParameter; }
			else if (t.Equals(typeof(float))) { _currentValue = floatParameter; }
			else if (t.Equals(typeof(double))) { _currentValue = doubleParameter; }
			else if (t.Equals(typeof(int))) { _currentValue = intParameter; }
		}
	}

	[System.Serializable]
	public enum InvokeMode {
		Off, RuntimeOnly, RuntimeAndEditor
	}
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ActivatedAction), true)]
public class ActivatedActionDrawer : PropertyDrawer {
	SerializedProperty target, callMode, selectedMethodName;
	MethodInfo[] methodsInfo;
	string[] methodNames = new string[0];
	int methodIndexSelection;
	Rect propertyRect, headerRect, targetRect, tabRect, prefixRect, navigationRect, bodyRect;
	Rect invokeModeRect, methodRect;
	//ActivatedAction activatedAction;
	SerializedProperty parameters;
	List<Rect> parameterRects = new List<Rect>();
	int indetLevel;

	int parameterBodyLines = 0;

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
		parameters = property.FindPropertyRelative("parameters");
		parameterBodyLines = 0;
		for (int i = 0; i < parameters.arraySize; i++) {
			if (!parameters.GetArrayElementAtIndex(i).FindPropertyRelative("type").stringValue.Equals("")) {
				parameterBodyLines += 1;
				if (parameters.GetArrayElementAtIndex(i).FindPropertyRelative("type").stringValue.Contains("Vector") && Screen.width <= 332) {
					parameterBodyLines += 1;
				}
			}
		}
		float height = (EditorGUIUtility.singleLineHeight + 1) * (3 + parameterBodyLines);
		return height;
	}

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
		FindProperties(property);
		UpdateRects(position);
		UpdateMethodsInfo();
		UpdateMethodsNames();
		CheckMethodIndexSelection();
		UpdateParameters();

		EditorGUI.BeginProperty(position, label, property);
		indetLevel = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;
		DrawBody();

		EditorGUI.PrefixLabel(prefixRect, new GUIContent(label.text + "()"));
		target.objectReferenceValue =
			EditorGUI.ObjectField(targetRect, new GUIContent(), target.objectReferenceValue, typeof(Object), true);

		if (EditorGUI.DropdownButton(navigationRect, new GUIContent(), FocusType.Keyboard)) {
			PopupWindow.Show(navigationRect, new NavigationPopup(target, this));
		}
		EditorGUI.LabelField(navigationRect, EditorGUIUtility.IconContent("UnityEditor.HierarchyWindow"));

		//EditorGUI.MaskField(invokeModeRect, property.FindPropertyRelative("invokeMask").intValue,
		                    //new string[] { "NoInvoke", "ReturnsVoid", "MethodIsGeneric", "NoParameters",
		                     //"OneParameter", "TwoOrMoreParameters", "ParameterSubclassOfObject" });

		EditorGUI.PropertyField(invokeModeRect, callMode, new GUIContent());
		if (GUI.changed) {
			property.serializedObject.ApplyModifiedProperties();
			GUI.changed = false;
		}
		methodIndexSelection = EditorGUI.Popup(methodRect, "", methodIndexSelection, methodNames);
		if (GUI.changed) {
			selectedMethodName.stringValue = methodNames[methodIndexSelection];
			property.serializedObject.ApplyModifiedProperties();
			GUI.changed = false;
		}

		for (int i = 0; i < parameters.arraySize; i++) {
			EditorGUI.PropertyField(parameterRects[i], parameters.GetArrayElementAtIndex(i), true);
			if (GUI.changed) {
				parameters.GetArrayElementAtIndex(i).serializedObject.ApplyModifiedProperties();
				GUI.changed = false;
			}
		}

		EditorGUI.EndProperty();
		EditorGUI.indentLevel = indetLevel;
	}

	public void CheckMethodIndexSelection() {
		methodIndexSelection = -1;
		for (int i = 0; i < methodNames.Length; i++) {
			if (selectedMethodName.stringValue == methodNames[i]) {
				methodIndexSelection = i;
				break;
			}
		}

		if (methodIndexSelection == -1) {
			selectedMethodName.stringValue = "";
			selectedMethodName.serializedObject.ApplyModifiedProperties();
		}
	}

	void FindProperties(SerializedProperty property) {
		target = property.FindPropertyRelative("target");
		selectedMethodName = property.FindPropertyRelative("selectedMethodName");
		parameters = property.FindPropertyRelative("parameters");
		callMode = property.FindPropertyRelative("callMode");

		//UpdateMethodsInfo();
		//UpdateMethodsNames();
	}

	void UpdateMethodsNames() {
		methodNames = new string[0];
		if (methodsInfo.Length == 0)
			return;

		methodNames = new string[methodsInfo.Count()];
		for (int i = 0; i < methodsInfo.Count(); i++) {
			var methodParameters = methodsInfo[i].GetParameters();
			var name = methodsInfo[i].ToString();
			List<int> indx = new List<int>();
			name = name.Substring(name.IndexOf(' ') + 1);
			if (methodsInfo[i].GetParameters().Any()) {
				for (int j = name.IndexOf(','); j > -1; j = name.IndexOf(',', j + 1)) {
					indx.Add(j);
				}
				for (int j = methodParameters.Count() - 2; j >= 0; j--) {
					name = name.Insert(indx[j], " " + methodParameters[j].Name);
				}
				indx.Add(name.IndexOf(")", System.StringComparison.Ordinal));
				name = name.Insert(indx.Last(), " " + methodParameters[methodParameters.Count() - 1].Name);
			}
			if (name.Contains("set_")) {
				name = name.Remove(0, 4);
				name = name.First().ToString().ToUpper() + name.Substring(1);
				name = name.Insert(0, "Setter::");
			}
			name = name.Replace("Int32 ", "Int ");
			name = name.Replace("Single ", "Float ");
			name = name.Replace("UnityEngine.", "");
			name = name.Replace("System.", "");
			methodNames[i] = name;
		}
	}

	void UpdateMethodsInfo() {
		methodsInfo = new MethodInfo[0];
		Object _target = target.objectReferenceValue;
		if (_target == null) return;
		var allMethodsInfo = _target.GetMethodsInfo(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public).ToList();
		List<MethodInfo> newMethodsInfo = new List<MethodInfo>();
		foreach (var method in allMethodsInfo) {
			var methodParameters = method.GetParameters();
			var methodname = method.ToString();
			if (method.IsGenericMethod && methodParameters.Count() >= 3 || method.GetGenericArguments().Count() > 1) continue;
			if (methodParameters.Count() <= 3
				&& methodname.IndexOf(" get_", System.StringComparison.OrdinalIgnoreCase) <= 0
				&& methodname.IndexOf(" internal_", System.StringComparison.OrdinalIgnoreCase) <= 0
				&& methodname.IndexOf(" obj_", System.StringComparison.OrdinalIgnoreCase) <= 0
				&& methodname.IndexOf(" MemberwiseClone()", System.StringComparison.OrdinalIgnoreCase) <= 0
				&& methodname.IndexOf(" Finalize()", System.StringComparison.OrdinalIgnoreCase) <= 0
				&& methodname.IndexOf(" Update()", System.StringComparison.Ordinal) <= 0
				&& methodname.IndexOf(" Is", System.StringComparison.Ordinal) <= 0
				&& methodname.IndexOf(" Get", System.StringComparison.Ordinal) <= 0
				&& methodname.IndexOf(" CompareTag(", System.StringComparison.Ordinal) <= 0
				&& methodname.IndexOf(" ToString()", System.StringComparison.Ordinal) <= 0
				&& methodname.IndexOf(" Find", System.StringComparison.Ordinal) <= 0) {

				if (methodParameters.Count() <= 3) {
					bool isAcceptable = true;
					foreach (var parameter in methodParameters) {
						var paramType = parameter.ParameterType;
						if (!isAcceptable) continue;
						isAcceptable = (paramType.Equals(typeof(int)) || paramType.Equals(typeof(float)) || paramType.Equals(typeof(double))
														|| paramType.Equals(typeof(bool)) || paramType.Equals(typeof(Vector3)) || paramType.Equals(typeof(Vector2))
														|| paramType.Equals(typeof(string)) || paramType.Equals(typeof(Object)) || paramType.IsSubclassOf(typeof(Object))
														|| paramType.Equals(typeof(Color)));
					}
					if (isAcceptable) newMethodsInfo.Add(method);
				}
			}
		}
		newMethodsInfo = newMethodsInfo.OrderBy(o => o.GetParameters().Count()).ToList();
		newMethodsInfo = newMethodsInfo.OrderBy(o => o.ToString().IndexOf(" set_", System.StringComparison.Ordinal)).ToList();
		methodsInfo = newMethodsInfo.ToArray();
	}

	void ClearParameter(SerializedProperty property) {
		property.FindPropertyRelative("name").stringValue = "";
		property.FindPropertyRelative("type").stringValue = "";
		property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
	}

	void UpdateParameter(SerializedProperty property, MethodInfo method, int parameterIndex) {
		string name, type;
		ParameterInfo[] parameters = method.GetParameters();
		bool isGeneric = method.IsGenericMethod;
		if (isGeneric && parameterIndex == -1) {
			name = "Type" + method.GetGenericArguments()[0];
			type = typeof(System.Type).AssemblyQualifiedName;
		}
		else {
			if (parameterIndex >= parameters.Count()) { ClearParameter(property); return; }
			ParameterInfo parameterInfo = parameters[parameterIndex];
			name = parameterInfo.ToString();
			type = parameterInfo.ParameterType.AssemblyQualifiedName;
		}
		name = name.Split(' ').Last();
		name = name.First().ToString().ToUpper() + name.Substring(1);
		name = System.Text.RegularExpressions.Regex.Replace(name, @"((?<=\p{Ll})\p{Lu})|((?!\A)\p{Lu}(?>\p{Ll}))", " $0");
		property.FindPropertyRelative("name").stringValue = name;
		property.FindPropertyRelative("type").stringValue = type;
		property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
	}

	void UpdateParameters() {
		if (selectedMethodName.stringValue == "" || methodIndexSelection == -1) {
			for (int i = 0; i < parameters.arraySize; i++) {
				ClearParameter(parameters.GetArrayElementAtIndex(i));
			}
			return;
		}
		MethodInfo selectedMethodInfo = methodsInfo[methodIndexSelection];
		if (!selectedMethodInfo.GetParameters().Any() && !selectedMethodInfo.IsGenericMethod) {
			for (int i = 0; i < parameters.arraySize; i++) {
				ClearParameter(parameters.GetArrayElementAtIndex(i));
			}
			return;
		}
		if (selectedMethodInfo.IsGenericMethod) {
			for (int i = -1; i < parameters.arraySize - 1; i++) {
				UpdateParameter(parameters.GetArrayElementAtIndex(i + 1), selectedMethodInfo, i);
			}
		}
		else {
			for (int i = 0; i < parameters.arraySize; i++) {
				UpdateParameter(parameters.GetArrayElementAtIndex(i), selectedMethodInfo, i);
			}
		}
	}

	void UpdateRects(Rect rect) {
		propertyRect = rect;
		propertyRect.y -= 2;
		propertyRect.height += 2;

		headerRect = new Rect(propertyRect);
		headerRect.height = EditorGUIUtility.singleLineHeight + 4;
		headerRect.y += 3;

		tabRect = new Rect(headerRect);
		tabRect.width = EditorGUIUtility.labelWidth;

		prefixRect = new Rect(tabRect);
		prefixRect.x += 6;

		targetRect = new Rect(headerRect);
		targetRect.height = EditorGUIUtility.singleLineHeight;
		targetRect.x += EditorGUIUtility.labelWidth + 35; targetRect.width -= EditorGUIUtility.labelWidth + 35 + 12;

		navigationRect = new Rect(EditorGUIUtility.labelWidth + 20 + EditorGUI.IndentedRect(propertyRect).x - 14, targetRect.y + 1,
															27, EditorGUIUtility.singleLineHeight + 1);

		bodyRect = new Rect(propertyRect);
		bodyRect.y += headerRect.height; bodyRect.height -= headerRect.height;

		methodRect = bodyRect;
		methodRect.y += 4; methodRect.height = EditorGUIUtility.singleLineHeight;
		methodRect.x += EditorGUIUtility.labelWidth + 6; methodRect.width -= EditorGUIUtility.labelWidth + 12;

		invokeModeRect = bodyRect;
		invokeModeRect.y += 4; invokeModeRect.height = EditorGUIUtility.singleLineHeight;
		invokeModeRect.x += 6; invokeModeRect.width = EditorGUIUtility.labelWidth - 6;

		GenerateParameterRects(parameters.arraySize);
	}

	void GenerateParameterRects(int size) {
		parameterRects.Clear();
		Rect parameterRect;
		for (int i = 0; i < size; i++) {
			if (i == 0) {
				parameterRect = bodyRect;
				parameterRect.y += 4 + 20; parameterRect.height = EditorGUIUtility.singleLineHeight;
				parameterRect.x += 6; parameterRect.width -= 6 * 2;
				if (parameters.GetArrayElementAtIndex(i).FindPropertyRelative("type").stringValue.Contains("Vector") && Screen.width <= 332) {
					parameterRect.height += EditorGUIUtility.singleLineHeight;
				}
			} else {
				parameterRect = parameterRects[i - 1];
				parameterRect.y += parameterRect.height + 2; parameterRect.height = EditorGUIUtility.singleLineHeight;
				if (parameters.GetArrayElementAtIndex(i).FindPropertyRelative("type").stringValue.Contains("Vector") && Screen.width <= 332) {
					parameterRect.height += EditorGUIUtility.singleLineHeight;
				}
			}
			parameterRects.Add(parameterRect);
		}
	}

	void DrawBody() {
		GUI.Box(headerRect, "", new GUIStyle("HelpBox"));
		GUI.Box(tabRect, "", new GUIStyle("RL Header"));
		bodyRect.height -= 1;
		GUI.Box(bodyRect, "", new GUIStyle("HelpBox"));
		bodyRect.height += 1;
		GUI.Box(bodyRect, "", new GUIStyle("RL Background"));
	}
}

[CustomPropertyDrawer(typeof(ActivatedAction.Parameter), true)]
public class ParameterDrawer : PropertyDrawer {
	public SerializedProperty name, typeString, isGeneric;
	public SerializedProperty objectParameter;
	public SerializedProperty colorParameter;
	public SerializedProperty vectorParameter;
	public SerializedProperty stringParameter;
	public SerializedProperty boolParameter, isSwitch;
	public SerializedProperty floatParameter;
	public SerializedProperty doubleParameter;
	public SerializedProperty intParameter;

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
		return EditorGUIUtility.singleLineHeight + 2;
	}

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
		FindPropertiesAndRects(position, property, label);
		name.serializedObject.Update();
		var type = System.Type.GetType(typeString.stringValue);
		if (type == null) return;
		if (type.Equals(typeof(System.Type))) {
			Rect objectRect = position; objectRect.width = 20; objectRect.x = position.width + EditorGUI.IndentedRect(position).x - 20;
			Rect stringRect = position; stringRect.width -= 17;
			objectParameter.objectReferenceValue =
				EditorGUI.ObjectField(objectRect, new GUIContent(), objectParameter.objectReferenceValue,
															typeof(object), true);
			if (GUI.changed) {
				if (objectParameter.objectReferenceValue == null) return;
				string typeName = objectParameter.objectReferenceValue.GetType().FullName;
				if (typeName.Contains("UnityEditor")) {
					typeName = objectParameter.objectReferenceValue.name + ", Assembly-CSharp";
				}
				if (System.Type.GetType(typeName) == null) { typeName += ", UnityEngine"; }
				if (System.Type.GetType(typeName) == null) { typeName = ""; }
				stringParameter.stringValue = typeName;
			}
			stringParameter.stringValue =
				EditorGUI.DelayedTextField(stringRect, new GUIContent(name.stringValue), stringParameter.stringValue); return;
		}
		if (type.IsSubclassOf(typeof(Object)) | type.Equals(typeof(Object))) {
			if (objectParameter.objectReferenceValue != null 
			    && !(objectParameter.objectReferenceValue.GetType().Equals(type) || objectParameter.objectReferenceValue.GetType().IsSubclassOf(type))) {
				objectParameter.objectReferenceValue = null; }
			objectParameter.objectReferenceValue = 
				EditorGUI.ObjectField(position, new GUIContent(name.stringValue), objectParameter.objectReferenceValue, type, true); return; }
		if (type.Equals(typeof(Vector3))) {
			vectorParameter.vector3Value = 
				EditorGUI.Vector3Field(position, new GUIContent(name.stringValue), vectorParameter.vector3Value); return; }
		if (type.Equals(typeof(Vector2))) {
			vectorParameter.vector3Value = 
				EditorGUI.Vector2Field(position, new GUIContent(name.stringValue), vectorParameter.vector3Value); return; }
		if (type.Equals(typeof(Color))) { 
			colorParameter.colorValue =
			 EditorGUI.ColorField(position, new GUIContent(name.stringValue), colorParameter.colorValue); return; }
		if (type.Equals(typeof(bool))) {
			Rect switchRect, boolRect;
			switchRect = position; switchRect.width = (position.xMax - position.xMin) / 2; switchRect.x += (position.xMax - position.xMin) / 2;
			boolRect = position; boolRect.width = (position.xMax - position.xMin) / 2;
			boolParameter.boolValue = 
				EditorGUI.Toggle(boolRect, new GUIContent(name.stringValue), boolParameter.boolValue);
			isSwitch.boolValue = 
				EditorGUI.Toggle(switchRect, new GUIContent("Switch"), isSwitch.boolValue); return; }
		if (type.Equals(typeof(string))) { 
			stringParameter.stringValue = 
				EditorGUI.TextField(position, new GUIContent(name.stringValue), stringParameter.stringValue); return; }
		if (type.Equals(typeof(float))) { 
			floatParameter.floatValue = 
				EditorGUI.FloatField(position, new GUIContent(name.stringValue), floatParameter.floatValue); return; }
		if (type.Equals(typeof(double))) { 
			doubleParameter.doubleValue = 
				EditorGUI.DoubleField(position, new GUIContent(name.stringValue), doubleParameter.doubleValue); return; }
		if (type.Equals(typeof(int))) { 
			intParameter.intValue = 
				EditorGUI.IntField(position, new GUIContent(name.stringValue), intParameter.intValue); return; }
	}

	void FindPropertiesAndRects(Rect position, SerializedProperty property, GUIContent label) {
		name = property.FindPropertyRelative("name");
		typeString = property.FindPropertyRelative("type");
		isGeneric = property.FindPropertyRelative("isGeneric");
		objectParameter = property.FindPropertyRelative("objectParameter");
		colorParameter = property.FindPropertyRelative("colorParameter");
		vectorParameter = property.FindPropertyRelative("vectorParameter");
		stringParameter = property.FindPropertyRelative("stringParameter");
		boolParameter = property.FindPropertyRelative("boolParameter");
		isSwitch = property.FindPropertyRelative("isSwitch");
		floatParameter = property.FindPropertyRelative("floatParameter");
		doubleParameter = property.FindPropertyRelative("doubleParameter");
		intParameter = property.FindPropertyRelative("intParameter");
	}
}

public class NavigationPopup : PopupWindowContent {
	public NavigationPopup(SerializedProperty target, ActivatedActionDrawer activatedActionDrawer) {
		this.target = target;
		this.activatedActionDrawer = activatedActionDrawer;
		GetNavigationContent();
	}
	public SerializedProperty target;
	public ActivatedActionDrawer activatedActionDrawer;
	Queue<Object> objects = new Queue<Object>();
	Queue<GUIContent[]> componentNames = new Queue<GUIContent[]>();

	public override Vector2 GetWindowSize() {
		if (objects.Any()) {
			return new Vector2(EditorGUIUtility.labelWidth, (EditorGUIUtility.singleLineHeight + 2) * objects.Count() + 12);
		}  else {
			return Vector3.one;
		}
	}

	public override void OnGUI(Rect rect) {
		Rect objRect = new Rect(rect);
		objRect.y += 6;
		objRect.height = EditorGUIUtility.singleLineHeight + 2;
		objRect.x += 12; objRect.width -= 24;

		for (int i = 0; i < objects.Count(); i++) {
			var icon = componentNames.ToList()[i][0];
			var name = componentNames.ToList()[i][1];
			objRect.width = 15;
			GUI.Box(objRect, icon.image, new GUIStyle());
			objRect.x = rect.x + 30;
			objRect.width = rect.width - 30 - 6;
			GUI.Box(objRect, name, new GUIStyle("IN ObjectField"));
			objRect.x = rect.x + 12;
			objRect.width = rect.width - 24;
			if (EditorGUI.DropdownButton(objRect, new GUIContent(), FocusType.Keyboard, new GUIStyle())) {
				target.objectReferenceValue = objects.ToList()[i];
				target.serializedObject.ApplyModifiedProperties();
				//activatedAction.target = target.objectReferenceValue;
				editorWindow.Close();
			}
			objRect.y += EditorGUIUtility.singleLineHeight + 2;
		}
	}

	public override void OnOpen() {
		GetNavigationContent();
	}

	public override void OnClose() {
		activatedActionDrawer.CheckMethodIndexSelection();
	}

	void GetNavigationContent() {
		objects.Clear();
		componentNames = new Queue<GUIContent[]>();
		if ((target.objectReferenceValue as GameObject)) {
			var obtainedValues = (target.objectReferenceValue as GameObject).GetComponents<Component>().ToList();
			for (int i = 0; i < obtainedValues.Count(); i++) {
				objects.Enqueue(obtainedValues[i]);
				var icon = EditorGUIUtility.ObjectContent(objects.ElementAt(i), objects.ElementAt(i).GetType());
				icon = new GUIContent(icon.image);
				componentNames.Enqueue(new GUIContent[] {
					icon,
					new GUIContent(objects.ElementAt(i).GetType().ToString().Split('.').Last())
				} );
			}
			objects.Enqueue(target.objectReferenceValue as GameObject);
			componentNames.Enqueue(new GUIContent[] {
				EditorGUIUtility.IconContent("GameObject Icon"),
				new GUIContent("GameObject")
			} );
		}  else if ((target.objectReferenceValue as Component)) {
			var obtainedValues = (target.objectReferenceValue as Component).GetComponents<Component>().ToList();
			for (int i = 0; i < obtainedValues.Count(); i++) {
				objects.Enqueue(obtainedValues[i]);
				var icon = EditorGUIUtility.ObjectContent(objects.ElementAt(i), objects.ElementAt(i).GetType());
				icon = new GUIContent(icon.image);
				componentNames.Enqueue(new GUIContent[] {
					icon,
					new GUIContent(objects.ElementAt(i).GetType().ToString().Split('.').Last())
				} );
			}
			objects.Enqueue((target.objectReferenceValue as Component).gameObject);
			componentNames.Enqueue(new GUIContent[] {
				EditorGUIUtility.IconContent("GameObject Icon"),
				new GUIContent("GameObject")
			} );
		}
	}
}
#endif
