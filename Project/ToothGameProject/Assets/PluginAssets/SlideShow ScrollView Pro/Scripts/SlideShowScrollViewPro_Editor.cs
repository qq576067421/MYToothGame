// Created by Carlos Arturo Rodriguez Silva (Legend) https://www.facebook.com/legendxh

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor (typeof(SlideShowScrollViewPro_Scroll))]
public class SlideShowScrollViewPro_Editor : Editor {
	public override void OnInspectorGUI () {
		var scroll = (SlideShowScrollViewPro_Scroll)target;

		if (GUILayout.Button ("Restart Script")) {
            scroll.Restart ();
		}

		DrawDefaultInspector ();
		

		if (GUILayout.Button ("Restart Script")) {
            scroll.Restart ();
		}

	}
}
#endif