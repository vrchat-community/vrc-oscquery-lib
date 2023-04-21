using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneChanger : MonoBehaviour
{
    public Button ButtonChatboxSender;
    public Button ButtonChatboxReceiver;
    public Button ButtonReceiver;
    public Button ButtonTrackers;

    private void Start()
    {
        ButtonChatboxSender.onClick.AddListener(()=>SceneManager.LoadScene(1));
        ButtonChatboxReceiver.onClick.AddListener(()=>SceneManager.LoadScene(2));
        ButtonReceiver.onClick.AddListener(()=>SceneManager.LoadScene(3));
        ButtonTrackers.onClick.AddListener(()=>SceneManager.LoadScene(4));
    }

    private void OnDestroy()
    {
        ButtonChatboxSender.onClick.RemoveAllListeners();
        ButtonChatboxReceiver.onClick.RemoveAllListeners();
        ButtonReceiver.onClick.RemoveAllListeners();
        ButtonTrackers.onClick.RemoveAllListeners();
    }
}
