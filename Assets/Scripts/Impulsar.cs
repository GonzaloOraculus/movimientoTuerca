using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Impulsar : MonoBehaviour
{
    public float magnitude;
    public Rigidbody thisRigid;
    public OVRInput.Controller ManoDerecha;
    public OVRInput.Controller ManoIzquiera;
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.CompareTag("Derecha"))
        {
            float velocidad = OVRInput.GetLocalControllerVelocity(ManoDerecha).magnitude;
            thisRigid.AddForce(velocidad * magnitude * collision.contacts[0].normal, ForceMode.Impulse);
        }
        else
        {
            if (collision.transform.CompareTag("Izquierda"))
            {
                float velocidad = OVRInput.GetLocalControllerVelocity(ManoIzquiera).magnitude;
                thisRigid.AddForce(velocidad * magnitude * collision.contacts[0].normal, ForceMode.Impulse);
            }
        }
            
            
    }
}
