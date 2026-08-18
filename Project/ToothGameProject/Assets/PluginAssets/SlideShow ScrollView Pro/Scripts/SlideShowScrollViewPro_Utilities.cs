using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SlideShowScrollViewPro_Utilities
{
    /// <summary>
    /// Get array element without error
    /// </summary>
    /// <returns>The get element.</returns>
    /// <param name="array">Array.</param>
    /// <param name="index">Index.</param>
    /// <typeparam name="T">The 1st type parameter.</typeparam>
    public static T TryGetElement<T>(this T[] array, int index)
    {
        if (index < array.Length) {
            return array[index];
        }
        return default(T);
    }

    /// <summary>
    /// Get Children from a GameObject, you can specify the recursivity
    /// </summary>
    /// <returns>The children.</returns>
    /// <param name="parent">Parent.</param>
    /// <param name="recursive">If set to <c>true</c> recursive.</param>
    public static GameObject[] GetChildren(GameObject parent, bool recursive = false)
    {
        List<GameObject> children = new List<GameObject>();
        for (int i = 0; i < parent.transform.childCount; i++) {
            children.Add(parent.transform.GetChild(i).gameObject);
            if (recursive) { // set true to go through the hiearchy.
                children.AddRange(GetChildren(parent.transform.GetChild(i).gameObject, recursive));
            }
        }
        return children.ToArray();
    }

    /// <summary>
    /// Returns the active child count.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static int GetActiveChildCount(RectTransform obj)
    {
        int activeChildCount = 0;
        foreach (Transform child in obj.transform) {
            if (child.gameObject.activeSelf) {
                activeChildCount++;
            }
        }
        return activeChildCount;
    }

    /// <summary>
    /// Returns the active child count.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static int GetActiveChildCount(Transform obj)
    {
        int activeChildCount = 0;
        foreach (Transform child in obj) {
            if (child.gameObject.activeSelf) {
                activeChildCount++;
            }
        }
        return activeChildCount;
    }

    /// <summary>
    /// Returns a list of game active gameobjects in the child.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static List<GameObject> GetActiveChild(Transform obj)
    {
        List<GameObject> activeChildCount = new List<GameObject>();
        foreach (Transform child in obj) {
            if (child.gameObject.activeSelf) {
                activeChildCount.Add(child.gameObject);
            }
        }
        return activeChildCount;
    }

    /// <summary>
    /// Returns every letter.
    /// </summary>
    /// <returns></returns>
    public static char[] ABC()
    {
        return "ABCDEFGHIJKLMNÑOPQRSTUVWXYZ".ToCharArray();
    }
}
