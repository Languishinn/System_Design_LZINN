using UnityEngine;

public class MenuClicked : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public MenuController menuController;
    public GameObject TargetInterface;
    public void ActiveInterface()
        {
            if (menuController != null)
            {
                menuController.ShowTargetMenu(TargetInterface);
                Debug.Log( TargetInterface+ "active");
            }
        }
}
