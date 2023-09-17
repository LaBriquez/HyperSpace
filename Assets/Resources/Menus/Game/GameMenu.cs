using UnityEngine;

public class GameMenu : MonoBehaviour
{
    [SerializeField] GameObject menu;
    
    //retour ou affichage du menu pause
    public void ReturnToParty()
    {
        bool inPause = Time.timeScale.CompareTo(1.0f) == 0;
        
        menu.SetActive(inPause);
        Time.timeScale = inPause? 0.0f : 1.0f;
    }

    //retour au menu principal
    public void QuitParty()
    {
        Time.timeScale = 1;
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    public void SoundPush()
    {
        
    }

    //pause
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || 
            Input.GetKeyDown(KeyCode.Joystick1Button7) || 
            Input.GetKeyDown(KeyCode.JoystickButton7))
            ReturnToParty();
    }
}