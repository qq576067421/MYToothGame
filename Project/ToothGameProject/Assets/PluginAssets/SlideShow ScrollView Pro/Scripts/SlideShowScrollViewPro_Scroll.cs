// Created by Carlos Arturo Rodriguez Silva (Legend)
// Contact: carlosarturors@gmail.com

// using System.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SlideShowScrollViewPro_Scroll : MonoBehaviour
{

    public List<GameObject> buttons;
    public List<GameObject> activeButtons;
    [HideInInspector]
    public List<MyCustomElementUI> elements;
    [HideInInspector]
    public List<SlideShowScrollViewPro_Group> groups;

    [Header("Variables")]
    public bool vertical;
    [Space(5)]
    public bool startOnGroup = false;
    public int groupDropdownStartValue = 1;

    [Space(5)]
    public bool selectRandomAtStart = true;

    public MyCustomElementUI selectedElementAtStart;

    [Space(5)]

    [Range(0f, 3f)]
    public float buttonsFadeTime = 0.35f;

    [Range(0f, 3f)]
    public float transitionTime = 0.5f;

    [Range(0f, 3f)]
    public float backgroundFadeTime = 0.35f;

    [Range(0.1f, 3f)]
    public float onPanelChangeFadeTime = 0.2f;

    [Space(5)]

    [Range(-100, 500f)]
    public float imagesSeparation = 100f;

    [Range(-100, 500f)]
    public float groupsSeparation = 50f;

    [Space(5)]

    [Range(10, 500)]
    public float elementsHorizontalSize = 240;
    [Range(10, 500)]
    public float elementsVerticalSize = 135;

    [Space(5)]

    public Vector3 normalScale = Vector3.one;
    public Vector3 selectedScale = new Vector3(1.75f, 1.75f, 1);

    [Space(10)]

    public int selectedElementID = -1;

    public int selectedElementPos = -1;

    int centerButton;


    [Header("Prefabs")]
    public RectTransform center;
    public ScrollRect scrollRect;
    public RectTransform viewport;

    public Image backgroundImage;

    [Space(5)]
    public GameObject imageElementUI;

    [Space(5)]
    public GameObject btnReturn;
    public GameObject btnGroup;

    [Space(5)]
    public FadeCanvas imagesFadeCanvas;
    public RectTransform panelImages;
    public RectTransform parentImagesList;


    [Space(5)]
    public FadeCanvas groupsFadeCanvas;
    public RectTransform panelGroups;
    public RectTransform parentGroupsList;

    /*[Space(5)]
    public TMP_InputField searchInputField;
    public TMP_Text searchNumResults;

    public TMP_Dropdown OrderByDropDown;
    public TMP_Dropdown GroupByDropDown;

    [Header("Selected Element Data")]
    public TMP_Text elementTitle;
    public TMP_Text elementVersion;*/

    [HideInInspector]
    public float[] distance;

    bool active = false;

    int previous = -1;
    int previousSelectedButton = -2;

    float minDistance = int.MaxValue;

    int centerGroupsButton;

    GameObject leftButton;
    GameObject rightButton;

    SlideShowScrollViewPro_Group actualDisplayingGroup;

    void Start()
    {
        Restart();
        RenderAPI.SetCurrentCol(selectedElementID);
    }

    /// <summary>
    /// You must follow this method to add button after clear function
    /// </summary>
    void AddButtonAfterClear ()
    {
        CreateButtonOnRuntime1();

        UpdateAll();
     
        LerpToNearestButton();
    }

    /// <summary>
    /// Clears all buttons
    /// </summary>
    public void ClearButtons()
    {
        foreach (GameObject button in buttons) {
            Destroy(button);
        }

        buttons.Clear();

        activeButtons.Clear();

        selectedElementID = -1;
        selectedElementPos = -1;
        previous = -1;
        previousSelectedButton = -2;

        leftButton = null;
        rightButton = null;

        Debug.Log("Buttons cleared");
    }

    /// <summary>
    /// Restart the script.
    /// </summary>
    public void Restart()
    {
        // Create groups
        CreateGroups();

        // Deactivate groups
        for (int i = 0; i < groups.Count; i++) {
            groups[i].gameObject.SetActive(false);
        }

        groupsFadeCanvas.FadeOut();

        // Set parent spacing
        parentGroupsList.GetComponent<HorizontalLayoutGroup>().spacing = groupsSeparation;
        parentImagesList.GetComponent<HorizontalLayoutGroup>().spacing = imagesSeparation;

        // Change viewport rotation
        if (vertical) {
            viewport.transform.localRotation = Quaternion.Euler(0, 0, 90);
        }
        else {
            viewport.transform.localRotation = Quaternion.Euler(0, 0, 0);
        }

        groupsFadeCanvas.FadeTime = onPanelChangeFadeTime;
        imagesFadeCanvas.FadeTime = onPanelChangeFadeTime;

        // Initiate all
        Initiate();

        //// Example you don't need this
        // Create extra buttons
        /*CreateButtonOnRuntime1();
        CreateButtonOnRuntime2();*/

        // Call this when after you added all your buttons in runtime
        UpdateAll();

        //// Example you don't need this
    }

    void CreateButtonOnRuntime1()
    {
        // Create extra button
        var newButton = Instantiate(imageElementUI, parentImagesList);

        newButton.GetComponent<LayoutElement>().preferredHeight = elementsVerticalSize;
        newButton.GetComponent<LayoutElement>().preferredWidth = elementsHorizontalSize;

        newButton.GetComponent<MyCustomElementUI>().myCustomData.levelData = "Another Level";
        newButton.GetComponent<MyCustomElementUI>().myCustomData.versionData = "Instantiated in runtime button";
        // newButton.GetComponent<MyCustomElementUI>().SetImage(mySprite);
        newButton.GetComponent<MyCustomElementUI>().UpdateTextsData();

        buttons.Add(newButton);

        Debug.Log("Button created in runtime");
    }

    void CreateButtonOnRuntime2()
    {
        // Create another button
        var anotherButton = Instantiate(imageElementUI, parentImagesList);

        anotherButton.GetComponent<LayoutElement>().preferredHeight = elementsVerticalSize;
        anotherButton.GetComponent<LayoutElement>().preferredWidth = elementsHorizontalSize;

        anotherButton.GetComponent<MyCustomElementUI>().myCustomData.levelData = "0 Zero";
        anotherButton.GetComponent<MyCustomElementUI>().myCustomData.versionData = "Instantiated in runtime button";
        // anotherButton.GetComponent<MyCustomElementUI>().SetImage(mySprite);
        anotherButton.GetComponent<MyCustomElementUI>().UpdateTextsData();

        buttons.Add(anotherButton);

        Debug.Log("Button created in runtime");
    }

    /// <summary>
    /// Recalculate the parent size, recalculate active buttons, and recalculate the center button.
    /// </summary>
    /// <param name="extraButtons"></param>
    public void RecalculateActiveButtonsAndSize()
    {

        int extraButtons = 0;
        if (leftButton != null) {
            extraButtons = 1;
        }

        if (rightButton != null) {
            extraButtons = 2;
        }

        // Set the correct size
        parentImagesList.sizeDelta = new Vector2((buttons.Count + extraButtons) * (elementsHorizontalSize + imagesSeparation), elementsVerticalSize);

        // Calculate active buttons
        activeButtons = CalculateActiveButtons();

        // Calculate center button
        centerButton = (int)(Math.Round(activeButtons.Count / 2f, MidpointRounding.AwayFromZero) - 1);

        if (activeButtons.Count % 2 == 0) {
            parentImagesList.anchoredPosition = new Vector2(elementsHorizontalSize / 2f, parentImagesList.anchoredPosition.y);
        }
        else {
            parentImagesList.anchoredPosition = new Vector2(0, parentImagesList.anchoredPosition.y);
        }

    }

    /// <summary>
    /// Updates all, call this function when you after added a new button in runtime
    /// </summary>
    public void UpdateAll()
    {
        // Update all
        RecalculateActiveButtonsAndSize();

        elements.Clear();

        if (buttons.Count >= 1) {
            // Set the ID to all buttons and assign to elements
            for (int i = 0; i < buttons.Count; i++) {
                buttons[i].GetComponent<MyCustomElementUI>().SetID(i);
                elements.Add(buttons[i].GetComponent<MyCustomElementUI>());
            }

            if (selectedElementID != -1) {
                if (buttons[selectedElementID].gameObject.activeSelf) {
                    StopCoroutine("IE_LerpToSelectedButton");
                    StartCoroutine("IE_LerpToSelectedButton");
                }
                else {
                    parentImagesList.anchoredPosition = new Vector2(0, parentImagesList.anchoredPosition.y);
                }
            }


            for (int i = 0; i < activeButtons.Count; i++) {
                activeButtons[i].GetComponent<ShowUIWhenIsVisible>().UpdateNow();
            }
        }

        ShowVisibleActiveButtons();
    }

    /// <summary>
    /// Creates the groups.
    /// </summary>
    void CreateGroups()
    {
        groups.Clear();

        // Create group for numbers
        var groupClone = Instantiate(btnGroup, parentGroupsList);

        groupClone.GetComponent<SlideShowScrollViewPro_Group>().indexNumber = 0;
        groupClone.GetComponent<SlideShowScrollViewPro_Group>().GroupName = "0-9";

        groups.Add(groupClone.GetComponent<SlideShowScrollViewPro_Group>());

        // Create one group for every letter
        char[] alpha = SlideShowScrollViewPro_Utilities.ABC();

        for (int i = 0; i < alpha.Length; i++) {
            groupClone = Instantiate(btnGroup, parentGroupsList);
            groupClone.GetComponent<SlideShowScrollViewPro_Group>().GroupName = alpha[i].ToString();
            groupClone.GetComponent<SlideShowScrollViewPro_Group>().indexNumber = i + 1;
            groups.Add(groupClone.GetComponent<SlideShowScrollViewPro_Group>());

        }

        // Create group for any other character
        groupClone = Instantiate(btnGroup, parentGroupsList);
        groupClone.GetComponent<SlideShowScrollViewPro_Group>().GroupName = "Others";
        groups.Add(groupClone.GetComponent<SlideShowScrollViewPro_Group>());

        parentGroupsList.sizeDelta = new Vector2(groups.Count * (elementsHorizontalSize + groupsSeparation), elementsVerticalSize);
    }

    /// <summary>
    /// Assign an Element to a group
    /// </summary>
    /// <param name="elementData"></param>
    /// <param name="buttonObject"></param>
    void AssignToGroup(MyCustomElementUI elementData, GameObject buttonObject)
    {
        Char firstLetter = ' ';

        Char.ToUpper(firstLetter);

        bool got = false;

        // If the letter is a number
        if (Char.IsNumber(firstLetter)) {
            for (int i = 0; i < groups.Count; i++) {
                if (groups[i].GroupName == "0-9") {
                    buttonObject.GetComponent<MyCustomElementUI>().actualGroup = groups[i];
                    groups[i].IncreaseElementsCount();
                    got = true;
                    break;
                }
            }
        }

        if (!got) {
            // Search for group with the same letter and assign to it
            for (int i = 0; i < groups.Count; i++) {
                if (groups[i].GroupName == firstLetter.ToString()) {
                    buttonObject.GetComponent<MyCustomElementUI>().actualGroup = groups[i];
                    groups[i].IncreaseElementsCount();
                    got = true;
                    break;
                }
            }
        }

        if (!got) {
            // If the letter doesn't exists then add to Others group
            for (int i = 0; i < groups.Count; i++) {
                if (groups[i].GroupName == "Others") {
                    buttonObject.GetComponent<MyCustomElementUI>().actualGroup = groups[i];
                    groups[i].GetComponent<SlideShowScrollViewPro_Group>().indexNumber = SlideShowScrollViewPro_Utilities.ABC().Length + 2;
                    groups[i].IncreaseElementsCount();
                    got = true;
                    break;
                }

            }
        }

        parentGroupsList.sizeDelta = new Vector2(SlideShowScrollViewPro_Utilities.GetActiveChildCount(parentGroupsList) * (elementsHorizontalSize + groupsSeparation), elementsVerticalSize);

        centerGroupsButton = (int)(Math.Round(SlideShowScrollViewPro_Utilities.GetActiveChildCount(parentGroupsList) / 2f, MidpointRounding.AwayFromZero) - 1);
        
    }

    /// <summary>
    /// Initialize all
    /// </summary>
    /// <returns></returns>
    void Initiate()
    {
        buttons.Clear();

        // Get children gameobjects and set initial element values
        var children = SlideShowScrollViewPro_Utilities.GetChildren(parentImagesList.gameObject).ToList();

        for (int i = 0; i < children.Count; i++) {
            children[i].GetComponent<LayoutElement>().preferredHeight = elementsVerticalSize;
            children[i].GetComponent<LayoutElement>().preferredWidth = elementsHorizontalSize;
            children[i].GetComponent<MyCustomElementUI>().gameObject.transform.localScale = normalScale;
            //children[i].GetComponent<MyCustomElementUI>().image.color = Color.gray;
            //children[i].GetComponent<MyCustomElementUI>().SetTextsColor(Color.gray);

            buttons.Add(children[i]);
        }

        distance = new float[buttons.Count];

        elements.Clear();

        // Set position to all buttons and assign to elements
        for (int i = 0; i < buttons.Count; i++) {
            buttons[i].GetComponent<MyCustomElementUI>().SetID(i);
            elements.Add(buttons[i].GetComponent<MyCustomElementUI>());
        }

        RecalculateActiveButtonsAndSize();

        active = true;

        // Start first lerp to button
        if (!selectRandomAtStart) {

            if (selectedElementAtStart == null) {
                Debug.LogWarning("Please assign SelectedElementAtStart prefab; Selecting random button");
                StartSelectRandom();
            }
            else {
                StartCoroutine(IE_InitializeWithElement());
            }
        }
        else {
            StartSelectRandom();
        }

        if (startOnGroup) {
            GroupBy_Click();
        }
        else {
            imagesFadeCanvas.FadeIn();
            groupsFadeCanvas.FadeOut();
        }

    }

    /// <summary>
    /// Selects Random button
    /// </summary>
    public void SelectRandom (int idx = int.MinValue)
    {
        selectedElementID = idx != int.MinValue ? idx : UnityEngine.Random.Range(0, buttons.Count - 1);
        selectedElementPos = buttons[selectedElementID].GetComponent<MyCustomElementUI>().elementPos;

        ShowElementDataNow();

        StartCoroutine("IE_LerpToSelectedButton");
    }

    /// <summary>
    /// Selects a random button and resets the previous selected button.
    /// </summary>
    void StartSelectRandom()
    {
        SelectRandom(2);

        previous = selectedElementID;
        previousSelectedButton = selectedElementID;

        ShowElementDataNow();

        StartCoroutine("IE_LerpToSelectedButton");
    }


    /// <summary>
    /// Changes the background sprite smooth using Lerp
    /// </summary>
    /// <returns></returns>
    IEnumerator ChangeBackgroundSprite()
    {
        yield break;
        float time = 0;

        var actualColor = backgroundImage.color;
        var newColor = actualColor;
        newColor.a = 0;


        while (time <= backgroundFadeTime) {
            time += Time.unscaledDeltaTime;
            backgroundImage.color = Color.Lerp(actualColor, newColor, time / backgroundFadeTime);

            yield return new WaitForEndOfFrame();
        }

        if (elements[selectedElementID].image.sprite == null) {
            backgroundImage.sprite = null;

        }
        else {
            backgroundImage.sprite = elements[selectedElementID].image.sprite;
        }

        backgroundImage.color = newColor;

        actualColor = backgroundImage.color;
        newColor.a = 1;

        time = 0;

        while (time <= backgroundFadeTime) {
            time += Time.unscaledDeltaTime;

            backgroundImage.color = Color.Lerp(actualColor, newColor, time / backgroundFadeTime);

            yield return new WaitForEndOfFrame();
        }

        backgroundImage.color = newColor;
    }

    /// <summary>
    /// Initializes with an element, show his data and lerps to him.
    /// </summary>
    /// <returns></returns>
    IEnumerator IE_InitializeWithElement()
    {
        selectedElementID = selectedElementAtStart.elementID;

        selectedElementPos = buttons[selectedElementID].GetComponent<MyCustomElementUI>().elementPos;

        StopCoroutine("ChangeBackgroundSprite");

        StartCoroutine("ChangeBackgroundSprite");

        previous = selectedElementID;
        previousSelectedButton = selectedElementID;

        StartCoroutine(IE_FadeIn());

        StartCoroutine("IE_LerpToSelectedButton");

        yield break;
    }

    /// <summary>
    /// Selects the specified button.
    /// </summary>
    /// <param name="elementPos"></param>
    /// <param name="elementID"></param>
    public void SelectButtonByID_Click(int elementID)
    {
        if(elements == null || elements.Count <= elementID || elementID < 0)
        {
            return;
        }
        selectedElementPos = elements[elementID].elementPos;
        selectedElementID = elementID;

        StopCoroutine("IE_LerpToSelectedButton");
        StartCoroutine("IE_LerpToSelectedButton");

        OnSelectionChange();
    }

    string lastText = "";
    /// <summary>
    /// Search elements using the inputfield text.
    /// </summary>
    public void SearchElements_Click()
    {
        if (buttons.Count == 0) {
            Debug.Log("No buttons to search");
            return;
        }

        // Hide all groups
        for (int i = 0; i < groups.Count; i++) {
            groups[i].ElementsCount = 0;
            groups[i].gameObject.SetActive(false);
        }

        int results = 0;

        // Search for elements
        for (int pos = 0; pos < elements.Count; pos++) {
            if (Regex.IsMatch(elements[pos].myCustomData.levelData,
                  "",
                  RegexOptions.IgnoreCase)) {

                // Show the button if we are displaying that group
                if (elements[pos].actualGroup == actualDisplayingGroup) {
                    elements[pos].gameObject.SetActive(true);
                }

                if (elements[pos].actualGroup != null) {
                    elements[pos].actualGroup.ElementsCount++;
                    elements[pos].actualGroup.gameObject.SetActive(true);
                }

                // Increase results
                results++;
                continue;
            }

            if (Regex.IsMatch(elements[pos].myCustomData.versionData,
                  "",
                  RegexOptions.IgnoreCase)) {

                // Show the button if we are displaying that group
                if (elements[pos].actualGroup == actualDisplayingGroup) {
                    elements[pos].gameObject.SetActive(true);
                }

                if (elements[pos].actualGroup != null) {
                    elements[pos].actualGroup.ElementsCount++;
                    elements[pos].actualGroup.gameObject.SetActive(true);
                }

                // Increase results
                results++;
                continue;
            }

            buttons[pos].SetActive(false);
        }

        // Recalculate
        RecalculateActiveButtonsAndSize();
        selectedElementPos = buttons[selectedElementID].GetComponent<MyCustomElementUI>().elementPos;
        centerGroupsButton = (int)(Math.Round(SlideShowScrollViewPro_Utilities.GetActiveChildCount(parentGroupsList) / 2f, MidpointRounding.AwayFromZero) - 1);

        ShowVisibleActiveButtons();
    }

    /// <summary>
    /// Orders the image elements list.
    /// </summary>
    public void OrderBy_Click()
    {
        if (buttons.Count == 0) {
            Debug.Log("No buttons to Order");
            return;
        }

        var orderedList = new List<MyCustomElementUI>();

        // Order buttons
        for (int pos = 0; pos < orderedList.Count; pos++) {
            buttons[orderedList[pos].elementID].GetComponent<MyCustomElementUI>().elementPos = pos;
            buttons[orderedList[pos].elementID].transform.SetSiblingIndex(pos);
        }

       if (leftButton != null) {
           leftButton.transform.SetAsFirstSibling();
        }

        if (rightButton != null) {
            rightButton.transform.SetAsLastSibling();            
        }

        if (panelGroups.gameObject.activeSelf) {
            // Reset the groups to the initial state
            for (int i = 0; i < groups.Count; i++) {
                groups[i].transform.SetParent(parentGroupsList);
                groups[i].transform.SetSiblingIndex(groups[i].indexNumber);
            }
        }

        // Lerp to actual selected button
        RecalculateActiveButtonsAndSize();

        selectedElementPos = buttons[selectedElementID].GetComponent<MyCustomElementUI>().elementPos;

        if (buttons[selectedElementID].gameObject.activeSelf) {
            StopCoroutine("IE_LerpToSelectedButton");
            StartCoroutine("IE_LerpToSelectedButton");
        } else {
            parentImagesList.anchoredPosition = new Vector2(0, parentImagesList.anchoredPosition.y);
        }

        ShowVisibleActiveButtons();
    }

    /// <summary>
    /// Groups the elements and Show/Hide groups panel
    /// </summary>
    public void GroupBy_Click()
    {
        if (buttons.Count == 0) {
            Debug.Log("No buttons to Group");
            return;
        }

        DestroyReturnButtons();

        // Reset groups positions
        for (int i = 0; i < groups.Count; i++) {
            groups[i].ResetGroup();

            groups[i].transform.SetParent(parentGroupsList);
            groups[i].transform.SetSiblingIndex(groups[i].indexNumber);
        }

        Debug.Log("No grouping");

        imagesFadeCanvas.FadeIn();
        groupsFadeCanvas.FadeOut();

        for (int i = 0; i < elements.Count; i++)
        {
            elements[i].GetComponent<MyCustomElementUI>().actualGroup = null;
        }

        selectedElementPos = buttons[selectedElementID].GetComponent<MyCustomElementUI>().elementPos;

        actualDisplayingGroup = null;

        selectedElementPos = buttons[selectedElementID].GetComponent<MyCustomElementUI>().elementPos;

        SearchElements_Click();    

        if (buttons[selectedElementID].gameObject.activeSelf) {
            StopCoroutine("IE_LerpToSelectedButton");
            StartCoroutine("IE_LerpToSelectedButton");
        } else {
            parentImagesList.anchoredPosition = new Vector2(0, parentImagesList.anchoredPosition.y);
        }

        ShowVisibleActiveButtons();
    }

    /// <summary>
    /// Show the elements from the specified group.
    /// </summary>
    /// <param name="group"></param>
    public void ShowElementsFromGroup(SlideShowScrollViewPro_Group group)
    {
        // Reset the groups to the initial state
        for (int i = 0; i < groups.Count; i++) {
            groups[i].transform.SetParent(parentGroupsList);
            groups[i].transform.SetSiblingIndex(groups[i].indexNumber);
        }

        // If we pressed a return button, destroy the return buttons
        if (group.returnButton) {
            imagesFadeCanvas.FadeOut();
            groupsFadeCanvas.FadeIn();
            DestroyReturnButtons();
            actualDisplayingGroup = null;
            parentGroupsList.anchoredPosition = new Vector2(0, parentGroupsList.anchoredPosition.y);
            return;
        }

        actualDisplayingGroup = group;

        DestroyReturnButtons();

        leftButton = null;
        rightButton = null;

        bool atLeastOne = false;

        // Check if the elements belong to the group if not, deactivate it
        for (int i = 0; i < buttons.Count; i++) {
            if (buttons[i].GetComponent<MyCustomElementUI>().actualGroup == group) {
                buttons[i].SetActive(true);
                atLeastOne = true;
            }
            else {
                buttons[i].SetActive(false);
            }
        }

        // Show left corner button
        // If you found at least one, the elements are displayed and groups are hidden
        if (atLeastOne) {
            imagesFadeCanvas.FadeIn();
            groupsFadeCanvas.FadeOut();

            OrderBy_Click();

            // Add groups to the corners
            // Check if the group to be selected is valid
            if ((group.indexNumber) <= 0) { // If it is less than 0 then there are no more groups left
                // Show return button      
                leftButton = Instantiate(btnReturn, parentImagesList);
                leftButton.transform.SetAsFirstSibling();

                // Fix rotation
                if (vertical) {
                    leftButton.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, -90));
                }
                else {
                    leftButton.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));
                }

            }
            else { // If it is valid, move the button of the previous letter to the selected one
                   // If that group has no elements check the following
                int actualPos = 1;
                bool hasElements = false;
                do {
                    if (groups[group.indexNumber - actualPos].ElementsCount <= 0) {
                        actualPos++;
                    }
                    else {
                        hasElements = true;
                    }

                    // If there is no more, exit the cycle
                    if ((group.indexNumber - actualPos) < 0) {
                        break;
                    }
                }
                while ((SlideShowScrollViewPro_Utilities.TryGetElement(groups.ToArray(), actualPos) != null) && !hasElements);

                if (hasElements) {
                    leftButton = groups[group.indexNumber - actualPos].gameObject;
                    leftButton.transform.SetParent(parentImagesList);
                    leftButton.transform.SetAsFirstSibling();
                }
                else {
                    // Show return button      
                    leftButton = Instantiate(btnReturn, parentImagesList);
                    leftButton.transform.SetAsFirstSibling();

                    // Fix rotation
                    if (vertical) {
                        leftButton.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, -90));
                    }
                    else {
                        leftButton.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));
                    }
                }
            }

            // Show right corner button
            if ((group.indexNumber + 2) > (groups.Count - 1)) { // If it is less than 0 then there are no more groups right
                rightButton = Instantiate(btnReturn, parentImagesList);
                rightButton.transform.SetAsLastSibling();

                if (vertical) {
                    rightButton.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, -90));
                }
                else {
                    rightButton.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));
                }
            }
            else {

                int actualPos = 1;

                bool hasElements = false;
                do {
                    if (groups[group.indexNumber + actualPos].ElementsCount <= 0) {
                        actualPos++;
                    }
                    else {
                        hasElements = true;
                    }

                    if ((group.indexNumber + actualPos) > groups.Count - 1) {
                        break;
                    }
                }
                while ((SlideShowScrollViewPro_Utilities.TryGetElement(groups.ToArray(), actualPos) != null) && !hasElements);

                if (hasElements) {
                    rightButton = groups[group.indexNumber + actualPos].gameObject;
                    rightButton.transform.SetParent(parentImagesList);
                    rightButton.transform.SetAsLastSibling();
                }
                else {
                    rightButton = Instantiate(btnReturn, parentImagesList);
                    rightButton.transform.SetAsLastSibling();

                    if (vertical) {
                        rightButton.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, -90));
                    }
                    else {
                        rightButton.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));
                    }
                }
            }
        }
        else { // If not, the elements are hidden and the group phase is returned (this should never happen anyway)
            imagesFadeCanvas.FadeOut();
            groupsFadeCanvas.FadeIn();

            actualDisplayingGroup = null;

            for (int i = 0; i < groups.Count; i++) {
                groups[i].transform.SetParent(parentGroupsList);
                groups[i].transform.SetSiblingIndex(groups[i].indexNumber);
            }
        }

        SearchElements_Click();
        RecalculateActiveButtonsAndSize();

        if (buttons[selectedElementID].gameObject.activeSelf) {
            selectedElementPos = buttons[selectedElementID].GetComponent<MyCustomElementUI>().elementPos;

            StopCoroutine("IE_LerpToSelectedButton");
            StartCoroutine("IE_LerpToSelectedButton");
        }
        else {
            selectedElementPos = -1;
            parentImagesList.anchoredPosition = new Vector2(0, parentImagesList.anchoredPosition.y);
        }

        ShowVisibleActiveButtons();
    }

    /// <summary>
    /// On selection shange, show element data.
    /// </summary>
    void OnSelectionChange()
    {
        if (previousSelectedButton != selectedElementID) {

            previous = previousSelectedButton;
            previousSelectedButton = selectedElementID;

            selectedElementPos = buttons[selectedElementID].GetComponent<MyCustomElementUI>().elementPos;

            RenderAPI.SetCurrentCol(selectedElementID);
            Debug.Log("Selection changed");

            ShowElementDataNow();
        }
    }

    /// <summary>
    /// Show element data, fade previous button and change background.
    /// </summary>
    void ShowElementDataNow()
    {

        if (!active) {
            return;
        }

        StartCoroutine(IE_FadeOutPreviousButton());

        StopCoroutine("ChangeBackgroundSprite");
        StartCoroutine("ChangeBackgroundSprite");

        Debug.Log("Showing element data");

        StartCoroutine(IE_FadeIn());
    }

    /// <summary>
    /// Lerp to the selected button.
    /// </summary>
    /// <returns></returns>
    IEnumerator IE_LerpToSelectedButton()
    {
        // Disable scroll
        scrollRect.horizontal = false;

        if (activeButtons.Count == 0) {
            yield break;
        }

        centerButton = (int)(Math.Round(activeButtons.Count / 2f, MidpointRounding.AwayFromZero) - 1);

        int pos = centerButton - buttons[selectedElementID].GetComponent<MyCustomElementUI>().elementPos;

        float newPosition = pos * (elementsHorizontalSize + imagesSeparation);

        //	Debug.Log ("Selected button: " + selectedButton);
        //
        //	Debug.Log ("Center button: " + centerButton);
        //
        //	Debug.Log ("Selected button pos: " + selectedButtonPos + " - " + "Pos: " + pos);
        //
        //	Debug.Log (newPosition);

        if (activeButtons.Count % 2 == 0) {
            newPosition += (elementsHorizontalSize / 2f) + (imagesSeparation / 2f);
        }

        float actualPosition = parentImagesList.anchoredPosition.x;

        float time = 0f;

        while (time < 1) {

            // Execute the function every 10 frames to no impact the performance
            if (Time.frameCount % 10 == 0) {
                ShowVisibleActiveButtons();
            }

            time += Time.unscaledDeltaTime / transitionTime;

            // Smooth Ease In/Out Lerp
            float t = time;
            t = Mathf.Sin(t * Mathf.PI * 0.5f);

            float newX = (Mathf.Lerp(actualPosition, newPosition, t));
            Vector2 panelNewPosition = new Vector2(newX, parentImagesList.anchoredPosition.y);

            parentImagesList.anchoredPosition = panelNewPosition;

            yield return new WaitForEndOfFrame();
        }

        Vector2 panelNewPosition2 = new Vector2(newPosition, parentImagesList.anchoredPosition.y);

        parentImagesList.anchoredPosition = panelNewPosition2;

        // Enable scroll
        scrollRect.horizontal = true;

        ShowVisibleActiveButtons();

        yield break;
    }

    /// <summary>
    /// Fade the previous button.
    /// </summary>
    /// <returns></returns>
    IEnumerator IE_FadeOutPreviousButton()
    {
        if (previous <= -1) {
            yield break;
        }

        int button = previous;

        float time = 0;
        var actualColor = buttons[button].GetComponent<MyCustomElementUI>().image.color;
        var actualScale = buttons[button].GetComponent<MyCustomElementUI>().gameObject.GetComponent<RectTransform>().localScale;
        var actualTextColor = buttons[button].GetComponent<MyCustomElementUI>().title.color;

        while (time < 1) {
            time += Time.unscaledDeltaTime / buttonsFadeTime;

            /*buttons[button].GetComponent<MyCustomElementUI>().image.color = Color.Lerp(actualColor,
              Color.gray,
              time);*/


            buttons[button].GetComponent<MyCustomElementUI>().gameObject.GetComponent<RectTransform>().localScale = Vector3.Lerp(actualScale,
              normalScale,
              time);


            //buttons[button].GetComponent<MyCustomElementUI>().SetTextsColor(Color.Lerp(actualTextColor, Color.gray, time));


            yield return new WaitForEndOfFrame();
        }
    }

    /// <summary>
    /// Fade in the selected button.
    /// </summary>
    /// <returns></returns>
    IEnumerator IE_FadeIn()
    {
        float time = 0;
        var actualColor = buttons[selectedElementID].GetComponent<MyCustomElementUI>().image.color;
        var actualScale = buttons[selectedElementID].GetComponent<MyCustomElementUI>().gameObject.GetComponent<RectTransform>().localScale;
        var actualTextColor = buttons[selectedElementID].GetComponent<MyCustomElementUI>().title.color;

        while (time < 1) {
            time += Time.unscaledDeltaTime / buttonsFadeTime;

            buttons[selectedElementID].GetComponent<MyCustomElementUI>().image.color = Color.Lerp(actualColor, Color.white, time);


            buttons[selectedElementID].GetComponent<MyCustomElementUI>().gameObject.GetComponent<RectTransform>().localScale = Vector3.Lerp(actualScale,
              selectedScale,
              time);


            buttons[selectedElementID].GetComponent<MyCustomElementUI>().SetTextsColor(Color.Lerp(actualTextColor, Color.white, time));


            yield return new WaitForEndOfFrame();
        }

    }

    /// <summary>
    /// Selects Down or Left Button
    /// </summary>
    public void SelectDownOrLeftButton_Click()
    {
        if (!active || !panelImages.gameObject.activeSelf || activeButtons.Count == 0) {
            return;
        }

        // Get the actual position
        int actualPos = 0;
        for (int i = 0; i < activeButtons.Count; i++) {
            if (activeButtons[i].GetComponent<MyCustomElementUI>().elementPos == selectedElementPos) {
                actualPos = i;
                break;
            }
        }

        if (actualPos - 1 >= 0) {
            for (int i = 0; i < activeButtons.Count; i++) {
                if (activeButtons[i].GetComponent<MyCustomElementUI>().elementPos == selectedElementPos - 1) {
                    actualPos = activeButtons[i].GetComponent<MyCustomElementUI>().elementID;
                    break;
                }
            }

            selectedElementID = actualPos;
        }
        else {
            selectedElementID = activeButtons[activeButtons.Count - 1].GetComponent<MyCustomElementUI>().elementID;
        }

        StopCoroutine("IE_LerpToSelectedButton");
        StartCoroutine("IE_LerpToSelectedButton");

        OnSelectionChange();
    }

    /// <summary>
    /// Selects Up or Right Button
    /// </summary>
    public void SelectUpOrRightButton_Click()
    {
        if (!active || !panelImages.gameObject.activeSelf || activeButtons.Count == 0) {
            return;
        }

        int actualPos = 0;
        for (int i = 0; i < activeButtons.Count; i++) {
            if (activeButtons[i].GetComponent<MyCustomElementUI>().elementPos == selectedElementPos) {
                actualPos = i;
                break;
            }
        }

        if (actualPos + 1 > activeButtons.Count - 1) {
            selectedElementID = activeButtons[0].GetComponent<MyCustomElementUI>().elementID;
        }
        else {
            for (int i = 0; i < activeButtons.Count; i++) {
                if (activeButtons[i].GetComponent<MyCustomElementUI>().elementPos == selectedElementPos + 1) {
                    actualPos = activeButtons[i].GetComponent<MyCustomElementUI>().elementID;
                    break;
                }
            }

            selectedElementID = actualPos;
        }

        StopCoroutine("IE_LerpToSelectedButton");
        StartCoroutine("IE_LerpToSelectedButton");

        OnSelectionChange();
    }

    /// <summary>
    /// Calculates the active buttons.
    /// </summary>
    /// <returns></returns>
    public List<GameObject> CalculateActiveButtons()
    {

        var list = SlideShowScrollViewPro_Utilities.GetChildren(parentImagesList.gameObject, false).ToList();

//        Debug.LogFormat("List count: {0}", list.Count);

        int activeObjectCount = 0;

        List<GameObject> newList = new List<GameObject>();
        for (int i = 0; i < list.Count; i++) {
            if (list[i].activeSelf) {

                if (list[i].GetComponent<MyCustomElementUI>() != null) {
                    list[i].GetComponent<MyCustomElementUI>().elementPos = activeObjectCount++;
                    newList.Add(list[i]);
                }

            }
        }
        return newList;
    }

    /// <summary>
    /// Lerp to the nearest button
    /// </summary>
    void LerpToNearestButton()
    {

        if (activeButtons.Count == 0) {
            return;
        }

        minDistance = int.MaxValue;

        for (int i = 0; i < activeButtons.Count; i++) {

            float actualDistance = Vector2.Distance(activeButtons[i].GetComponent<RectTransform>().position, center.position);

            if (actualDistance <= minDistance) {
                minDistance = actualDistance;
                selectedElementID = activeButtons[i].GetComponent<MyCustomElementUI>().elementID;
                selectedElementPos = activeButtons[i].GetComponent<MyCustomElementUI>().elementPos;
            }
        }

        StopCoroutine("IE_LerpToSelectedButton");
        StartCoroutine("IE_LerpToSelectedButton");

        OnSelectionChange();
    }

    void Update()
    {
        // Set images scroll limits
        if (panelImages.gameObject.activeSelf) {
            var value = (activeButtons.Count - 1 - centerButton) * (elementsHorizontalSize + imagesSeparation);

            if (parentImagesList.anchoredPosition.x > value) {
                parentImagesList.anchoredPosition = new Vector2(value, parentImagesList.anchoredPosition.y);
            }
            else if (parentImagesList.anchoredPosition.x < -value) {
                parentImagesList.anchoredPosition = new Vector2(-value, parentImagesList.anchoredPosition.y);
            }
        }

        // Set group scroll limits
        if (panelGroups.gameObject.activeSelf) {
            var value = (SlideShowScrollViewPro_Utilities.GetActiveChildCount(parentGroupsList) - 1 - centerGroupsButton) * (elementsHorizontalSize + imagesSeparation);

            if (parentGroupsList.anchoredPosition.x > value) {
                parentGroupsList.anchoredPosition = new Vector2(value, parentGroupsList.anchoredPosition.y);
            }
            else if (parentGroupsList.anchoredPosition.x < -value) {
                parentGroupsList.anchoredPosition = new Vector2(-value, parentGroupsList.anchoredPosition.y);
            }
        }

        // Execute the function every 10 frames to no impact the performance
        if (Time.frameCount % 10 == 0) {
            if (updateVisibleObjects) {
                ShowVisibleActiveButtons();
            }
        }
    }

    /// <summary>
    /// Destroy return buttons
    /// </summary>
    void DestroyReturnButtons()
    {

        if (leftButton != null) {
            if (leftButton.GetComponent<SlideShowScrollViewPro_Group>().returnButton) {
                Destroy(leftButton);
            }
        }

        if (rightButton != null) {
            if (rightButton.GetComponent<SlideShowScrollViewPro_Group>().returnButton) {
                Destroy(rightButton);
            }
        }
    }

    /// <summary>
    /// Shows the visible by camera active buttons and deactivate not visible
    /// </summary>
    public void ShowVisibleActiveButtons()
    {
        if (activeButtons.Count == 0) {
            return;
        }
        for (int i = 0; i < activeButtons.Count; i++) {
            activeButtons[i].GetComponent<ShowUIWhenIsVisible>().UpdateNow();
        }

    }

    bool updateVisibleObjects; 

    /// <summary>
    /// On Start Drag
    /// </summary>
    public void StartDrag()
    {
        center.GetComponent<Image>().enabled = true;

        StopCoroutine("IE_LerpToSelectedButton");
        scrollRect.horizontal = true;

        updateVisibleObjects = true;
    }


    /// <summary>
    /// On End Drag
    /// </summary>
    public void EndDrag()
    {
        center.GetComponent<Image>().enabled = false;

        active = true;
        LerpToNearestButton();

        updateVisibleObjects = false;
    }

    /// <summary>
    /// Update visible gameobjects on scroll
    /// </summary>
    public void OnScroll ()
    {
        ShowVisibleActiveButtons();
    }
}











