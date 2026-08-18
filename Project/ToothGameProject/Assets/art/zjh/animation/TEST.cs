using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TEST : MonoBehaviour
{
    // Start is called before the first frame update
    public Animator _camera;
    public Animator _enviroment;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            _camera.SetBool("IsRotation", true);
            _enviroment.SetBool("IsRotation", true);
        }else if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            _camera.SetBool("IsRotation", false);
            _enviroment.SetBool("IsRotation", false);
        }
    }
}
