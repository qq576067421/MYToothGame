using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


	public class SlideShowScrollViewPro_PointerHelper : MonoBehaviour, IPointerClickHandler {

		public MyCustomElementUI songData;

		#region IPointerClickHandler implementation

		public void OnPointerClick (PointerEventData pointerData) {
			if (pointerData.button == PointerEventData.InputButton.Right) {
				songData.SelectThisImageOnly_Click ();
//				FindObjectOfType<SongSelection_SongOptions> ().Open ();
			}

			#endregion

		}
	}

