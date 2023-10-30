using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ScoreManager : NetworkBehaviour
{
    private NetworkVariable<int> score = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> timer = new NetworkVariable<int>(30, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    TextMeshProUGUI scoreText;
    TextMeshProUGUI timerText;

    float timeLeft = 0;

    // Start is called before the first frame update
    void Start()
    {
        scoreText = GameObject.FindWithTag("ScoreText").GetComponent<TextMeshProUGUI>();
        timerText = GameObject.FindWithTag("TimerText").GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        if (NetworkManager.Singleton.IsServer && timer.Value > 0)
        {
            timeLeft += Time.deltaTime;
            if (timeLeft >= 1)
            {
                timeLeft = 0;
                timer.Value -= 1;
            }
        }
        scoreText.text = score.Value.ToString();
        timerText.text = timer.Value.ToString();
    }

    //Upon collision with another GameObject, this GameObject will reverse direction
    private void OnTriggerEnter(Collider other)
    {
        if (NetworkManager.Singleton.IsServer && timer.Value > 0)
        {
            score.Value += 100;
        }
    }

}
