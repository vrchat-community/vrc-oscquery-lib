using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneChanger : MonoBehaviour
{
    public Button ButtonChatbox;
    public Button ButtonReceiver;

    private void Start()
    {
        ButtonChatbox.onClick.AddListener(()=>SceneManager.LoadScene(1));
        ButtonReceiver.onClick.AddListener(()=>SceneManager.LoadScene(2));
    }

    private void OnDestroy()
    {
        ButtonChatbox.onClick.RemoveAllListeners();
        ButtonReceiver.onClick.RemoveAllListeners();
    }
}
