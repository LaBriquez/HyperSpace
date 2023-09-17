using UnityEngine;
using UnityEngine.InputSystem;

public class OptionsMenu : MonoBehaviour
{
    [SerializeField] UnityEngine.UI.Dropdown resDropdown, scrmoddd;
    [SerializeField] UnityEngine.UI.Scrollbar barMain, barMusic, barSound;
    [SerializeField] InputActionAsset inputActionAsset;
    [SerializeField] GameObject[] optionsMenu;
    [SerializeField] GameObject writeKeyMask;
    [SerializeField] UnityEngine.UI.Text[] keyTexts;
    [SerializeField] string[] keys;
    [SerializeField] int current, keyIndex, keyD;
    
    public UnityEngine.Audio.AudioMixer audioMixer;

    public void ChangeMenu(int i)
    {
        optionsMenu[current].SetActive(false);
        optionsMenu[i].SetActive(true);
        current = i;
    }
    
    // changement de résolutuon d'écran
    public void ChangeRes(int i)
    {
        string[] values = resDropdown.options[i].text.Split('x');
        Screen.SetResolution(System.Int32.Parse(values[0]), System.Int32.Parse(values[1]), Screen.fullScreenMode);
    }
    
    // fullscreen ou pas
    public void ChangeScreenMode(int i)
    {
        Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height,
            (FullScreenMode) System.Enum.GetValues(typeof(FullScreenMode)).GetValue(i));
    }
    
    // changement des volumes
    public void ChangeMainVolume(float v)
    {
        audioMixer.SetFloat("Main", Mathf.Log10(v + 0.001f) * 20.0f);
    }
    
    public void ChangeMusicVolume(float v)
    {
        audioMixer.SetFloat("Music", Mathf.Log10(v + 0.001f) * 20.0f);
    }
    
    public void ChangeSoundVolume(float v)
    {
        audioMixer.SetFloat("Sounds", Mathf.Log10(v + 0.001f) * 20.0f);
    }
    
    public void ChangeKey(string i)
    {
        string[] s = i.Split('/');
        keyIndex = System.Int32.Parse(s[0]);
        keyD = System.Int32.Parse(s[1]);
        
        writeKeyMask.SetActive(true);
        //StartCoroutine(ChangeKey());
    }

    void Start()
    {
        // ajout des options
        Application.targetFrameRate = (int) Screen.currentResolution.refreshRateRatio.value;
        foreach (Resolution r in Screen.resolutions)
        {
            string res = r.width + "x" + r.height;
            
            if (!resDropdown.options.Exists(data => data.text == res))
                resDropdown.options.Add(new UnityEngine.UI.Dropdown.OptionData(res));
        }
        
        resDropdown.value = resDropdown.options.FindIndex(data => 
            Screen.currentResolution.width + "x" + Screen.currentResolution.height == data.text);

        scrmoddd.value = scrmoddd.options.FindIndex(data => Screen.fullScreenMode.ToString() == data.text);

        if (!System.IO.File.Exists(Application.dataPath + "/Sound.ini"))
        {
            var f = System.IO.File.Create(Application.dataPath + "/Sound.ini");
            f.Write(new [] 
                {(byte) '1', (byte) '\n', 
                    (byte) '1', (byte) '\n', 
                    (byte) '1'});
            f.Close();
        }
        
        // sauvegarde les séléctions des volume
        System.IO.StreamReader fs = new(Application.dataPath + "/Sound.ini");

        barMain.value = System.Single.Parse(fs.ReadLine());
        barMusic.value = System.Single.Parse(fs.ReadLine());
        barSound.value = System.Single.Parse(fs.ReadLine());

        ChangeMainVolume(barMain.value);
        ChangeMusicVolume(barMusic.value);
        ChangeSoundVolume(barSound.value);
        
        fs.Close();

        /*int keysIndex = 0;
        for (int m = 0; m < inputActionAsset.actionMaps[0].actions.Count; m++)
        {
            var a = inputActionAsset.actionMaps[0].actions[m];
            for (int i = 0; i < a.bindings.Count; i++)
            {
                if (a.bindings[i].name != "")
                {
                    if (a.bindings[i].path.Contains("<Keyboard>/") ||
                        a.bindings[i].path.Contains("<Mouse>/"))
                    {
                        string t = a.bindings[i].path.Split('/')[1];

                        keyTexts[keysIndex++].text = t;
                    }
                }
                else
                {
                    string t = a.bindings[i].path.Split('/')[1];

                    keyTexts[keysIndex++].text = t;
                }
            }
        }*/
    }

    // sauvegarde les séléctions
    void OnDestroy()
    {
        float main, music, sounds;
        audioMixer.GetFloat("Main", out main);
        audioMixer.GetFloat("Music", out music);
        audioMixer.GetFloat("Sounds", out sounds);
        
        System.IO.StreamWriter fs = new(Application.dataPath + "/Sound.ini");

        fs.WriteLine(Mathf.Pow(10.0f, main / 20.0f) - 0.001f);
        fs.WriteLine(Mathf.Pow(10.0f, music / 20.0f) - 0.001f);
        fs.WriteLine(Mathf.Pow(10.0f, sounds / 20.0f) - 0.001f);
        
        fs.Close();
    }

    /*IEnumerator ChangeKey()
    {
        while (keyIndex != -1)
        {
            yield return null;
            foreach (KeyCode k in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(k))
                {
                    inputActionAsset.FindAction(keys[keyIndex])
                    .ChangeBinding(keyD)
                    .WithPath(
                        k.ToString().Length == 1? 
                            "<Keyboard>/#(" + k + ")" : 
                        k >= KeyCode.Mouse0 && k <= KeyCode.Mouse6? 
                            "<Mouse>/" + k : 
                            "<Keyboard>/" + k
                    );

                    if (keyIndex > 3)
                        keyIndex += 8;
                    else if (keyIndex == 3)
                        keyIndex += 3 + keyD - 1;
                    else if (keyIndex > 1)
                        keyIndex += 3;
                    else
                        keyIndex += keyD - 1;

                    Debug.Log(keyIndex + keyD);
                    keyTexts[keyIndex].text = k.ToString().Length == 1? 
                        "#(" + k + ")" : k.ToString();
                    
                    keyIndex = -1;
                    keyD = -1;
                    writeKeyMask.SetActive(false);
                    break;
                }
            }
        }
    }*/
}