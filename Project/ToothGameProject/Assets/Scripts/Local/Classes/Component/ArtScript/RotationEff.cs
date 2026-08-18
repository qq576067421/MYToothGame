using UnityEngine;
using System.Collections;

public class RotationEff : MonoBehaviour 
{
	public float xSpeed;
	public float ySpeed;
	public float zSpeed;

	void Start ()
	{

	}
	void Update ()
	{
		transform.Rotate (xSpeed * Time.deltaTime, ySpeed * Time.deltaTime, zSpeed * Time.deltaTime);
	}
}
