using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class UnityInternalGUIResources : EditorWindow {
	struct GUIResource {
		public readonly GUIStyle style;
		public readonly GUIContent icon;
		public readonly GUIContent name;
		public bool isIcon;

		public GUIResource(GUIStyle style, string name) {
			this.style = style;
			this.name = new GUIContent(name);
			this.icon = GUIContent.none;
			isIcon = false;
		}

		public GUIResource(GUIContent content, string name) {
			this.style = GUIStyle.none;
			this.name = new GUIContent(name);
			this.icon = content;
			isIcon = true;
		}

		public override bool Equals(object obj) {
			return obj is GUIResource && this.Equals((GUIResource)obj);
		}

		public override int GetHashCode() {
			return (name.text + style.name).GetHashCode();
		}

		public bool Equals(GUIResource obj) {
			return name.text == obj.name.text;
		}

		public int CompareTo(GUIResource obj) {
			return string.Compare(name.text + style.name, obj.name.text + style.name, System.StringComparison.Ordinal);
		}
	}

	[MenuItem("Window/Unity Internal GUI Resources")]
	public static void ShowWindow() {
		GetWindow<UnityInternalGUIResources>("GUI Resources");
	}

	Rect bodyRect, headerRect, refreshButtonRect, toggleRect, searchBarRect, searchBarBodyRect, searchBarEndRect;
	Rect textureRect;
	GUIContent refreshButton, searchBar, isDrawIcon, noDrawIcon;
	Vector2 scrollPos;
	bool drawIcons = true;
	string search = "";
	List<GUIResource> GUIResources = new List<GUIResource>();

	GUIContent inactiveText = new GUIContent("inactive");
	GUIContent activeText = new GUIContent("active");

	void OnEnable() {
		drawIcons = true;
		refreshButton = new GUIContent(EditorGUIUtility.IconContent("d_preAudioLoopOff").image,
				"Refresh: Icons are only loaded in memory when the appropriate window is opened.");
		isDrawIcon = new GUIContent(EditorGUIUtility.IconContent("Shadow Icon").image);
		noDrawIcon = new GUIContent(EditorGUIUtility.IconContent("GUILayer Icon").image);
		
		this.StartCoroutine("FindGUIResources");
	}

	void OnGUI() {
		UpdateRects();

		GUI.Box(headerRect, "", EditorStyles.toolbarButton);
		if (GUI.Button(refreshButtonRect, refreshButton, EditorStyles.toolbarButton)) {
			this.StopAllCoroutines();
			GUIResources.Clear();
			this.StartCoroutine("FindGUIResources");
			foreach (GUIStyle style in GUI.skin.customStyles) {
				GUIResources.Add(
					new GUIResource(style, style.name)
				);
			}
		}
		if (GUI.Button(toggleRect, drawIcons ? isDrawIcon : noDrawIcon, EditorStyles.toolbarButton)) {
			this.StopAllCoroutines();
			GUIResources.Clear();
			drawIcons = !drawIcons;

			this.StartCoroutine("FindGUIResources");
			foreach (GUIStyle style in GUI.skin.customStyles) {
				GUIResources.Add(
					new GUIResource(style, style.name)
				);
			}
		}
		search = EditorGUI.TextField(searchBarBodyRect, search, (GUIStyle)"ToolbarSeachTextField");
		if (GUI.Button(searchBarEndRect, "", "ToolbarSeachCancelButton")) {
			search = "";
			GUI.FocusControl(null);
		}

		GUILayout.Space(EditorGUIUtility.singleLineHeight + 2);
		scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Width(bodyRect.width), GUILayout.Height(bodyRect.height));
		foreach (var item in GUIResources) {
			if (search == "" || item.name.text.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0) {
				if (drawIcons && item.isIcon) {
					textureRect = GUILayoutUtility.GetRect(18, 18, 18, 18, GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false));
					textureRect.x += 6;
					GUI.Box(textureRect, item.icon.image, new GUIStyle());
					textureRect.width = 30; textureRect.height = 30; textureRect.x += 18;
					GUI.Box(textureRect, item.icon.image, new GUIStyle());
					textureRect.width = 50; textureRect.height = 50; textureRect.x += 30;
					GUI.Box(textureRect, item.icon.image, new GUIStyle());
					EditorGUILayout.LabelField(item.icon, item.name, GUILayout.Height(EditorGUIUtility.singleLineHeight * 2));
					if (GUI.Button(GUILayoutUtility.GetLastRect(), "", new GUIStyle())) CopyText("EditorGUIUtility.FindTexture( \"" + item.name.text + "\" )");

				} else if (!drawIcons && !item.isIcon) {
					textureRect = GUILayoutUtility.GetRect(0, position.width, 70, 70);
					GUILayout.Space(EditorGUIUtility.singleLineHeight * 2);
					var r = textureRect;
					textureRect.y += EditorGUIUtility.singleLineHeight;
					textureRect.height += EditorGUIUtility.singleLineHeight;
					textureRect.x += 6;
					textureRect.width = textureRect.width / 2 - 10;
					textureRect.height -= 10;
					GUI.Toggle(textureRect, false, inactiveText, item.style);
					textureRect.x += textureRect.width + 6;
					GUI.Toggle(textureRect, true, activeText, item.style);
					textureRect.height += 10;
					if (GUI.Button(r, GUIContent.none, "RL Header")) CopyText("(GUIStyle)\"" + item.style.name + "\"");
					r.height = EditorGUIUtility.singleLineHeight;
					EditorGUI.LabelField(r, GUIContent.none, item.name, "Icon.OutlineBorder");
				}
			}
		}

		EditorGUILayout.EndScrollView();
	}

	void UpdateRects() {
		headerRect = new Rect(0, 0, position.width, EditorGUIUtility.singleLineHeight);

		refreshButtonRect = headerRect;
		refreshButtonRect.width = 25;

		toggleRect = headerRect;
		toggleRect.width = 25;
		toggleRect.x += 25;

		searchBarRect = headerRect;
		searchBarRect.width -= 50;
		searchBarRect.x += 50 + 5;

		searchBarBodyRect = searchBarRect;
		searchBarBodyRect.y += 2;
		searchBarBodyRect.width -= 15;

		searchBarEndRect = searchBarRect;
		searchBarEndRect.y += 2;
		searchBarEndRect.width = 15;
		searchBarEndRect.x = headerRect.width - 15;

		bodyRect = new Rect(0, headerRect.height, position.width, position.height - headerRect.height);
	}

	IEnumerator FindGUIResources() {
		List<Texture2D> icons = new List<Texture2D>((IEnumerable<Texture2D>)Resources.FindObjectsOfTypeAll(typeof(Texture2D)));
		icons.Sort((pA, pB) => System.String.Compare(pA.name, pB.name, System.StringComparison.OrdinalIgnoreCase));
		foreach (Object icon in icons) {
			if (!icon) continue;
			if (icon.name == System.String.Empty) continue;
			if ((icon.hideFlags & (HideFlags.HideAndDontSave | HideFlags.HideInInspector)) <= 0) continue;
			if (!EditorUtility.IsPersistent(icon)) continue;
			Debug.unityLogger.logEnabled = false;
			GUIContent iconContent = EditorGUIUtility.IconContent(icon.name);
			Debug.unityLogger.logEnabled = true;
			if (iconContent == null || iconContent.image == null) continue;
			if (GUIResources.Any()) {
				if (GUIResources.Last().isIcon && icon.name.Equals(GUIResources.Last().name.text)) continue;
			}

			GUIResources.Add(
				new GUIResource(iconContent, icon.name)
			);
			yield return new WaitForSeconds(0.00001f);
		}
	}

	void CopyText(string pText) {
		TextEditor editor = new TextEditor();

		//editor.content = new GUIContent(pText); // Unity 4.x code
		editor.text = pText; // Unity 5.x code

		editor.SelectAll();
		editor.Copy();
	}
}
