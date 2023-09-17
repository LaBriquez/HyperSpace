using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MapChoose : MonoBehaviour
{
    [SerializeField] int mapIndex, shipIndex, shipCount1, shipCount2;
    [SerializeField] Text textShipCount, textShipCount2;
    
    // change la map
    public void ChangeMap(int i)
    {
        mapIndex = i;
    }
    
    // change le vaisseau selectionner
    public void ChangeShip(int i)
    {
        shipIndex = i;
    }
    
    //1000 vaisseau max chacun
    public void ChangeShipCount1(float i)
    {
        shipCount1 = 1 + (int) (999.0f * i);
        textShipCount.text = shipCount1.ToString();
    }
    
    public void ChangeShipCount2(float i)
    {
        shipCount2 = 1 + (int) (999.0f * i);
        textShipCount2.text = shipCount2.ToString();
    }

    //set les paramètre et change de map
    public void LoadMap()
    {
        GameSetting.shipIndex = shipIndex;
        GameSetting.shipCount1 = shipCount1;
        GameSetting.shipCount2 = shipCount2;
        
        SceneManager.LoadScene(mapIndex);
    }
}