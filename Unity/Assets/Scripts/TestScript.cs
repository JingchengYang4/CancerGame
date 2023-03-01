using UnityEngine;
using UnityEngine.UI;
using System;

public class TestScript : MonoBehaviour
{
    public string signalRHubURL = "http://localhost:5000/MainHub";

    public string statusText = "Awaiting Connection...";
    public string connectedText = "Connection Started";
    public string disconnectedText = "Connection Disconnected";

    public InputField input;

    private Text uiText;

    SignalR signalR;

    void Start()
    {
        uiText = GameObject.Find("Text").GetComponent<Text>();
        DisplayMessage(statusText);

        signalR = new SignalR();
        signalR.Init(signalRHubURL);

        signalR.On("Message", (string payload) =>
        {
            var json = JsonUtility.FromJson<JsonPayload>(payload);
            DisplayMessage(json.message);
        });

        signalR.ConnectionStarted += (object sender, ConnectionEventArgs e) =>
        {
            Debug.Log($"Connected: {e.ConnectionId}");
            DisplayMessage(connectedText);
        };

        signalR.ConnectionClosed += (object sender, ConnectionEventArgs e) =>
        {
            Debug.Log($"Disconnected: {e.ConnectionId}");
            DisplayMessage(disconnectedText);
        };

        signalR.Connect();
    }

    void DisplayMessage(string message)
    {
        uiText.text += $"\n{message}";
        Debug.Log($"MESSAGE: {message}");
    }

    public void SendMessage()
    {
        signalR.Invoke("Message", JsonUtility.ToJson(new JsonPayload() { message = input.text }));
    }

    [Serializable]
    public class JsonPayload
    {
        public string message;
    }
}
