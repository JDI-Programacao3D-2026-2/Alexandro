using UnityEngine;

public class HeroClassManager : MonoBehaviour
{
    public enum HeroClass
    {
        None = -1,
        Soldier,
        Berserker
    }

    [Header("Botão de seleção de classe:")]
    public static HeroClassManager Instance;
    public HeroClass SelectedClass;
    public GameObject StartButton;
    //public GameObject CharacterFrame;
    //public GameObject CharacterData;
    
    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SelectedClass = HeroClass.None;

        StartButton.SetActive(false);
        //CharacterFrame.SetActive(false);
        //CharacterData.SetActive(false);
    }
    
    public void SelectSoldier()
    {
        if (SelectedClass == HeroClass.Soldier)
        {
            SelectedClass = HeroClass.None;
            StartButton.SetActive(false);
            //CharacterFrame.SetActive(false);
            //CharacterData.SetActive(false);
            Debug.Log("Classe deselecionada: Soldier");
            return;
        }
        SelectedClass = HeroClass.Soldier;
        ClassWasSelected();
    }

    public void SelectBerserker()
    {
        if (SelectedClass == HeroClass.Berserker)
        {
            SelectedClass = HeroClass.None;
            StartButton.SetActive(false);
            //CharacterFrame.SetActive(false);
            //CharacterData.SetActive(false);
            Debug.Log("Classe deselecionada: Berserker");
            return;
        }
        SelectedClass = HeroClass.Berserker;
        ClassWasSelected();
    }

    public void ClassWasSelected()
    {
        if (SelectedClass != HeroClass.None)
        {
            StartButton.SetActive(true);
            //CharacterFrame.SetActive(true);
            //CharacterData.SetActive(true);
            Debug.Log("Classe selecionada: " + SelectedClass.ToString());
        }
    }

}
