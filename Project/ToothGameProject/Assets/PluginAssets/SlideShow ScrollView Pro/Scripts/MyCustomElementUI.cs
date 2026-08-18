using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.SceneManagement;

public class MyCustomElementUI : MonoBehaviour
{
    [Header("Element Values")]

    public SlideShowScrollViewPro_Group actualGroup;

    public int elementPos;
    public int elementID;
    public Button button;
    public Image image;

    [Header("My Values")]
    // Data Variables
    public TMP_Text title;
    public TMP_Text version;

    public MyCustomData myCustomData;

    private SlideShowScrollViewPro_Scroll m_Scroll;

    private void Awake()
    {
        if(m_Scroll == null)
        {
            m_Scroll = this.GetComponentInParent<SlideShowScrollViewPro_Scroll>();
        }
    }
    // Fix rotation
    void Start()
    {
        if (m_Scroll == null)
        {
            m_Scroll = this.GetComponentInParent<SlideShowScrollViewPro_Scroll>();
        }
        if (m_Scroll.vertical) 
        {
            transform.localRotation = Quaternion.Euler(new Vector3(0, 0, -90));
        }
        else 
        {
            transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));
        }
    }

    /// <summary>
    /// Sets the element ID in the scroll.
    /// </summary>
    /// <param name="ID"></param>
    public void SetID(int ID)
    {
        elementID = ID;
    }

    /// <summary>
    /// Updates the texts using the saved data.
    /// </summary>
    public void UpdateTextsData ()
    {
        title.text = myCustomData.levelData;
        version.text = myCustomData.versionData;
    }



    /// <summary>
    /// Select this element, if already selected do another action.
    /// </summary>
    public void SelectThis_Click()
    {
        // If this button is already selected
        if (m_Scroll.selectedElementPos == elementPos && m_Scroll.selectedElementID == elementID) {
            button.interactable = false;

            Debug.Log("Selected again, button deactivated");

            // Example
            myCustomData.LoadLevel();
        }
        else { // If not selected yet
            SelectThisImageOnly_Click();
        }
    }

    /// <summary>
    /// Select this element only.
    /// </summary>
    public void SelectThisImageOnly_Click()
    {
        m_Scroll.SelectButtonByID_Click(elementID);
    }

    /// <summary>
    /// Set the texts color.
    /// </summary>
    /// <param name="color"></param>
    public void SetTextsColor (Color color)
    {
        title.color = color;
        version.color = color;
    }

    /// <summary>
    /// Sets the image for this element.
    /// </summary>
    /// <param name="SongImage"></param>
    public void SetImage(Sprite SongImage)
    {
        //image.color = Color.gray;
        image.sprite = SongImage;
    }

    /// <summary>
    /// Delete from list on destroy
    /// </summary>
    void OnDestroy()
    {
        if (m_Scroll.buttons.Exists(x => gameObject)) {
            m_Scroll.RecalculateActiveButtonsAndSize();
        }
    }

    private void OnDisable()
    {
        GetComponent<ShowUIWhenIsVisible>().UpdateNow();
    }

    private void OnEnable()
    {
        GetComponent<ShowUIWhenIsVisible>().UpdateNow();
    }
}
