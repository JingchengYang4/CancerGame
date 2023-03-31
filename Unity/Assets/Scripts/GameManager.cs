using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public Transform enemyStack;

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
    public bool hasExamination = false;

    private string roomID;

    public GameObject loadingPanel;

    private TeamStatus teamStat;

    public GameObject winPanel;
    public Text winLabel;

    public GameObject endPanel;
    public Text endScoreLabel;

    public Card enemyCardUsed;

    public RectTransform tooltip;
    public int tooltipCounter = 0;

    public bool isViewingCard;
    public GameObject viewCardPanel;
    public Image cardImage;

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

    public bool cnVersion = false;

    public void ConfigureSignalR()
    {
        if (Application.isEditor && !debugWeb)
        {
            signalRHubURL = "http://localhost:5544/MainHub";
        }
        else if (cnVersion)
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
            joinRoomPage.SetActive(true);
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
                partyLabel.text = "患者阵营";
                cancerProgressPanel.SetActive(false || hasExamination);
            }
            else
            {
                partyLabel.text = "癌症阵营";
                cancerProgressPanel.SetActive(true);
            }
        });
        
        signalR.On("Rooms", (string payload) =>
        {
            var rooms = JsonConvert.DeserializeObject<List<string>>(payload);
            roomsStat.text = translateDefault("房间:\n" + string.Join('\n', rooms));
        });
        
        signalR.On("Stage", (string payload) =>
        {
            int val = int.Parse(payload);
            stage = (TreatmentStage) val;
            switch (stage)
            {
                case TreatmentStage.Dia:
                    stageLabel.text = "诊断阶段";
                    break;
                case TreatmentStage.Treatment:
                    stageLabel.text = "治疗阶段";
                    break;
                case TreatmentStage.PostTreatment:
                    stageLabel.text = "后续治疗阶段";
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
            teamStatLabel.text = $"房间: {roomID}\n患者阵营:\n{string.Join('\n', teamStat.patientPlayers)}\n\n癌症阵营:\n{string.Join('\n', teamStat.cancerPlayers)}";
            teamStatLabel.text = translateDefault(teamStatLabel.text);
            UpdateTurnLabel();
        });
        
        signalR.On("Card", (string payload) =>
        {
            var index = int.Parse(payload);
            StartCoroutine(EnemyTurn(index));
        });
        
        signalR.On("Win", (string payload) =>
        {
            winLabel.text = payload + "获胜";
            winPanel.SetActive(true);
        });
        
        signalR.On("Reset", (string payload) =>
        {
            winPanel.SetActive(false);
            Reset();
        });
        
        signalR.On("End", (string payload) =>
        {
            endPanel.SetActive(true);
            float score = 0;
            if (turn == Turn.Cancer)
            {
                score = (10 - stat.health) + stat.cancer + (10 - stat.emotion);
            }
            else
            {
                score = stat.health - stat.cancer + stat.emotion;
            }
            endScoreLabel.text = $"总分:\n{score}";
        });
        
        signalR.On("Close", (string payload) =>
        {
            endPanel.SetActive(false);
            joinRoomPage.SetActive(true);
            Reset();
        });

        signalR.Connect();
    }

    private void LateUpdate()
    {
        if (tooltipCounter <= 0)
        {
            tooltip.gameObject.SetActive(false);
        }
        else
        {
            tooltip.gameObject.SetActive(true);
        }
        
        var mosPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mosPos.z = -5;
        tooltip.localPosition = mosPos;
        tooltipCounter = 0;
    }

    public void Reset()
    {
        hasExamination = false;
        slot.Clear();
        viewCardPanel.SetActive(false);
    }

    public void UpdateTurnLabel()
    {
        if (turnPayload.side == 0)
        {
            turnLabel.text = $"轮到{translateDefault(teamStat.patientPlayers[turnPayload.counter])}";
        }
        else
        {
            turnLabel.text = $"轮到{translateDefault(teamStat.cancerPlayers[turnPayload.counter])}";
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
        turnLabel.text = "轮到你了";
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

        var tags = card.meta.tags.Split('#');
        foreach (var tag in tags)
        {
            switch (tag)
            {
                case "SHOW_CANCER":
                    cancerProgressPanel.SetActive(true);
                    hasExamination = true;
                    break;
            }
        }
        
        signalR.Invoke("Card", JsonConvert.SerializeObject(card.meta));
        //StartCoroutine(UseCardCor(card));
    }

    public IEnumerator EnemyTurn(int index)
    {
        viewCardPanel.SetActive(false);
        if(enemyCardUsed is not null)
        {
            enemyCardUsed.transform.position -= Vector3.forward;
        }
        var c = newCard(index);
        c.transform.position = enemySlot.position;
        c.slotPos = Vector3.zero;
        yield return new WaitForSeconds(1.5f);
        c.slotPos = enemyStack.position;
        //ProcessCard(c);
        if(enemyCardUsed is not null)
        {
            enemyCardUsed.state = CardState.Decay;
        }

        enemyCardUsed = c;
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
            new Dropdown.OptionData(localization["Diagnosis"]),
            new Dropdown.OptionData(localization["Treatment"]),
            new Dropdown.OptionData(localization["Post-treatment"]) 
        };
        stateSelector.value = (int) turn;
    }

    private Dictionary<string, string> localization = new Dictionary<string, string>()
    {
        {"Diagnosis", "诊断"},
        {"Treatment", "治疗"},
        {"Post-treatment", "后续治疗"},
    };

    public void ConfirmChangeState()
    {
        changeStatePanel.SetActive(false);
        signalR.Invoke("SetState",  stateSelector.value.ToString());
    }

    public void End()
    {
        signalR.Invoke("EndGame", roomID);
    }

    public void ViewCard(Sprite sprite)
    {
        cardImage.sprite = sprite;
        viewCardPanel.SetActive(true);
        isViewingCard = true;
    }

    public void CloseViewCard()
    {
        isViewingCard = false;
        viewCardPanel.SetActive(false);
    }

    public string translateDefault(string text)
    {
        return text.Replace("Cancer", "癌症阵营").Replace("Patient", "患者阵营").Replace("Players", "玩家").Replace("Player", "玩家");
    }
}
