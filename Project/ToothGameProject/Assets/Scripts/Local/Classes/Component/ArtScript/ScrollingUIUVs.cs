using UnityEngine;
using System.Collections;

public class ScrollingUIUVs : MonoBehaviour
{
    public Vector2 uvAnimationRate = new Vector2( 1.0f, 0.0f );
    public UnityEngine.UI.RawImage m_RawImage;
         

    Vector2 uvOffset = Vector2.zero;
    private void Start()
    {

    }
    void LateUpdate()
    {
        uvOffset += ( uvAnimationRate * Time.deltaTime );

        if(m_RawImage != null)
        {
            var rect = m_RawImage.uvRect;
            rect.x = uvOffset.x;
            rect.y = uvOffset.y;

            if(Mathf.Abs(rect.x) >= 1000)
            {
                rect.x = 0;
            }
            if (Mathf.Abs(rect.y) >= 1000)
            {
                rect.y = 0;
            }

            m_RawImage.uvRect = rect; 
        }
    }
}