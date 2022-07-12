using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum GameMode
{
    preGame, 
    loading, // загрузка списка слов
    makeLevel, // создается отдельный уровень
    levelPrep, // рендер уровня
    inLevel // в игре
}
public class WordGame : MonoBehaviour
{
    static public WordGame S;

    [Header("Set in Inspector")]
    public GameObject prefabLetter;
    public Rect wordArea = new Rect(-24, 19, 48, 28);
    public float letterSize = 1.5f;
    public bool showAllWyrds = true;
    public float bigLetterSize = 4f;
    public Color bigColorDim = new Color(0.8f, 0.8f, 0.8f);
    public Color bigColorSelected = new Color(1f, 0.9f, 0.7f);
    public Vector3 bigLetterCenter = new Vector3(0, -16, 0);
    public Color[] wyrdPalette;

    [Header("Set Dynamically")]
    public GameMode mode = GameMode.preGame;
    public WordLevel currLevel;
    public List<Wyrd> wyrds; // список всех слов уровня
    public List<Letter> bigLetters;
    public List<Letter> bigLettersActive;
    public string testWord;

    private string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    private Transform letterAnchor, bigLetterAnchor;

    private void Awake()
    {
        S = this;
        letterAnchor = new GameObject("LetterAnchor").transform;
        bigLetterAnchor = new GameObject("BigLetterAnchor").transform;
    }
    // Start is called before the first frame update
    void Start()
    {
        mode = GameMode.loading;
        WordList.INIT();
    }

    //вызывается методом SendMessage из WordList
    public void WordListParseComplete()
    {
        mode = GameMode.makeLevel;
        currLevel = MakeWordLevel();
    }
    public WordLevel MakeWordLevel(int levelNum = -1)
    {
        WordLevel level = new WordLevel();
        if (levelNum == -1)
            level.longWordIndex = Random.Range(0, WordList.LONG_WORD_COUNT);
        else
            ;
        level.levelNum = levelNum;
        level.word = WordList.GET_LONG_WORD(level.longWordIndex);
        level.charDict = WordLevel.MakeCharDict(level.word);

        StartCoroutine(FindSubWordsCoroutine(level));
        return (level);
    }
    public IEnumerator FindSubWordsCoroutine(WordLevel level)
    {
        level.subWords = new List<string>();
        string str;

        List<string> words = WordList.GET_WORDS();
        for(int i = 0; i < WordList.WORD_COUNT; i++)
        {
            str = words[i];
            if (WordLevel.CheckWordInLevel(str, level))
                level.subWords.Add(str);
            if (i % WordList.NUM_TO_PARSE_BEFORE_YIELD == 0)
                yield return null;
        }

        level.subWords.Sort();
        level.subWords = SortWordsByLength(level.subWords).ToList();
        SubWordSearchComplete();
    }
    public static IEnumerable<string> SortWordsByLength(IEnumerable<string> ws)
    {
        ws = ws.OrderBy(s => s.Length);
        return ws;
    }

    public void SubWordSearchComplete()
    {
        mode = GameMode.levelPrep;
        Layout();
    }
    void Layout()
    {
        //поместить на экрон плитки с буквами каждого возможного слова текущего уровня
        wyrds = new List<Wyrd>();

        //обьявить локальные переменные, которые будут пользоваться методом
        GameObject go;
        Letter lett;
        string word;
        Vector3 pos;
        float left = 0;
        float columnWidth = 3;
        char c;
        Color col;
        Wyrd wyrd;

        int numRows = Mathf.RoundToInt(wordArea.height / letterSize);
        for(int i = 0; i < currLevel.subWords.Count; i++)
        {
            wyrd = new Wyrd();
            word = currLevel.subWords[i];
            columnWidth = Mathf.Max(columnWidth, word.Length);
            for(int j = 0; j < word.Length;j++)
            {
                c = word[j];
                go = Instantiate<GameObject>(prefabLetter);
                go.transform.SetParent(letterAnchor);
                lett = go.GetComponent<Letter>();
                lett.c = c;
                pos = new Vector3(wordArea.x + left + j * letterSize, wordArea.y, 0);
                pos.y -= (i % numRows) * letterSize;
                lett.posImmediate = pos + Vector3.up * (20 + i % numRows); //буквы начинают перемещаться из-за экрана
                lett.pos = pos;
                lett.timeStart = Time.time + i * 0.1f;
                go.transform.localScale = Vector3.one * letterSize;
                wyrd.Add(lett);
            }
            if (showAllWyrds) wyrd.visible = true;
            wyrd.color = wyrdPalette[word.Length - WordList.WORD_LENGTH_MIN];
            wyrds.Add(wyrd);
            if (i % numRows == numRows - 1)
                left += (columnWidth + 0.5f) * letterSize;
        }

        //поместить на экран большие плитки с буквам
        // инициализировать список больших букв
        bigLetters = new List<Letter>();
        bigLettersActive = new List<Letter>();

        for(int i =0;i<currLevel.word.Length;i++)
        {
            c = currLevel.word[i];
            go = Instantiate<GameObject>(prefabLetter);
            go.transform.SetParent(bigLetterAnchor);
            lett = go.GetComponent<Letter>();
            lett.c = c;
            go.transform.localScale = Vector3.one * bigLetterSize;
            pos = new Vector3(0, -100, 0);
            lett.posImmediate = pos;
            lett.pos = pos;
            lett.timeStart = Time.time + currLevel.subWords.Count * 0.05f;
            lett.easingCuve = Easing.Sin + "-0.18";
            col = bigColorDim;
            lett.color = col;
            lett.visible = true;
            lett.big = true;
            bigLetters.Add(lett);
        }
        bigLetters = ShuffleLetters(bigLetters);
        ArrangeBigLetters();
        mode = GameMode.inLevel;
    }

    List<Letter> ShuffleLetters(List<Letter> letts)
    {
        List<Letter> newL = new List<Letter>();
        int ndx;
        while (letts.Count > 0)
        {
            ndx = Random.Range(0, letts.Count);
            newL.Add(letts[ndx]);
            letts.RemoveAt(ndx);
        }
        return (newL);
    }
    /// <summary>
    /// Отображение больших букв
    /// </summary>
    void ArrangeBigLetters()
    {
        float halfWidth = ((float)bigLetters.Count) / 2f - 0.5f;
        Vector3 pos;
        for(int i = 0; i < bigLetters.Count; i++)
        {
            pos = bigLetterCenter;
            pos.x += (i - halfWidth) * bigLetterSize;
            bigLetters[i].pos = pos;
        }

        halfWidth = ((float)bigLettersActive.Count) / 2f - 0.5f;
        for (int i = 0; i < bigLettersActive.Count; i++)
        {
            pos = bigLetterCenter;
            pos.x += (i - halfWidth) * bigLetterSize;
            pos.y += bigLetterSize * 1.25f;
            bigLettersActive[i].pos = pos;
        }
    }
    // 
    void Update()
    {
        Letter ltr;
        char c;
        switch (mode)
        {
            case GameMode.inLevel:
                //обход всех введенных игроком символов в кадре
                foreach(char cIt in Input.inputString)
                {
                    //если символ можно добавить с тестовое слово - добавить
                    c = System.Char.ToUpperInvariant(cIt);
                    if (upperCase.Contains(c))
                    {
                        ltr = FindNextLetterByChar(c);
                        if(ltr != null)
                        {
                            testWord += c.ToString();
                            bigLettersActive.Add(ltr);
                            bigLetters.Remove(ltr);
                            ltr.color = bigColorSelected;
                            ArrangeBigLetters();
                        }
                    }
                    //при нажатии BackSpace - удаление предыдущего символа
                    if(c == '\b')
                    {
                        if (bigLettersActive.Count == 0) return;
                        if (testWord.Length > 1)
                            testWord = testWord.Substring(0, testWord.Length - 1);
                        else
                            testWord = "";
                        ltr = bigLettersActive[bigLettersActive.Count - 1];
                        bigLettersActive.Remove(ltr);
                        bigLetters.Add(ltr);
                        ltr.color = bigColorDim;
                        ArrangeBigLetters();
                    }
                    //при нажатии Enter - проба слова
                    if (c == '\n' || c == '\r')
                        CheckWord();
                    //при нажатии Space - перемешать буквы слова
                    if (c == ' ')
                    {
                        bigLetters = ShuffleLetters(bigLetters);
                        ArrangeBigLetters();
                    }
                }
                break;
        }
    }
    /// <summary>
    /// найти плитку с символом c в BigLetters
    /// </summary>
    Letter FindNextLetterByChar(char c)
    {
        foreach(Letter ltr in bigLetters)
        {
            if (ltr.c == c)
                return (ltr);
        }
        return (null);
    }
    public void CheckWord()
    {
        string subWord;
        bool foundTestWord = false;
        List<int> containedWords = new List<int>();
        // поиск всех совпадений со словами уровня
        for (int i = 0; i < currLevel.subWords.Count; i++)
        {
            if (wyrds[i].found)
                continue;
            subWord = currLevel.subWords[i];
            if (string.Equals(testWord, subWord))
            {
                HighlightWyrd(i);
                ScoreManager.SCORE(wyrds[i], 1);
                foundTestWord = true;
            }
            else if (testWord.Contains(subWord))
                containedWords.Add(i);
        }
        // отгадано хотя бы 1 слово
        if(foundTestWord)
        {
            int numContained = containedWords.Count;
            int ndx;
            for(int i = 0; i<containedWords.Count; i++)
            {
                ndx = numContained - i - 1;
                HighlightWyrd(containedWords[ndx]);
                ScoreManager.SCORE(wyrds[containedWords[ndx]], i + 2);
            }
        }
        ClearBigLettersActive();
    }
    /// <summary>
    /// Отобразить отгаданное слово
    /// </summary>
    void HighlightWyrd(int ndx)
    {
        wyrds[ndx].found = true;
        wyrds[ndx].color = (wyrds[ndx].color + Color.white) / 2f;
        wyrds[ndx].visible = true;
    }
    void ClearBigLettersActive()
    {
        testWord = "";
        foreach(Letter ltr in bigLettersActive)
        {
            bigLetters.Add(ltr);
            ltr.color = bigColorDim;
        }
        bigLettersActive.Clear();
        ArrangeBigLetters();
    }
}
