using UnityEngine;
using UnityEngine.UI;
using System.Collections;

using SimpleJSON;
using UnityEngine.Networking;

public class Game_TrueOrFalse : TrueGameAncestor {

    [Tooltip("A jó válasznál megjelenő kép")]
    public Sprite goodAnswer;
    [Tooltip("A rossz válasznál megjelenő kép")]
    public Sprite wrongAnswer;
    [Tooltip("Az igaz és a hamis gombok villogásának sebessége")]
    public float flashingSpeed;

    public Sprite trueButtonDark;
    public Sprite trueButtonLight;
    public Sprite falseButtonDark;
    public Sprite falseButtonLight;

    GameObject questionMove;        // A gameObject, aminek segítségével fog előugrani a kérdés
    Text questionUIText;            // A kérdést tartalmazó UIText
    TEXDraw questionUITEXDraw;      // A kérdést tartalmazó UITEXDraw

    Text trueText;
    Text trueTextPressed;
    Text falseText;
    Text falseTextPressed;

    SpriteRenderer feedBack;        // A válasz helyességét mutató objektum (pipa vagy X)

    SpriteRenderer trueButton;
    SpriteRenderer falseButton;

    SpriteRenderer trueButtonLightSpriteRenderer;
    SpriteRenderer falseButtonLightSpriteRenderer;

    GameObject trueButtonPressed;
    GameObject falseButtonPressed;

    TaskTrueOrFalseData taskData;   // Az igaz hamis játék feladata

    /// <summary>
    /// Mit választott a felhasználó : null - még nem választott, true = az igazat választotta, false = a hamisat választotta
    /// </summary>
    bool? selectTrue;
    /// <summary>
    /// villogásnál ha ez true, akkor az igaz gomb világit, egyébként a hamis gomb
    /// </summary>
    bool trueLight;
    /// <summary>
    /// Mennyi idő maradt még a következő gomb villágítás cseréig
    /// </summary>
    float changeLightRemainTime;

    /*
    string[] questions = new string[] {
        "Ha a felhőben jégkristályok vannak," + System.Environment.NewLine + "és azok lehullanak, villámlás lesz.",
        "A légnemű anyagokra jellemző, hogy mindig " + System.Environment.NewLine + "kitöltik a rendelkezésükre álló teret."
    };
    */

    // Use this for initialization
    override public void Awake () {
        base.Awake(); // Meghívjuk az ős osztály Awake metódusát

        questionMove = Common.SearchGameObject(gameObject, "questionMove");
        questionUIText = Common.SearchGameObject(gameObject, "questionUIText").GetComponent<Text>();
        questionUITEXDraw = Common.SearchGameObject(gameObject, "questionUITEXDraw").GetComponent<TEXDraw>();

        trueText = Common.SearchGameObject(gameObject, "trueText").GetComponent<Text>();
        trueTextPressed = Common.SearchGameObject(gameObject, "trueTextPressed").GetComponent<Text>();
        falseText = Common.SearchGameObject(gameObject, "falseText").GetComponent<Text>();
        falseTextPressed = Common.SearchGameObject(gameObject, "falseTextPressed").GetComponent<Text>();

        feedBack = Common.SearchGameObject(gameObject, "feedBack").GetComponent<SpriteRenderer>();

        trueButton = Common.SearchGameObject(gameObject, "trueButton").GetComponent<SpriteRenderer>();
        trueButtonLightSpriteRenderer = Common.SearchGameObject(gameObject, "trueLight").GetComponent<SpriteRenderer>();
        trueButtonPressed = Common.SearchGameObject(gameObject, "trueButtonPressed");

        falseButton = Common.SearchGameObject(gameObject, "falseButton").GetComponent<SpriteRenderer>();
        falseButtonLightSpriteRenderer = Common.SearchGameObject(gameObject, "falseLight").GetComponent<SpriteRenderer>();
        falseButtonPressed = Common.SearchGameObject(gameObject, "falseButtonPressed");

        // Gombok szkriptjének beállítása, kivéve a menu gombjait, mert azokat a menu szkript állítja magára
        foreach (Button button in GetComponentsInChildren<Button>())
            button.buttonClick = ButtonClick;
    }

    /// <summary>
    /// Felkészülünk a feladat megmutatására.
    /// </summary>
    /// <returns></returns>
    override public IEnumerator PrepareTask()
    {
        exitButton.SetActive(false); // Common.configurationController.deviceIsServer);

        clock.timeInterval = taskData.time;
        clock.Reset(0);

        // Az Igaz / Hamis szöveget kicseréljük a nyelvnek megfelelően
        trueText.text = Common.languageController.Translate("True");
        trueTextPressed.text = Common.languageController.Translate("True");
        falseText.text = Common.languageController.Translate("False");
        falseTextPressed.text = Common.languageController.Translate("False");

        // Megjelenítjük a kérdés szövegét
        questionUIText.text = taskData.question;
        questionUITEXDraw.text = taskData.question;
        questionUIText.enabled = Common.configurationController.textComponentType == ConfigurationController.TextComponentType.Text;
        questionUITEXDraw.enabled = Common.configurationController.textComponentType == ConfigurationController.TextComponentType.TEXDraw;

        // Eltüntetjük a válasz visszajelzésére szolgáló képet (zöld pipa vagy piros X)
        feedBack.transform.localScale = Vector3.zero;

        yield return null;
    }

    override public IEnumerator InitCoroutine()
    {
        status = Status.Init;

        // Lekérdezzük a feladatot
        taskData = (TaskTrueOrFalseData)Common.taskController.task;

        questionMove.transform.localScale = Vector3.one * 0.0001f; //  new Vector3(0.0001f, 0.0001f, 0.0001f);
        //questionMove.transform.localScale = Vector3.zero;

        // Felkészülünk a feladat megjelenítésére
        yield return StartCoroutine(PrepareTask());

        selectTrue = null;
    }

    // Amikor az új képernyő megjelenítése befejeződik, akkor ezt meghívja a ScreenController
    override public IEnumerator ScreenShowFinishCoroutine()
    {
        // Megjelenítjük a kérdés szövegét
        //questionUIText.text = taskData.question;

        //questionMove.transform.localScale = new Vector3(0.0001f, 0.0001f, 0.0001f);
        iTween.ScaleTo(questionMove, iTween.Hash("islocal", true, "scale", Vector3.one, "time", taskData.animSpeed1, "easeType", iTween.EaseType.easeOutElastic));
        Common.audioController.SFXPlay("boing");

        yield return new WaitForSeconds(taskData.animSpeed1);

        status = Status.Play;
        Common.HHHnetwork.messageProcessingEnabled = true;

        Common.audioController.SetBackgroundMusic(1, 0.05f, 4); // Elindítjuk a háttérzenét
    }

    // A kérdéshez tartozó játék elemeket kell eltávolítani, ha meghívják ezt a metódust. A TaskController fogja új feladatnál ha az új feladatot ugyan annak a képernyőnek kell megjelenítenie
    // Meglehet adni neki egy callBack függvényt, amit akkor hív meg ha végzet a játék elemek elrejtésével, mivel ez sokáig is eltarthat és addig nem kéne tovább menni az új feladatra.
    override public IEnumerator HideGameElement()
    {
        // Eltüntetjük a régi kérdést és az értékelést
        iTween.ScaleTo(questionMove, iTween.Hash("islocal", true, "scale", Vector3.zero, "time", taskData.animSpeed1, "easeType", iTween.EaseType.linear));
        iTween.ScaleTo(feedBack.gameObject, iTween.Hash("islocal", true, "scale", Vector3.zero, "time", taskData.animSpeed1, "easeType", iTween.EaseType.linear));

        clock.Reset(1);

        yield return new WaitForSeconds(taskData.animSpeed1);
    }

    /// <summary>
    /// A tanári tablet óraterv előnézeti képernyője hívja meg ha meg kell mutatni a játék előnézetét.
    /// A task paraméter tartalmazza a játék képernyőjének adatait.
    /// </summary>
    /// <param name="task">A megjelenítendő képernyő adata</param>
    override public IEnumerator Preview(TaskAncestor task)
    {
        taskData = (TaskTrueOrFalseData)task;
        yield return StartCoroutine(PrepareTask());
    }

    IEnumerator EvaluateCoroutine (JSONNode jsonData) 
    {
        if (jsonData[C.JSONKeys.evaluateAnswer].Value == C.JSONValues.evaluateIsIgnore)
            yield break;

        status = Status.Result;
        selectTrue = jsonData[C.JSONKeys.selectedAnswer].AsBool;

        if (Common.configurationController.answerFeedback != ConfigurationController.AnswerFeedback.Immediately)
        {
            yield break;
        }

        switch (jsonData[C.JSONKeys.evaluateAnswer].Value)
        {
            case C.JSONValues.evaluateIsTrue:
                // Ha jót válaszoltak
                feedBack.sprite = goodAnswer;
                Common.audioController.SFXPlay("positive");
                break;

            case C.JSONValues.evaluateIsFalse:
                // Ha rosszat válaszoltak
                feedBack.sprite = wrongAnswer;
                Common.audioController.SFXPlay("negative");
                break;
        }

        iTween.ScaleTo(feedBack.gameObject, iTween.Hash("islocal", true, "scale", Vector3.one, "time", 1, "easeType", iTween.EaseType.easeOutElastic));

        yield return new  WaitForSeconds(1.2f);
    }

    // Update is called once per frame
    new void Update()
    {
        base.Update();

        // Gombok kezelés
        changeLightRemainTime -= Time.deltaTime;

        if (changeLightRemainTime <= 0)
        {
            trueLight = !trueLight;
            changeLightRemainTime = flashingSpeed;
        }

        if (selectTrue == null) // Ha még nem választottak villogás animálása
        {
            trueButton.sprite = (trueLight) ? trueButtonLight : trueButtonDark;
            trueButtonLightSpriteRenderer.enabled = trueLight;

            falseButton.sprite = (trueLight) ? falseButtonDark : falseButtonLight;
            falseButtonLightSpriteRenderer.enabled = !trueLight;

            trueButton.gameObject.SetActive(true);
            falseButton.gameObject.SetActive(true);

            trueButtonPressed.SetActive(false);
            falseButtonPressed.SetActive(false);
        }
        else { // Ha már válaszottak
            trueButton.sprite = trueButtonDark;
            trueButtonLightSpriteRenderer.enabled = false;

            falseButton.sprite = falseButtonDark;
            falseButtonLightSpriteRenderer.enabled = false;

            trueButtonPressed.SetActive(selectTrue.Value);
            trueButton.gameObject.SetActive(!selectTrue.Value);

            falseButtonPressed.SetActive(!selectTrue.Value);
            falseButton.gameObject.SetActive(selectTrue.Value);
        }
    }

    IEnumerator ExitCoroutine() {
        status = Status.Exit;

        clock.Stop();

        Common.taskController.GameExit();

        yield return null;
    }

    /*
    // Task controller hívja meg ha történt valamilyen esemény
    // networkEvent változóban található a történt esemény
    // jsonNode-ban esetleg lehetnek további paraméterek az esemény kiegészítésére
    override public void EventHappened(JSONNode jsonNode)
    {
        switch (jsonNode[C.JSONKeys.gameEvent])
        {
            case C.NetworkGameEvent.SelectTrue:
                StartCoroutine(EvaluateCoroutine(true));
                break;
            case C.NetworkGameEvent.SelectFalse:
                StartCoroutine(EvaluateCoroutine(false));
                break;
        }
    }
    */

    /// <summary>
    /// Üzenet érkezett a hálózaton, amit a TaskController továbbított.
    /// </summary>
    /// <param name="networkEventType"></param>
    /// <param name="connectionID"></param>
    /// <param name="jsonNodeMessage"></param>
    override public void MessageArrived(NetworkEventType networkEventType, int connectionId, JSONNode jsonNodeMessage)
    {
        // Ős osztálynak is elküldjük a bejövő üzenetet
        base.MessageArrived(networkEventType, connectionId, jsonNodeMessage);

        switch (jsonNodeMessage[C.JSONKeys.gameEventType])
        {
            case C.JSONValues.answer:
                status = Status.Result;
                StartCoroutine(EvaluateCoroutine(jsonNodeMessage));

                break;

            case C.JSONValues.nextPlayer:
                status = Status.Play;

                break;
        }
    }

    // Egy eseményt küldünk a TaskManagernek, amit meg kell osztani a csoport többi játékosával is
    void SendEventGroup(string gameEvent) {
        JSONClass jsonClass = new JSONClass();
        jsonClass[C.JSONKeys.dataContent] = C.JSONValues.gameEvent;
        jsonClass[C.JSONKeys.gameEvent] = gameEvent;

        Common.taskController.SendMessageToServer(jsonClass);
    }

    // Ha rákattintottak a buborékra, akkor meghívódik ez az eljárás a buborékon levő Button szkript által
    override protected void ButtonClick(Button button)
    {
        base.ButtonClick(button);

        if (userInputIsEnabled)
        {
            // Ha még nem volt válasz
            if (selectTrue == null)
            {
                // Ha játékmódban vagyunk, akkor 
                switch (button.buttonType)
                {
                    case Button.ButtonType.TrueAnswer:
                    case Button.ButtonType.FalseAnswer:

                        JSONClass jsonClass = new JSONClass();
                        jsonClass[C.JSONKeys.dataContent] = C.JSONValues.gameEvent;
                        jsonClass[C.JSONKeys.gameEventType] = C.JSONValues.answer;
                        jsonClass[C.JSONKeys.selectedAnswer].AsBool = button.buttonType == Button.ButtonType.TrueAnswer;

                        Common.taskController.SendMessageToServer(jsonClass);

                        break;

                        /*
                    case Button.ButtonType.Exit: // Megnyomták az exit gombot
                        StartCoroutine(ExitCoroutine());

                        break;

                    case Button.ButtonType.SwitchLayout: // Megnyomták a layout váltó gombot
                        //layoutManager.ChangeLayout();
                        //SetPictures();
                        break;
                        */
                }
            }
        }
    }
}