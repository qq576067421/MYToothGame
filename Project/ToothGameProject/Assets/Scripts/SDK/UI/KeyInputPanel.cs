using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KeyInputPanel : MainScene
{
    [SerializeField]
    private Image[] images; // 0:Left, 1:Right, 2:Up, 3:Down, 4:Confirm

    private Coroutine[] activeCoroutines;

    private void Awake()
    {
        if (images != null)
        {
            activeCoroutines = new Coroutine[images.Length];
        }
    }

    protected override void OnLeftArrowPressed()
    {
        StartFlash(0);
    }

    protected override void OnRightArrowPressed()
    {
        StartFlash(1);
    }

    protected override void OnUpArrowPressed()
    {
        StartFlash(2);
    }

    protected override void OnDownArrowPressed()
    {
        StartFlash(3);
    }

    protected override void OnButtonOKPressed()
    {
        StartFlash(4);
    }

    protected override void OnButtonJoystickButton1Pressed()
    {
        Debug.Log("asddffsdf 按下了扳机键！！！");
        StartFlash(5);
    }

    private void StartFlash(int index)
    {
        if (images == null || index < 0 || index >= images.Length) return;
        Debug.Log("asddffsdf 按下了扳机键！！！ AAAAAA");
        Image img = images[index];
        if (img == null) return;
        Debug.Log("asddffsdf 按下了扳机键！！！ BBBBBBBB");
        if (activeCoroutines[index] != null)
        {
            StopCoroutine(activeCoroutines[index]);
            Debug.Log("asddffsdf 按下了扳机键！！！ CCC");
        }

        activeCoroutines[index] = StartCoroutine(FlashRoutine(img, index));
    }

    private IEnumerator FlashRoutine(Image img, int index)
    {
        float duration = 1.0f;
        float endTime = Time.time + duration;
        float interval = 0.1f;
        bool isGreen = true;

        while (Time.time < endTime)
        {
            img.color = isGreen ? Color.green : Color.white;
            yield return new WaitForSeconds(interval);
            isGreen = !isGreen;
        }

        img.color = Color.white;
        activeCoroutines[index] = null;
    }
}
