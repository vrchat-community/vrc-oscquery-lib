using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneChanger : MonoBehaviour
{
    public Button ButtonChatbox;
    public Button ButtonReceiver;
    public Button ButtonTrackers;

    private void Start()
    {
        ButtonChatbox.onClick.AddListener(()=>SceneManager.LoadScene(1));
        ButtonReceiver.onClick.AddListener(()=>SceneManager.LoadScene(2));
        ButtonTrackers.onClick.AddListener(()=>SceneManager.LoadScene(3));
    }

    private void OnDestroy()
    {
        ButtonChatbox.onClick.RemoveAllListeners();
        ButtonReceiver.onClick.RemoveAllListeners();
        ButtonTrackers.onClick.RemoveAllListeners();
    }
}
