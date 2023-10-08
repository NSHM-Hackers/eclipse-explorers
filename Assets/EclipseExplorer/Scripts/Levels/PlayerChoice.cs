using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
[AddComponentMenu("Eclipse/PlayerChoice", order: 0)]
public class PlayerChoice : MonoBehaviour
{
    [SerializeField] private Text text;
    [SerializeField] private Image bg;
    public Text TextUI => text;

    [SerializeField] private Color normalBg = new Color(0, 0, 0, 0), highlightBg = new Color(1, 1, 0, .5f);
    [SerializeField] private bool isQuizChoice;
    // [HideInInspector] public bool isCorrectChoice;

    [ShowIf("isQuizChoice")] [SerializeField]
    private GameObject markCorrect, markIncorrect;

    public void Awake()
    {
        if (text == null)
        {
            text = GetComponent<Text>();
        }
    }

    public void Highlight()
    {
        bg.color = highlightBg;
    }

    public void Normal()
    {
        bg.color = normalBg;
    }

    public void MarkCorrect()
    {
        markCorrect.SetActive(true);
    }

    public void MarkIncorrect()
    {
        markIncorrect.SetActive(true);
    }
    /*if (isCorrectChoice)
    {
        markCorrect.SetActive(true);
    }
    else
    {
        markIncorrect.SetActive(true);
    }*/
}