using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShowUIWhenIsVisible : MonoBehaviour {

    public GameObject content;
    public RectTransform rectTransform;
    public void UpdateNow()
    {
        /*bool isFullyVisible = rectTransform.IsVisibleFrom(Camera.main);
        if (isFullyVisible) {
            content.SetActive(true);
        }
        else {
            content.SetActive(false);
        }*/
    }

    //void Update ()
    //{
    //    bool isFullyVisible = rectTransform.IsVisibleFrom(Camera.main);
    //    if (isFullyVisible) {
    //        content.SetActive(true);
    //    }
    //    else {
    //        content.SetActive(false);
    //    }
    //}
}
