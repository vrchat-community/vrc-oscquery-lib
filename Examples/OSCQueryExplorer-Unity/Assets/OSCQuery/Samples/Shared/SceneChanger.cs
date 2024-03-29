﻿using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneChanger : MonoBehaviour
{
    public Button ButtonChatboxSender;
    public Button ButtonChatboxReceiver;
    public Button ButtonReceiver;
    public Button ButtonTrackerSender;
    public Button ButtonTrackerReceiver;
    public Button ButtonAdvertiseAndFind;
    public Button ButtonHeadAndWristReceiver;

    private void Start()
    {
        ButtonChatboxSender.onClick.AddListener(()=>SceneManager.LoadScene(1));
        ButtonChatboxReceiver.onClick.AddListener(()=>SceneManager.LoadScene(2));
        ButtonTrackerSender.onClick.AddListener(()=>SceneManager.LoadScene(3));
        ButtonTrackerReceiver.onClick.AddListener(()=>SceneManager.LoadScene(4));
        ButtonReceiver.onClick.AddListener(()=>SceneManager.LoadScene(5));
        ButtonAdvertiseAndFind.onClick.AddListener(()=>SceneManager.LoadScene(6));
        ButtonHeadAndWristReceiver.onClick.AddListener(() => SceneManager.LoadScene(7));
    }

    private void OnDestroy()
    {
        ButtonChatboxSender.onClick.RemoveAllListeners();
        ButtonChatboxReceiver.onClick.RemoveAllListeners();
        ButtonTrackerSender.onClick.RemoveAllListeners();
        ButtonTrackerReceiver.onClick.RemoveAllListeners();
        ButtonReceiver.onClick.RemoveAllListeners();
        ButtonAdvertiseAndFind.onClick.RemoveAllListeners();
        ButtonHeadAndWristReceiver.onClick.RemoveAllListeners();
    }
}
