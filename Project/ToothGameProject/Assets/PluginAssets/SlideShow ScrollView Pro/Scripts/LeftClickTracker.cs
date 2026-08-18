using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class LeftClickTracker : MonoBehaviour, IPointerClickHandler
{
    #region IPointerClickHandler implementation

    public void OnPointerClick(PointerEventData pointerData)
    {
        if (pointerData.button == PointerEventData.InputButton.Right) {
            Debug.Log("Left Click detected");
        }
    }

    #endregion
}

