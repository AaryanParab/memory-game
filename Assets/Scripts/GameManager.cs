using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.Serialization;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [Header("References - Assign in Inspector")]
    public GameObject cardPrefab;
    public Transform boardParent;
    public GridLayoutGroup gridLayout;
    
    public TextMeshProUGUI txtScore;
    public TextMeshProUGUI txtMatches;
    public TextMeshProUGUI txtTurns;
    
    public GameObject pauseMenu;
    public Button btnResume;
    public Button btnRestart;
    [FormerlySerializedAs("btn3x3")] public Button btn3x4;
    public Button btn4x4;
    [FormerlySerializedAs("btn5x5")] public Button btn5x6;

    private List<Card> cards = new List<Card>();
    private List<Card> flippedCards = new List<Card>();

    private int rows = 4, cols = 4;
    
    private int matchesFound = 0;
    private int totalTurns = 0;
    private int score = 0;
    private int totalPairs;

    private bool isPaused = false;

    private AudioSource audioSource;
    public AudioClip flipSound, matchSound, mismatchSound, gameOverSound;

    private const string SAVE_KEY = "CardMatchSave";
    
    private const float CARD_ASPECT_RATIO = 1.4f;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        
        if (btnResume)   btnResume.onClick.AddListener(ResumeGame);
        if (btnRestart)  btnRestart.onClick.AddListener(() => NewGame(rows, cols));
        if (btn3x4)      btn3x4.onClick.AddListener(() => NewGame(3, 4));
        if (btn4x4)      btn4x4.onClick.AddListener(() => NewGame(4, 4));
        if (btn5x6)      btn5x6.onClick.AddListener(() => NewGame(5, 6));
    }

    private void Start()
    {
        if (pauseMenu) pauseMenu.SetActive(false);
        LoadProgress();
        if (cards.Count == 0)
        {
            NewGame(4, 4);
        }
        UpdateUI();
    }

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        if (pauseMenu) pauseMenu.SetActive(isPaused);
    }

    private void ResumeGame()
    {
        isPaused = false;
        if (pauseMenu) pauseMenu.SetActive(false);
    }

    public void NewGame(int r, int c)
    {
        rows = r;
        cols = c;
        GenerateBoard(r, c);
        ResumeGame();
        SaveProgress();
    }

    public void GenerateBoard(int r, int c)
    {
        rows = r;
        cols = c;
        ClearBoard();

        RectTransform boardRT = boardParent.GetComponent<RectTransform>();
        
        boardRT.anchorMin = new Vector2(0.20f, 0f);
        boardRT.anchorMax = Vector2.one;
        boardRT.anchoredPosition = Vector2.zero;
        boardRT.sizeDelta = Vector2.zero;
        
        gridLayout.padding = new RectOffset(20, 20, 50, 50);
        
        gridLayout.childAlignment = TextAnchor.MiddleCenter;

        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = c;
        
        float dynamicSpacingX = Mathf.Max(12f, Screen.width  * 0.045f);
        float dynamicSpacingY = Mathf.Max(12f, Screen.height * 0.045f);
        gridLayout.spacing = new Vector2(dynamicSpacingX, dynamicSpacingY);
        
        List<int> ids = Enumerable.Range(0, (r * c) / 2).ToList();
        ids.AddRange(ids);
        ids = ids.OrderBy(x => Random.value).ToList();

        for (int i = 0; i < r * c; i++)
        {
            GameObject go = Instantiate(cardPrefab, boardParent);
            Card card = go.GetComponent<Card>();
            card.id = ids[i];
            card.frontSprite = GetSpriteForId(card.id);
            card.backSprite = GetBackSprite();
            cards.Add(card);

            Button btn = go.GetComponent<Button>();
            btn.onClick.AddListener(() => OnCardClicked(card));
        }
        
        LayoutRebuilder.ForceRebuildLayoutImmediate(boardRT);
        
        float paddingH = gridLayout.padding.left + gridLayout.padding.right;
        float paddingV = gridLayout.padding.top + gridLayout.padding.bottom;
        float spacingH = gridLayout.spacing.x * (cols - 1);
        float spacingV = gridLayout.spacing.y * (rows - 1);

        float availableWidth  = boardRT.rect.width  - paddingH - spacingH;
        float availableHeight = boardRT.rect.height - paddingV - spacingV;

        float cellWidth  = availableWidth / cols;
        float cellHeight = availableHeight / rows;

        if (cellHeight / CARD_ASPECT_RATIO < cellWidth)
            cellWidth = cellHeight / CARD_ASPECT_RATIO;
        else
            cellHeight = cellWidth * CARD_ASPECT_RATIO;

        gridLayout.cellSize = new Vector2(cellWidth, cellHeight);

        totalPairs = (r * c) / 2;
        matchesFound = 0;
        totalTurns = 0;
        score = 0;
        UpdateUI();
    }

    private void ClearBoard()
    {
        foreach (Transform child in boardParent)
            Destroy(child.gameObject);
        cards.Clear();
        flippedCards.Clear();
    }

    private Sprite GetSpriteForId(int id)
    {
        int texWidth = 128;
        int texHeight = Mathf.RoundToInt(128 * CARD_ASPECT_RATIO);

        Texture2D tex = new Texture2D(texWidth, texHeight);
        Color color = new Color((id % 5) * 0.2f + 0.3f, (id % 3) * 0.3f + 0.3f, (id % 7) * 0.15f + 0.4f);
        
        for (int x = 0; x < texWidth; x++)
            for (int y = 0; y < texHeight; y++)
                tex.SetPixel(x, y, color);
        
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, texWidth, texHeight), new Vector2(0.5f, 0.5f));
    }

    private Sprite GetBackSprite()
    {
        int texWidth = 128;
        int texHeight = Mathf.RoundToInt(128 * CARD_ASPECT_RATIO);

        Texture2D tex = new Texture2D(texWidth, texHeight);
        for (int x = 0; x < texWidth; x++)
            for (int y = 0; y < texHeight; y++)
                tex.SetPixel(x, y, new Color(0.1f, 0.1f, 0.2f));
        
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, texWidth, texHeight), new Vector2(0.5f, 0.5f));
    }

    private void OnCardClicked(Card card)
    {
        if (isPaused || card.IsFlipped() || card.IsMatched()) return;

        card.Flip(true);
        flippedCards.Add(card);
        PlaySound("flip");

        if (flippedCards.Count == 2)
        {
            Card c1 = flippedCards[0];
            Card c2 = flippedCards[1];
            flippedCards.Clear();
            StartCoroutine(CheckMatch(c1, c2));
        }
    }

    private IEnumerator CheckMatch(Card c1, Card c2)
    {
        yield return new WaitForSeconds(0.3f);

        totalTurns++;

        if (c1.id == c2.id)
        {
            c1.SetMatched();
            c2.SetMatched();
            matchesFound++;
            PlaySound("match");

            if (matchesFound == totalPairs)
            {
                PlaySound("gameover");
                Debug.Log($"🎉 Game Won! Matches: {matchesFound} | Turns: {totalTurns} | Score: {score}");
            }
        }
        else
        {
            c1.Flip(false);
            c2.Flip(false);
            PlaySound("mismatch");
        }

        score = (matchesFound * 200) - (totalTurns * 10);
        if (score < 0) score = 0;

        UpdateUI();
        SaveProgress();
    }

    private void UpdateUI()
    {
        if (txtScore != null)   txtScore.text   = $"Score: {score}";
        if (txtMatches != null) txtMatches.text = $"Matches: {matchesFound}/{totalPairs}";
        if (txtTurns != null)   txtTurns.text   = $"Turns: {totalTurns}";
    }

    private void PlaySound(string type)
    {
        AudioClip clip = type switch
        {
            "flip"      => flipSound,
            "match"     => matchSound,
            "mismatch"  => mismatchSound,
            "gameover"  => gameOverSound,
            _ => null
        };

        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    [System.Serializable]
    private class GameSave
    {
        public int rows, cols;
        public int score;
        public int matchesFound;
        public int totalTurns;
    }

    public void SaveProgress()
    {
        GameSave save = new GameSave
        {
            rows = rows,
            cols = cols,
            score = score,
            matchesFound = matchesFound,
            totalTurns = totalTurns
        };
        PlayerPrefs.SetString(SAVE_KEY, JsonUtility.ToJson(save));
        PlayerPrefs.Save();
    }

    public void LoadProgress()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY)) return;

        GameSave save = JsonUtility.FromJson<GameSave>(PlayerPrefs.GetString(SAVE_KEY));
        score = save.score;
        matchesFound = save.matchesFound;
        totalTurns = save.totalTurns;
        GenerateBoard(save.rows, save.cols);
    }
}