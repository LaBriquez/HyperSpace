using UnityEngine;

public class Menu : MonoBehaviour
{
    [SerializeField] int current;
    [SerializeField] GameObject[] menus;
    [SerializeField] GameObject controls;

    // changement des menu (jouer, options, etc...)
    public void ChangeMenu(int i)
    {
        menus[current].SetActive(false);
        menus[i].SetActive(true);
        current = i;
    }

    // affiche les controles
    public void ShowControls()
    {
        controls.SetActive(!controls.activeSelf);
    }
    
    public void Quit()
    {
        Application.Quit();
    }

    public void SoundPush()
    {
        //audiopush.Play();
    }
}