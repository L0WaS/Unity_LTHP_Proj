using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

using SimpleJSON;
using UnityEngine.Networking;

public class Game_Read : TrueGameAncestor
{
    /// <summary>
    /// A kérdés sorszámát tartalmazó text
    /// </summary>
    [Tooltip("A kérdés sorszámát tartalmazó text")]
    GameObject questionNumberPrefab;
    /// <summary>
    /// A kérdések gombja, amit ki kell tölteni
    /// </summary>
    [Tooltip("A kérdések gombja, amit ki kell tölteni")]
    GameObject buttonTextPrefab;
    /// <summary>
    /// A kérdés szövege szavanként, amit nem lehet megnyomni
    /// </summary>
    [Tooltip("A kérdés szövege szavanként, amit nem lehet megnyomni")]
    GameObject textWordPrefab;

    /// <summary>
    /// Milyen széles a kérdés számát tartalmazó hely
    /// </summary>
    [Tooltip("Milyen széles a kérdés számát tartalmazó hely")]
    public float questionNumberWidth;
    /// <summary>
    /// Hány pixel van két szó között
    /// </summary>
    [Tooltip("Hány pixel van két szó között")]
    public float distanceBetweenWords;

    /// <summary>
    /// Hány pixel van két sor között
    /// </summary>
    [Tooltip("Hány pixel van két sor között")]
    public float distanceBetweenLines;
    /// <summary>
    /// Hány pixel van két kérdés között
    /// </summary>
    [Tooltip("Hány pixel van két kérdés között")]
    public float distanceBetweenQuestions;

    /// <summary>
    /// Hány pixel van a felső és alsó margó és a kérdések között
    /// </summary>
    [Tooltip("Hány pixel van a felső és alsó margó és a kérdések között")]
    public float distanceBetweenMargo;

    /// <summary>
    /// Hány pixel van a kérdés és a válasz gomb között függőlegesen
    /// </summary>
    [Tooltip("Hány pixel van a kérdés és a válasz gomb között függőlegesen")]
    public float distanceBetweenAnswers;

    /// <summary>
    /// A kérdés mennyire van a bal margótól (bal behúzás)
    /// </summary>
    [Tooltip("A kérdés mennyire van a bal margótól (bal behúzás)")]
    public float leftRetraction;

    CanvasGroup canvasGroup;            // A teljes canvas tartalom fade-eléséhez

    //Scrollbar scrollBar;              // A szöveg görgetősávja
    RectTransform rectTransformContentStory;    // A tartalom rectTransformja
    RectTransform rectTransformContentQuestion; // A tartalom rectTransformja

    GameObject scrollButtonsStory;
    GameObject scrollButtonsQuestion;

    Text storyText;

    GameObject questionHoldingPrefab;   // A kérdéseket tartó gameObject (ebből csinálunk egy másolatot és a másolatra tesszük a kérdéseket, hogy könnyebb legyen törölni)
    GameObject actQuestionHolding;      // A kérdéseket valóban tartalmazó gameObject, amit könnyen törölhetünk

    SpriteRenderer buttonOpen;          // A történet megnyitásának gombja, a történet lesz látható
    SpriteRenderer buttonClose;         // A történet bezárásának gombja, a kérdések lesznek láthatóak
    SpriteRenderer buttonShadow;        // A történet / kérdések választó gomb árnyéka

    SpriteRenderer transparentTop;      // A felső szöveg fade takaró
    SpriteRenderer transparentBottom;   // Az alsó szöveg fade takaró

    GameObject buttonPulse;             // A gomb pulzálásához
    bool buttonPulseIsEnabled = true;   // Engedélyezi a gomb pulzálását (csak addig kell pulzálnia míg nem veszik észre és rá nem kettintanak először)
    bool buttonPulseBig;                // A gomb mérete éppen nagy
    float buttonPulseLittleSize = 0.8f; // Mekkora legyen a kis gomb mérete
    float buttonPulseFrequency = 1.2f;  // Milyen sebességgel pulzáljon a gomb
    float remainingTime = 1.2f;         // Mennyi idő maradt a következő pulzálásig

    TaskReadData taskData;              // A feladatot tartalmazó objektum

    List<UIReadButton> listOfButton;    // A térténet kérdéseinek gombjai
    List<UIReadButton> listOfAnswers;   // Egy kérdéshez létrehozott válasz gombok listája

    bool storyShowed;                   // Ha true, akkor a történet van mutatva, ha false akkor a kérdések
    //float storyPos;                   // A történet görgetősávjának pozíciója
    //float questionPos;                // A kérdések görgetősávjának pozíciója

    UIReadButton selectedButton;        // A kiválaszott gomb ahová a választ kell "írni"

    float questionHeight;               // A kérdések magassága
    float answerBottomPos;              // Hol van a kérdésre adható válaszok alja

    int succesfullTask;                 // megoldott részfeladatok száma

    override public void Awake()
    {
        base.Awake();

        // Megkeressük a játékban használt prefabokat
        questionNumberPrefab = gameObject.SearchChild("UIReadTextQuestionNumber").gameObject;
        buttonTextPrefab = gameObject.SearchChild("UIReadButton").gameObject;
        textWordPrefab = gameObject.SearchChild("UIReadTextWord").gameObject;
        // Kikapcsoljuk a prefab-okat
        questionNumberPrefab.SetActive(false);
        buttonTextPrefab.SetActive(false);
        textWordPrefab.SetActive(false);

        canvasGroup = Common.SearchGameObject(gameObject, "CanvasText").GetComponent<CanvasGroup>();

        //scrollBar = Common.SearchGameObject(gameObject, "Scrollbar Vertical").GetComponent<Scrollbar>();
        rectTransformContentStory = Common.SearchGameObject(gameObject, "ContentStory").GetComponent<RectTransform>();
        rectTransformContentQuestion = Common.SearchGameObject(gameObject, "ContentQuestions").GetComponent<RectTransform>();

        scrollButtonsStory = Common.SearchChild(gameObject, "ScrollButtonsStory").gameObject;
        scrollButtonsQuestion = Common.SearchChild(gameObject, "ScrollButtonsQuestion").gameObject;

        storyText = Common.SearchGameObject(gameObject, "Text").GetComponent<Text>();
        questionHoldingPrefab = Common.SearchGameObject(gameObject, "QuestionsHolding").gameObject;

        buttonOpen = Common.SearchGameObject(gameObject, "ButtonOpen").GetComponent<SpriteRenderer>();
        buttonClose = Common.SearchGameObject(gameObject, "ButtonClose").GetComponent<SpriteRenderer>();
        buttonShadow = Common.SearchGameObject(gameObject, "ButtonShadow").GetComponent<SpriteRenderer>();

        transparentTop = Common.SearchGameObject(gameObject, "TransparentTop").GetComponent<SpriteRenderer>();
        transparentBottom = Common.SearchGameObject(gameObject, "TransparentBottom").GetComponent<SpriteRenderer>();

        buttonPulse = gameObject.SearchChild("Button").gameObject;

        // Gombok szkriptjének beállítása
        foreach (Button button in GetComponentsInChildren<Button>(true))
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

        canvasGroup.alpha = 0;

        // A történetet és a címét elhelyezzük a Text componensben
        string text = "\n";

        if (!string.IsNullOrEmpty(taskData.textTitle)) // Ha van cím hozzá adjuk
            text += "<b>" + taskData.textTitle + "</b>\n\n";

        storyText.text = text + "     " + taskData.textContent + "\n";

        //storyPos = 1;
        //questionPos = 1;

        // Kérdések létrehozása -------------------------------------------------------------------------------------

        // Az esetlegesen létező előző kérdéseket eltávolítjuk
        Destroy(actQuestionHolding);

        // Készítünk egy másolatot a kérdések tárolására készített gameObject-ből
        actQuestionHolding = Instantiate(questionHoldingPrefab);
        actQuestionHolding.transform.SetParent(questionHoldingPrefab.transform.parent, false);

        //float fullWidth = ((RectTransform)questionHoldingPrefab.transform).rect.width; // sizeDelta.x;    // Mekkora a szélesség amivel dolgozhatunk
        float fullWidth = questionHoldingPrefab.GetComponent<LayoutElement>().preferredWidth;
        //fullWidth = 767;

        float cursorX = 0;
        float cursorY = -distanceBetweenMargo;

        listOfButton = new List<UIReadButton>();





        //for (int i = 0; i < 20; i++)
        //{
        //    taskData.questions.Add(taskData.questions[0]);
        //}






        for (int i = 0; i < taskData.questions.Count; i++)
        {
            cursorX = 0;

            // A kérdés sorszámát elhelyezzük legelőre
            UIReadTextQuestionNumber questionNumber = Instantiate(questionNumberPrefab, actQuestionHolding.transform, false).GetComponent<UIReadTextQuestionNumber>();
            questionNumber.gameObject.SetActive(true);
            //questionNumber.transform.SetParent(actQuestionHolding.transform, false);
            questionNumber.Init((i + 1).ToString());
            //questionNumber.ChangeTextColor(layoutManager.GetColor("textColor"));
            questionNumber.transform.localPosition = new Vector2(cursorX, cursorY);

            cursorX = leftRetraction;

            // Feldaraboljuk a kérdést a szóközök mentén
            string[] substrings = taskData.questions[i].question.Split(' ');
            int questionCounter = 0; // Kérdéseket számolja a szövegben, hogy tudjam melyik kérdéshez melyik lehetőségeket kell társítani

            // Végig megyünk a kapott rész stringeken
            foreach (string item in substrings)
            {
                float gameObjectWidth = 0;
                GameObject newGameObject = null;

                // Ha valamelyik szó ??, akkor az egy kérdést jelent
                if (item == "??")
                {
                    UIReadButton button = Instantiate(buttonTextPrefab, actQuestionHolding.transform, false).GetComponent<UIReadButton>();
                    button.gameObject.SetActive(true);
                    //button.transform.SetParent(actQuestionHolding.transform, false);
                    button.Init(taskData.questions[i].answerGroups[questionCounter], ButtonQuestionClick, i, questionCounter);

                    gameObjectWidth = button.GetWidth();
                    newGameObject = button.gameObject;

                    questionCounter++;
                    listOfButton.Add(button);
                    //SetPicturesOnReadButton(button);
                }
                else
                {   // Egyébként létrehozzuk a szót ha nem üres string
                    if (string.IsNullOrEmpty(item)) continue;

                    UIReadTextWord textWord = Instantiate(textWordPrefab, actQuestionHolding.transform, false).GetComponent<UIReadTextWord>();
                    textWord.gameObject.SetActive(true);
                    //textWord.transform.SetParent(actQuestionHolding.transform, false);
                    textWord.Init(item);
                    //textWord.ChangeTextColor(layoutManager.GetColor("textColor"));

                    gameObjectWidth = textWord.GetWidth();
                    newGameObject = textWord.gameObject;
                }

                // Elhelyezzük az objektumot
                // Ha nem fér el már az adott sorba, akkor következő sorba lépünk ha már van a sorba legalább egy elem
                if (cursorX + gameObjectWidth > fullWidth && cursorX > leftRetraction)
                {
                    cursorX = leftRetraction;
                    cursorY += distanceBetweenLines;
                }

                newGameObject.transform.localPosition = new Vector2(cursorX, cursorY);
                cursorX += gameObjectWidth + distanceBetweenWords;
            }

            cursorY += distanceBetweenQuestions;
        }

        questionHeight = Mathf.Abs(cursorY);

        ShowStory(true);

        selectedButton = null;
        listOfAnswers = null;
        succesfullTask = 0;

        buttonPulseIsEnabled = true;
        remainingTime = buttonPulseFrequency;

        yield break;
    }

    // A ScreenController hívja meg ezt a metódust ha ez lesz a következő kép amit meg kell mutatni. 
    // A játéknak alaphelyzetbe kell állítania magát
    override public IEnumerator InitCoroutine()
    {
        status = Status.Init;

        // Lekérdezzük a feladat adatait
        taskData = (TaskReadData)Common.taskController.task;

        yield return StartCoroutine(PrepareTask());



        yield return null;
    }

    // Amikor az új képernyő megjelenítése befejeződik, akkor ezt meghívja a ScreenController
    override public IEnumerator ScreenShowFinishCoroutine()
    {
        // A szöveg előtűnik
        iTween.ValueTo(gameObject, iTween.Hash("from", 0, "to", 1, "time", taskData.animSpeed1, "easetype", iTween.EaseType.linear, "onupdate", "FadeCanvasUpdate", "onupdatetarget", gameObject));
        
        yield return new WaitForSeconds(taskData.animSpeed1); // Várunk amíg az animáció befejeződik

        Common.audioController.SetBackgroundMusic(1, 0.05f, 4); // Elindítjuk a háttérzenét

        status = Status.Play;
        Common.HHHnetwork.messageProcessingEnabled = true;

        yield return null;
    }

    // iTween ValueTo call this
    public void FadeCanvasUpdate(float value)
    {
        canvasGroup.alpha = value;
    }

    // A kérdéshez tartozó játék elemeket kell eltávolítani, ha meghívják ezt a metódust. A TaskController fogja új feladatnál ha az új feladatot ugyan annak a képernyőnek kell megjelenítenie
    // Meglehet adni neki egy callBack függvényt, amit akkor hív meg ha végzet a játék elemek elrejtésével, mivel ez sokáig is eltarthat és addig nem kéne tovább menni az új feladatra.
    override public IEnumerator HideGameElement()
    {
        clock.Reset(1); // Az órát alaphelyzetbe állítja

        iTween.ValueTo(gameObject, iTween.Hash("from", 1, "to", 0, "time", taskData.animSpeed1, "easetype", iTween.EaseType.linear, "onupdate", "FadeCanvasUpdate", "onupdatetarget", gameObject));

        yield return new WaitForSeconds(taskData.animSpeed1); // Várunk amíg az animáció befejeződik
    }

    /// <summary>
    /// A tanári tablet óraterv előnézeti képernyője hívja meg ha meg kell mutatni a játék előnézetét.
    /// A task paraméter tartalmazza a játék képernyőjének adatait.
    /// </summary>
    /// <param name="task">A megjelenítendő képernyő adata</param>
    override public IEnumerator Preview(TaskAncestor task)
    {
        taskData = (TaskReadData)task;

        yield return StartCoroutine(PrepareTask());

        remainingTime = float.MaxValue;
        canvasGroup.alpha = 1;
    }

    // Update is called once per frame
    new void Update()
    {
        base.Update();

        //rectTransformContent.sizeDelta = new Vector2(0, (storyShowed) ? storyText.preferredHeight : questionHeight);
        rectTransformContentStory.sizeDelta = new Vector2(0, storyText.preferredHeight);
        rectTransformContentQuestion.sizeDelta = new Vector2(0, Mathf.Max(questionHeight, answerBottomPos));

        // Váltó gomb pulzálása
        remainingTime -= Time.deltaTime;

        if (buttonPulseBig && !buttonPulseIsEnabled)
            remainingTime = float.MaxValue;

        if (remainingTime < 0) {

            /*
            buttonPulse.transform.localScale = Vector3.one * 0.5f;
            iTween.ScaleTo(buttonPulse, iTween.Hash("islocal", true, "scale", Vector3.one, "time", 1, "easeType", iTween.EaseType.easeOutElastic));
            */

            buttonPulseBig = !buttonPulseBig;
            iTween.ScaleTo(buttonPulse, iTween.Hash("islocal", true, "scale", Vector3.one * ((buttonPulseBig)? 1 : buttonPulseLittleSize), "time", 1, "easeType", iTween.EaseType.easeOutCubic));


            remainingTime = buttonPulseFrequency;
        }

        /*
        menu.menuEnabled = (status == Status.Play);

        if (status == Status.Play) // Ha megy a játék, akkor megy az óra
            clock.Go();
        else
            clock.Stop();
            */
    }

    // Játéknak vége letelt az idő, vagy a játék befejeződött
    override public IEnumerator GameEnd()
    {
        status = Status.Result;
        //clock.Stop();

        yield return new WaitForSeconds(2);

        // Tájékoztatjuk a feladatkezelőt, hogy vége a játéknak és átadjuk a játékos eredményeit
        Common.taskControllerOld.TaskEnd(null);
    }



    // A menüből kiválasztották a kilépést a játékból
    /*
    IEnumerator ExitCoroutine()
    {
        status = Status.Exit;
        clock.Stop();

        Common.taskController.GameExit();
        yield return null;
    }
    */

    /// <summary>
    /// Beállíthatjuk, hogy a történetet akarjuk látni vagy a kérdéseket.
    /// Ha a Show változó true, akkor a történetet.
    /// </summary>
    /// <param name="show">Ha true, akkor a történet lesz látható egyébként a kérdések.</param>
    void ShowStory(bool show) {
        // Megjegyezzük az aktuális görgetősáv pozícióját
        /*
        if (storyShowed)
            storyPos = scrollBar.value;
        else 
            questionPos = scrollBar.value;
        */
        buttonPulseIsEnabled = false;
        //remainingTime = float.MaxValue;

        // Beállítjuk a gombok láthatóságát
        buttonOpen.gameObject.SetActive(!show);
        buttonClose.gameObject.SetActive(show);

        // Beállítjuk a tartalom méretét
        // rectTransformContent.sizeDelta = new Vector2(0, (show) ? storyText.preferredHeight : Mathf.Max(questionHeight, answerBottomPos));


        // Beállítjuk melyik tartalom legyen látható
        rectTransformContentStory.parent.parent.gameObject.SetActive(show);
        rectTransformContentQuestion.parent.parent.gameObject.SetActive(!show);

        scrollButtonsStory.SetActive(show);
        scrollButtonsQuestion.SetActive(!show);

        //storyText.gameObject.SetActive(show);
        //actQuestionHolding.SetActive(!show);

        // Beállítjuk a görgetősáv értékét a korábbira
        //scrollBar.value = (show) ? storyPos : questionPos;

        storyShowed = show;
    }

    /// <summary>
    /// Létrehozza a buttonID változóban megadott gomb válasz lehetőségeit.
    /// </summary>
    /// <param name="buttonID"></param>
    void ShowAnswer(UIReadButton questionButton) 
    {
        questionButton.Selected(true);

        DeleteAnswer();
        listOfAnswers = new List<UIReadButton>();

        float fullWidth = questionHoldingPrefab.GetComponent<LayoutElement>().preferredWidth;
        float questionButtonCenter = questionButton.transform.localPosition.x + questionButton.GetWidth() / 2;
        float cursorX = 0;
        float cursorY = questionButton.transform.localPosition.y + distanceBetweenAnswers;
        int firstButtonIndexActualLine = 0;
        foreach (string answer in questionButton.GetAnswers())
        {
            UIReadButton answerButton = Instantiate(buttonTextPrefab, actQuestionHolding.transform, false).GetComponent<UIReadButton>();
            answerButton.gameObject.SetActive(true);
            //answerButton.transform.SetParent(actQuestionHolding.transform, false);
            answerButton.Init(answer, ButtonAnswerClick);

            // Elhelyezzük az új válasz gombot
            // Ha nem fér el már az adott sorba, akkor következő sorba lépünk
            if (cursorX + answerButton.GetWidth() > fullWidth && cursorX > 0)
            {
                AdjustAnswerButtons(questionButtonCenter, cursorX, fullWidth, firstButtonIndexActualLine, listOfAnswers);

                cursorX = 0;
                cursorY += distanceBetweenAnswers;

                firstButtonIndexActualLine = listOfAnswers.Count;
            }

            answerButton.transform.localPosition = new Vector2(cursorX, cursorY);
            cursorX += answerButton.GetWidth() + distanceBetweenWords;

            listOfAnswers.Add(answerButton);
            //SetPicturesOnReadButton(answerButton);
        }

        AdjustAnswerButtons(questionButtonCenter, cursorX, fullWidth, firstButtonIndexActualLine, listOfAnswers);

        answerBottomPos = Mathf.Abs(cursorY + distanceBetweenAnswers - distanceBetweenMargo);

        selectedButton = questionButton;
    }

    /// <summary>
    /// A létrehozott válasz gombokat a kérdés gomb alá igazítja.
    /// </summary>
    /// <param name="questionButtonCenter">Hol van a kérdés gomb közepe.</param>
    /// <param name="width">A létrehozott válasz gomb sor milyen széles.</param>
    /// <param name="fullWidth">Mennyi a rendelkezésre álló hely teljes szélessége.</param>
    /// <param name="firstButtonIndexActualLine">Mi az indexe az aktuális sor első válasz gombjának.</param>
    /// <param name="listOfAnswers">A létrehozott válasz gombok.</param>
    void AdjustAnswerButtons(float questionButtonCenter, float width, float fullWidth, int firstButtonIndexActualLine, List<UIReadButton> listOfAnswers) {
        // A maradék helyett elosztjuk, és a válaszokat a kérdés alá igazítjuk
        if (width > 0)
            width -= distanceBetweenWords;

        // Hol kell lennie az első válasznak
        float basePosX = questionButtonCenter - width / 2;

        if (basePosX < 0)
            basePosX = 0;

        if (basePosX + width > fullWidth)
            basePosX = fullWidth - width;

        // Végig megyünk az aktuális sorban létrehozott gombokon és igazítjuk az X koordinátáját
        for (int i = firstButtonIndexActualLine; i < listOfAnswers.Count; i++)
        {
            Vector2 pos = listOfAnswers[i].transform.localPosition;
            listOfAnswers[i].transform.localPosition = new Vector2(pos.x + basePosX, pos.y);
        }
    }

    /// <summary>
    /// Törli egy kérdés gomb válaszlehetőségeit
    /// </summary>
    void DeleteAnswer()
    {
        if (listOfAnswers != null)
            foreach (UIReadButton button in listOfAnswers)
                Destroy(button.gameObject);

        answerBottomPos = 0;
        listOfAnswers = null;
    }

    /// <summary>
    /// Kiértékeli a program a megadott választ.
    /// </summary>
    /// <param name="answer">A kérdésre a válasz.</param>
    IEnumerator EvaluateAnswerCoroutine(JSONNode jsonData) 
    {
        int questionIndex = jsonData[C.JSONKeys.selectedQuestion].AsInt;
        int subQuestionIndex = jsonData[C.JSONKeys.selectedSubQuestion].AsInt;

        // Megkeressük a kérdést
        selectedButton = null;
        foreach (var item in listOfButton)
        {
            if (item.questionIndex == questionIndex && item.subQuestionIndex == subQuestionIndex)
            {
                selectedButton = item;
                break;
            }
        }

        // Nem találtuk meg, kilépünk
        if (selectedButton == null)
            yield break;

        DeleteAnswer();
        ShowAnswer(selectedButton);

        if (Common.configurationController.answerFeedback != ConfigurationController.AnswerFeedback.Immediately)
        {
            selectedButton.ShowText(jsonData[C.JSONKeys.selectedAnswer].Value);
            DeleteAnswer();
        }
        else
        {
            switch (jsonData[C.JSONKeys.evaluateAnswer].Value)
            {
                case C.JSONValues.evaluateIsTrue:
                    // Helyes a válasz
                    DeleteAnswer();

                    selectedButton.ShowText(jsonData[C.JSONKeys.selectedAnswer]);
                    Common.audioController.SFXPlay("positive");
                    yield return new WaitForSeconds(selectedButton.Flashing(true));

                    break;

                case C.JSONValues.evaluateIsFalse:
                    // A válasz helytelen

                    // Megkeressük azt a választ gombot, amit megnyomtak, hogy tudjuk pirossal villogtatni
                    UIReadButton answerButton = null;

                    foreach (UIReadButton button in listOfAnswers)
                    {
                        if (button.GetText() == jsonData[C.JSONKeys.selectedAnswer].Value)
                        {
                            answerButton = button;
                            break;
                        }
                    }

                    Common.audioController.SFXPlay("negative");
                    yield return new WaitForSeconds(answerButton.Flashing(false));

                    DeleteAnswer();
                    selectedButton.Selected(false);

                    break;
            }
        }

        selectedButton = null;
    }

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

        if (networkEventType == NetworkEventType.DataEvent)
        {
            switch (jsonNodeMessage[C.JSONKeys.gameEventType])
            {
                case C.JSONValues.answer:
                    status = Status.Result;
                    StartCoroutine(EvaluateAnswerCoroutine(jsonNodeMessage));

                    break;

                case C.JSONValues.nextPlayer:
                    status = Status.Play;

                    break;
            }
        }
    }

    // Rákattintottak egy kérdésre
    void ButtonQuestionClick(Object o)
    {
        if (userInputIsEnabled)
        {
            UIReadButton button = (UIReadButton)o;

            if (selectedButton == null)
            {
                ShowAnswer(button);
            }
            else if (selectedButton == button)
            {
                selectedButton.Selected(false);
                selectedButton = null;
                DeleteAnswer();
            }
        }
    }

    // Rákattintottak egy válaszra
    void ButtonAnswerClick(Object o)
    {
        if (userInputIsEnabled)
        {
            UIReadButton button = (UIReadButton)o;

            // Ha játékmódban vagyunk, akkor elküldjük a játékos választását
            JSONClass jsonClass = new JSONClass();
            jsonClass[C.JSONKeys.dataContent] = C.JSONValues.gameEvent;
            jsonClass[C.JSONKeys.gameEventType] = C.JSONValues.answer;
            jsonClass[C.JSONKeys.selectedQuestion].AsInt = selectedButton.questionIndex;
            jsonClass[C.JSONKeys.selectedSubQuestion].AsInt = selectedButton.subQuestionIndex;
            jsonClass[C.JSONKeys.selectedAnswer] = button.GetText();

            Common.taskController.SendMessageToServer(jsonClass);
        }
    }

    // Ha rákattintottak egy gombra, akkor meghívódik ez az eljárás a Button szkript által
    override protected void ButtonClick(Button button)
    {
        base.ButtonClick(button);

        if (userInputIsEnabled)
        {
            switch (button.buttonType)
            {
                /*
                case Button.ButtonType.Exit:
                    StartCoroutine(ExitCoroutine());
                    break;
                    */

                case Button.ButtonType.ShowStory:
                    ShowStory(true);
                    buttonPulseIsEnabled = false; // Leállítjuk a váltógomb pulzálását
                    break;

                case Button.ButtonType.ShowQuestion:
                    ShowStory(false);
                    break;
            }
        }
    }
}

