using TMPro;
using UnityEngine;

public class MainMenuManager : MonoBehaviour
{

    [SerializeField] TMP_InputField ip;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void SetIsSpectator()
    {
        PlayerPrefs.SetInt("IsSpectator", 1);
        UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    public void SetIsPlayer()
    {
        PlayerPrefs.SetInt("IsSpectator", 0);
        PlayerPrefs.SetString("IpAdress", ip.text);

        UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}
