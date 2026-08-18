using UnityEngine;
using UnityEngine.SceneManagement;

public class MyCustomData : MonoBehaviour
{
    public string levelData;
    public string versionData;

    public void LoadLevel ()
    {
        //        SceneManager.LoadScene(textData);
        Debug.LogFormat("LoadingScene: {0}, Version: {1}", levelData, versionData);
    }
}