using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GameManager;

public class GameManager : MonoBehaviour
{
    public event Action<eStateGame> StateChangedAction = delegate { };

    public enum eLevelMode
    {
        TIMER,
        MOVES
    }

    public enum eStateGame
    {
        SETUP,
        MAIN_MENU,
        GAME_STARTED,
        PAUSE,
        GAME_OVER,
    }

    private eStateGame m_state;
    public eStateGame State
    {
        get { return m_state; }
        private set
        {
            m_state = value;

            StateChangedAction(m_state);
        }
    }
    public static GameManager Instance { get; private set; }

    private GameSettings m_gameSettings;
    private Reskin m_reskin;
    private BoardController m_boardController;

    private UIMainManager m_uiMenu;
    private LevelCondition m_levelCondition;
    private eLevelMode m_elevelMode;
    private GameObject m_cellBackground;
    private GameObject[] m_itemNormals;

    private void Awake()
    {
        State = eStateGame.SETUP;
        Instance = this;
        m_gameSettings = Resources.Load<GameSettings>(Constants.GAME_SETTINGS_PATH);
        m_reskin = Resources.Load<Reskin>(Constants.SKIN_PATH);
        m_uiMenu = FindObjectOfType<UIMainManager>();
        m_uiMenu.Setup(this);
        m_cellBackground = Resources.Load<GameObject>(Constants.PREFAB_CELL_BACKGROUND);
        InitItemNormal();
    }

    private void InitItemNormal()
    {
        m_itemNormals = new GameObject[10];
        string path = "prefabs/itemNormal0";
        for (int i = 0; i < 7; i++)
        {
            m_itemNormals[i] = Resources.Load<GameObject>(path + (i + 1).ToString());
            SpriteRenderer render = m_itemNormals[i].GetComponent<SpriteRenderer>();
            render.sprite = m_reskin.Skins[i];
        }
        m_itemNormals[7] = Resources.Load<GameObject>(Constants.PREFAB_BONUS_BOMB);
        m_itemNormals[8] = Resources.Load<GameObject>(Constants.PREFAB_BONUS_HORIZONTAL);
        m_itemNormals[9] = Resources.Load<GameObject>(Constants.PREFAB_BONUS_VERTICAL);
    }

    void Start()
    {
        State = eStateGame.MAIN_MENU;
    }

    // Update is called once per frame
    void Update()
    {
        if (m_boardController != null) m_boardController.Update();
    }


    internal void SetState(eStateGame state)
    {
        State = state;

        if(State == eStateGame.PAUSE)
        {
            DOTween.PauseAll();
        }
        else
        {
            DOTween.PlayAll();
        }
    }

    public void LoadLevel(eLevelMode mode)
    {
        m_boardController = new GameObject("BoardController").AddComponent<BoardController>();
        m_boardController.StartGame(this, m_gameSettings);
        m_elevelMode = mode;
        if (mode == eLevelMode.MOVES)
        {
            m_levelCondition = this.gameObject.AddComponent<LevelMoves>();
            m_levelCondition.Setup(m_gameSettings.LevelMoves, m_uiMenu.GetLevelConditionView(), m_boardController);
        }
        else if (mode == eLevelMode.TIMER)
        {
            m_levelCondition = this.gameObject.AddComponent<LevelTime>();
            m_levelCondition.Setup(m_gameSettings.LevelMoves, m_uiMenu.GetLevelConditionView(), this);
        }

        m_levelCondition.ConditionCompleteEvent += GameOver;
        State = eStateGame.GAME_STARTED;
    }

    public void GameOver()
    {
        StartCoroutine(WaitBoardController());
    }

    internal void ClearLevel()
    {
        if (m_boardController)
        {
            m_boardController.Clear();
            Destroy(m_boardController.gameObject);
            m_boardController = null;
        }
    }

    private IEnumerator WaitBoardController()
    {
        while (m_boardController.IsBusy)
        {
            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForSeconds(1f);

        State = eStateGame.GAME_OVER;

        if (m_levelCondition != null)
        {
            m_levelCondition.ConditionCompleteEvent -= GameOver;

            Destroy(m_levelCondition);
            m_levelCondition = null;
        }
    }
    public void Restart()
    {
        StartCoroutine(RestartCoroutine());
    }
    private IEnumerator RestartCoroutine()
    {
        while (m_boardController.IsBusy)
        {
            yield return new WaitForEndOfFrame();
        }
        ClearLevel();
        if (m_levelCondition != null)
        {
            m_levelCondition.ConditionCompleteEvent -= GameOver;
            Destroy(m_levelCondition);
            m_levelCondition = null;
        }
        yield return new WaitForEndOfFrame();
        LoadLevel(m_elevelMode);
    }
    public GameObject GetItemNormal(string path)
    {
        switch (path)
        {
            case Constants.PREFAB_BONUS_BOMB:
                return m_itemNormals[7];
            case Constants.PREFAB_BONUS_HORIZONTAL:
                return m_itemNormals[8];
            case Constants.PREFAB_BONUS_VERTICAL:
                return m_itemNormals[9];
            default:
                int index = path[path.Length - 1] - 49;
                return m_itemNormals[index];
        }
    }
    public GameObject GetCellBackGround()
    {
        return m_cellBackground;
    }
}
