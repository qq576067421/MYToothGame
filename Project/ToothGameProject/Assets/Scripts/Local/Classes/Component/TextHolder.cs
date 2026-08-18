using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Events;

namespace LCL
{
    public class TextHolder : MonoBehaviour
    {
        public TextAsset Asset;
        public Text Text;

        private void Awake()
        {
            if(Text != null)
            {
                Text.text = Asset.text;
            }
        }
    }
}