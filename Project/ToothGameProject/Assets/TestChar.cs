using GameDll;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityUI;

public class TestChar : MonoBehaviour
{

    [SerializeField] LUIText curText;

    private void OnEnable()
    {
        AndroidParseDataDemo.Instance.onFollowPlayerRotation += (float[] rot) => {
            for (int i = 0; i < rot.Length; i++)
                GetComponent<LUIText>().text = $"座位{i} 旋转 {rot[i]}";
            //transform.rotation = Quaternion.Euler(0, rot[i] * 90f, 0);
        };
        AndroidParseDataDemo.Instance.onPlayerNormalAttack += (seat, combo) => {
            curText.text = $"座位{seat} 普通出拳 {combo} 连击!";
            if (combo >= 2) Debug.Log($"座位{seat} 交替出拳 {combo} 连击!");
        };
        AndroidParseDataDemo.Instance.onPlayerSkillAttack += (seat) => {
            //CBattleLogic.GetInstance()?.TryPlayerSkillBySeat(seat, faceForward, Vector3.zero);
            curText.text = $"座位{seat} 技能抛物!";
        };
    }

    // Start is called before the first frame update
    void Start()
    {
        // 主动查询某座位完整状态
        var s = AndroidParseDataDemo.Instance.GetPlayerState(2);
        if (!s.isValid) return;
        if ((s.poseType & 3) == 3) Debug.Log("座位2 双手举起");
        if (s.leftHandType == 0) Debug.Log("座位2 左手握拳");
        float yaw = s.rotationOffset * 90f;  // -90°~90°，0=正对相机
    }

    // Update is called once per frame
    void Update()
    {
        
    }

}
