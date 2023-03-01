using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    public Turn turn = Turn.Patient;

    public GameObject cardObj;
    public CardSlot slot;
    public Transform enemySlot;

    public GameStat stat;

    public Text stageLabel;
    public Text turnLabel;
    public Text partyLabel;

    public Text healthStat;
    public Text emotionStat;
    public Text economyStat;
    public RectTransform cancerProgress;

    public Text teamStatLabel;

    public bool inProgress = false;
    public GameObject skipBtn;

    public List<CardMeta> FulltimePatientCards = new List<CardMeta>();
    public List<CardMeta> PostTreatmentPatientCards = new List<CardMeta>();

    public List<CardMeta> PostTreatmentCancerCards = new List<CardMeta>();

    public List<CardMeta> TreatmentCancerCards = new List<CardMeta>();
    public List<CardMeta> TreatmentPatientCards = new List<CardMeta>();
    
    public List<CardMeta> DiaCancerCards = new List<CardMeta>();
    public List<CardMeta> DiaPatientCards = new List<CardMeta>();

    public TreatmentStage stage;

    private SignalR signalR;

    public Text roomsStat;
    public InputField roomInput;
    public GameObject joinRoomPage;
    public bool isConnected = false;

    public InputField playerNameInput;

    public GameObject changeStatePanel;
    public Dropdown stateSelector;

    public TurnPayload turnPayload;

    public GameObject cancerProgressPanel;

    private string roomID;

    public GameObject loadingPanel;

    private TeamStatus teamStat;

    public void RefreshStat(GameStat gstat)
    {
        stat = gstat;
        
        healthStat.text = stat.health.ToString();
        emotionStat.text = stat.emotion.ToString();
        economyStat.text = stat.economy.ToString();
        cancerProgress.anchorMax = new Vector2(stat.cancer/5.0f, 1);
    }

    public List<CardMeta> cardFull;

    private void Start()
    {
        loadingPanel.SetActive(true);
        cardFull = FulltimePatientCards;
        cardFull.AddRange(PostTreatmentCancerCards);
        cardFull.AddRange(PostTreatmentPatientCards);
        cardFull.AddRange(TreatmentCancerCards);
        cardFull.AddRange(TreatmentPatientCards);
        cardFull.AddRange(DiaPatientCards);
        cardFull.AddRange(DiaCancerCards);

        for (int i = 0; i < cardFull.Count; i++)
        {
            cardFull[i].index = i;
        }
        
        ConfigureSignalR();
        turnLabel.text = "";
    }
    
    //private string signalRHubURL = "http://localhost:5000/MainHub";
    private string signalRHubURL;

    public string connectionID;

    private void Join(string roomID)
    {
        signalR.Invoke("Join", JsonConvert.SerializeObject(new JoinRequestPayload() { connectionID = connectionID, roomID = roomID}));
        Debug.Log("Joining");
    }

    public bool debugWeb = false;

    public void ConfigureSignalR()
    {
        if (Application.isEditor && !debugWeb)
        {
            signalRHubURL = "http://localhost:5544/MainHub";
        }
        else
        {
            signalRHubURL = "https://cancer.scie.dev/MainHub";
        }
        signalR = new SignalR();
        signalR.Init(signalRHubURL);

        signalR.ConnectionStarted += (object sender, ConnectionEventArgs e) =>
        {
            connectionID = e.ConnectionId;
            Debug.Log($"Connected: {e.ConnectionId}");
            isConnected = true;
            loadingPanel.SetActive(false);
            //Join();
        };

        signalR.ConnectionClosed += (object sender, ConnectionEventArgs e) =>
        {
            Debug.Log($"Disconnected: {e.ConnectionId}");
            isConnected = false;
        };
        
        signalR.On("Side", (string payload) =>
        {
            int side = int.Parse(payload);
            turn = (Turn) side;
            if (turn == Turn.Patient)
            {
                partyLabel.text = "Patient Party";
                cancerProgressPanel.SetActive(false);
            }
            else
            {
                partyLabel.text = "Cancer Party";
                cancerProgressPanel.SetActive(true);
            }
        });
        
        signalR.On("Rooms", (string payload) =>
        {
            var rooms = JsonConvert.DeserializeObject<List<string>>(payload);
            roomsStat.text = "Rooms:\n" + string.Join('\n', rooms);
        });
        
        signalR.On("Stage", (string payload) =>
        {
            int val = int.Parse(payload);
            stage = (TreatmentStage) val;
            switch (stage)
            {
                case TreatmentStage.Dia:
                    stageLabel.text = "Diagnosis";
                    break;
                case TreatmentStage.Treatment:
                    stageLabel.text = "Treatment";
                    break;
                case TreatmentStage.PostTreatment:
                    stageLabel.text = "Post-treatment";
                    break;
            }
        });
        
        signalR.On("Turn", (string payload) =>
        {
            turnPayload = JsonConvert.DeserializeObject<TurnPayload>(payload);
            if (turnPayload.connectionID == connectionID)
            {
                StartCoroutine(PlayerTurn());
            }
            else
            {
                InProgress();
                UpdateTurnLabel();
            }
        });
        
        signalR.On("Stat", (string payload) =>
        {
            Debug.Log(payload);
            var gstat = JsonConvert.DeserializeObject<GameStat>(payload);
            RefreshStat(gstat);
        });
        
        signalR.On("Team", (string payload) =>
        {
            teamStat = JsonConvert.DeserializeObject<TeamStatus>(payload);
            teamStatLabel.text = $"Room: {roomID}\nPatient Party:\n{string.Join('\n', teamStat.patientPlayers)}\n\nCancer Party:\n{string.Join('\n', teamStat.cancerPlayers)}";
            UpdateTurnLabel();
        });
        
        signalR.On("Card", (string payload) =>
        {
            var index = int.Parse(payload);
            StartCoroutine(EnemyTurn(index));
        });

        signalR.Connect();
    }

    public void UpdateTurnLabel()
    {
        if (turnPayload.side == 0)
        {
            turnLabel.text = $"{teamStat.patientPlayers[turnPayload.counter]}'sTurn";
        }
        else
        {
            turnLabel.text = $"{teamStat.cancerPlayers[turnPayload.counter]}'s Turn";
        }
    }

    public void InProgress()
    {
        skipBtn.SetActive(false);
        inProgress = true;
    }

    public void NotInProgress()
    {
        skipBtn.SetActive(true);
        inProgress = false;
    }

    public Card newCard(int index = -1)
    {
        CardMeta cardMeta;
        if (index == -1)
        {
            var metaList = new List<CardMeta>();

            if (turn == Turn.Patient)
            {
                metaList.AddRange(FulltimePatientCards);
                if (stage == TreatmentStage.PostTreatment)
                    metaList.AddRange(PostTreatmentPatientCards);
                else if (stage == TreatmentStage.Treatment)
                    metaList.AddRange(TreatmentPatientCards);
                else
                    metaList.AddRange(DiaPatientCards);
            }
            else
            {
                if (stage == TreatmentStage.PostTreatment)
                    metaList.AddRange(PostTreatmentCancerCards);
                else if (stage == TreatmentStage.Treatment)
                    metaList.AddRange(TreatmentCancerCards);
                else
                    metaList.AddRange(DiaCancerCards);
            }

            cardMeta = metaList[Random.Range(0, metaList.Count)];
        }
        else
        {
            cardMeta = cardFull[index];
        }

        var c = Instantiate(cardObj, Vector3.zero, Quaternion.identity);
        c.GetComponent<SpriteRenderer>().sprite = cardMeta.sprite;
        var card = c.GetComponent<Card>();
        card.meta = cardMeta;
        return card;
    }

    public IEnumerator PlayerTurn()
    {
        turnLabel.text = "Your Turn";
        yield return new WaitForSeconds(1);
        var c = newCard();
        c.transform.position = enemySlot.position;
        slot.AddCard(c);
        yield return new WaitForSeconds(1);
        NotInProgress();
    }

    public void SkipTurn()
    {
        if(inProgress)
        {
            return;
        }
        InProgress();
        signalR.Invoke("Skip", connectionID);
        //StartCoroutine(EnemyTurn());
    }

    public void UseCard(Card card)
    {
        if (inProgress)
        {
            return;
        }
        InProgress();
        Debug.Log(JsonConvert.SerializeObject(card.meta));
        signalR.Invoke("Card", JsonConvert.SerializeObject(card.meta));
        //StartCoroutine(UseCardCor(card));
    }

    public IEnumerator EnemyTurn(int index)
    {
        var c = newCard(index);
        c.transform.position = enemySlot.position;
        c.slotPos = Vector3.zero;
        yield return new WaitForSeconds(2);
        //ProcessCard(c);
        c.state = CardState.Decay;
        //StartCoroutine(PlayerTurn());
    }

    private void OnApplicationQuit()
    {
        signalR.Stop();
    }

    public void JoinRoom()
    {
        if (isConnected && roomInput.text.Length > 0)
        {
            roomID = roomInput.text;
            Join(roomInput.text);
            joinRoomPage.SetActive(false);
        }
    }

    public void ChangePlayerName()
    {
        signalR.Invoke("SetName", playerNameInput.text);
    }

    public void OpenChangeState()
    {
        changeStatePanel.SetActive(true);
        stateSelector.options = new List<Dropdown.OptionData>()
        {
            new Dropdown.OptionData("Diagnosis"),
            new Dropdown.OptionData("Treatment"),
            new Dropdown.OptionData("Post-treatment") 
        };
        stateSelector.value = (int) turn;
    }

    public void ConfirmChangeState()
    {
        changeStatePanel.SetActive(false);
        signalR.Invoke("SetState", stateSelector.value.ToString());
    }
}