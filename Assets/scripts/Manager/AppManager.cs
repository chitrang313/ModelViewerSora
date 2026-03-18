using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class AppManager:MonoBehaviour {
    public static AppManager instance;
    [SerializeField] private Button processDataButton;
    public UnityEvent onDataProcessedEvent;
    private void Awake() {
        Singletone();
        processDataButton.onClick.RemoveAllListeners();
        processDataButton.onClick.AddListener(OnClickProcessData);
    }
    private void Singletone() {
        if (instance == null) {
            instance = this;
        } else {
            Destroy(gameObject);
        }
    }
    private void OnClickProcessData() {
        onDataProcessedEvent?.Invoke();
    }
}//AppManager class end.
