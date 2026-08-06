using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.InputSystem;
public class Omitir : MonoBehaviour
{
    public InputActionProperty botonA; 
    public PlayableDirector director;
    void Update()
    {
        if (botonA.action.WasPressedThisFrame())
        {
            Skip();
        }
    }
    public void Skip()
    {
        if (director != null && director.state == PlayState.Playing)
        {
            director.time = director.duration; // ir al final
            director.Evaluate(); // aplicar cambios
            director.Stop(); // detener
        }
    }
    
    
}
