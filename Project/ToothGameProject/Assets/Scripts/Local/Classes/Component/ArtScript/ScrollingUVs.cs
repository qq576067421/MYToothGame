using UnityEngine;
using System.Collections;

public class ScrollingUVs : MonoBehaviour
{
    public int materialIndex = 0;
    public Vector2 uvAnimationRate = new Vector2( 1.0f, 0.0f );
    public string textureName = "_MainTex";

    Vector2 uvOffset = Vector2.zero;
    private Renderer m_Renderer = null;
    private Material m_Material;
    private void Start()
    {
        m_Renderer = GetComponent<Renderer>();
        m_Material = m_Renderer.materials[materialIndex];
    }
    void LateUpdate()
    {
        uvOffset += ( uvAnimationRate * Time.deltaTime );
        if(m_Renderer != null && m_Renderer.enabled)
        {
            m_Material.SetTextureOffset( textureName, uvOffset );
        }
    }
}