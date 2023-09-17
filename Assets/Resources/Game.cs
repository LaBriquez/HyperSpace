using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Game : MonoBehaviour
{
    static Game game;
    public static Game mainGame => game;
    public static bool InGame => game != null;

    [SerializeField] float dtS, maxdts, timerParty;
    [SerializeField] int maxEnn;
    [SerializeField] Camera camera;

    [SerializeField] Text textDest, textPositionParty, textPlacement, textSpeed, timerBeginText, textTimer;
    [SerializeField] Text textFinalTimer;
    [SerializeField] GameObject EndGamePanel;
    [SerializeField] RectTransform recCirNext, recCirPath;
    
    [SerializeField] Image slider, bonusImage, recCirNextImage, bonusTimerImage;
    [SerializeField] Sprite[] spritesBonus;

    [SerializeField] PlayerInput playerInput;
    
    // démarre la partie
    void Awake()
    {
        game = this;
        ShipSpawningSystem.Activate();
        timerParty = -3;
    }

    // update du timer
    void FixedUpdate()
    {
        if (!mainGame) return;
        timerParty += Time.deltaTime;
        textTimer.text = timerBeginText.text != ""? "" : (int)timerParty / 60 + ":" + (int)timerParty % 60;
    }

    // affichage des stats et timer
    public void SetTimerBegin(float t)
    {
        timerBeginText.text = t > 0? t.ToString("0") : "";
    }
    
    public void SetMaxEnnemies(int nbr)
    {
        maxEnn = nbr + 1;
    }

    public void SetBonMaltimer(float t)
    {
        bonusTimerImage.color = t > 0? Color.yellow : Color.clear;
    }

    public void SetBonus(int bonus, float timer)
    {
        if (timer > 0)
        {
            slider.fillAmount = timer;
            return;
        }

        bonusImage.color = Color.white;
        bonusImage.sprite = spritesBonus[bonus];
    }
    
    public void ResetBonus()
    {
        slider.fillAmount = 1;
        
        bonusImage.color = Color.clear;
        bonusImage.sprite = null;
    }

    public void GetPositionInGame(int nbr)
    {
        textPositionParty.text = nbr + "/" + maxEnn;
    }
    
    public void SetSpeed(float v)
    {
        dtS -= Time.deltaTime;
        if (dtS > 0) return;
        dtS = maxdts;

        textSpeed.text = (v / Time.deltaTime * 3600.0f / 1000.0f).ToString("0") + " km/h";
    }
    
    // fonction de fin de partie
    public void EndGame(int nbr)
    {
        game = null;
        textPlacement.text = nbr + "/" + maxEnn;
        textFinalTimer.text = (int)timerParty / 60 + ":" + (int)timerParty % 60;
        EndGamePanel.SetActive(true);
    }

    //les actions du joueur
    public bool Accelerate()
    {
        return playerInput.actions["Accelerate"].IsPressed();
    }
    
    public bool ActiveBonus()
    {
        return playerInput.actions["Bonus"].WasPerformedThisFrame();
    }
    
    public bool ChangeView()
    {
        return playerInput.actions["View"].WasPerformedThisFrame();
    }
    
    public Vector2 Move()
    {
        return playerInput.actions["Move"].ReadValue<Vector2>();
    }
    
    public float RotateShip()
    {
        return -playerInput.actions["Rotate"].ReadValue<float>();
    }

    // les 2 points qui permettent de savoir ou est le prochain checkpoint
    public void setDestination(Vector3 dest, bool isEnd)
    {
        if (isEnd) recCirNextImage.color = Color.red;
        Vector3 direction = dest - camera.transform.position;
        textDest.text = direction.magnitude + " m";
        if (Vector3.Dot(direction, camera.transform.forward) < 0)
            recCirNext.transform.position = new(Screen.currentResolution.width, 0, -100);
        else
            recCirNext.transform.position = camera.WorldToScreenPoint(dest);
    }
    
    public void setNextDestination(Vector3 dest)
    {
        Vector3 direction = dest - camera.transform.position;
        if (Vector3.Dot(direction, camera.transform.forward) < 0)
            recCirPath.transform.position = new(Screen.currentResolution.width, 0, -100);
        else
            recCirPath.transform.position = camera.WorldToScreenPoint(dest);
    }
    
    // set la position et rotation de la camera
    public void CameraPosition(Vector3 camPos, Quaternion camRot, float l)
    {
        camera.transform.position = Vector3.Lerp(camera.transform.position, camPos, l * Time.deltaTime);
        camera.transform.rotation = Quaternion.Lerp(camera.transform.rotation, camRot, l * Time.deltaTime);
    }
}