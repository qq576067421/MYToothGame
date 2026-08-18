using UnityEngine;

namespace GameDll
{
    [ExecuteInEditMode]
    public class HeroAttributePanel : MonoBehaviour
    {
        [Header("Hero Reference")]
        [SerializeField] public PlayerHero m_TargetHero;

        public PlayerHero GetTargetHero() { return m_TargetHero; }

        public void Init(PlayerHero hero)
        {
            m_TargetHero = hero;
        }
    }
}
