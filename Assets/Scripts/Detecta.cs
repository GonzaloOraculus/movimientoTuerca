using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Detecta : MonoBehaviour
{
    public AudioSource audioPlayer;
    public string otroTag;                         //Tag del otro objeto que se desea detectar
    private Transform referenciaOtro;               //El transform que se usa como referencia de posición del otro objeto
    public float toleranciaPos;
    public float toleranciaAng;

    private bool isGrabbed;                        //Cuando es true significa que el objeto ha sido agarrado
    public Transform referenciaEste;              //El transform que se usa como referencia de este objeto
    private bool scanPosition;                     //Cuando es true en el ciclo de update se escanea la posición
    private OwnGrabbable agarrador;               //Scrip 

    private void Start()
    {
        isGrabbed = false;
        scanPosition = false;
        referenciaOtro = null;
    }

    public void GrabBegin()
    {
        isGrabbed = true;
    }

    public void GrabEnd()
    {
        isGrabbed = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((isGrabbed) && (!scanPosition))
        {
            if (other.CompareTag(otroTag))
            {
                scanPosition = true;
                referenciaOtro = other.transform;
                audioPlayer.Play();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if ((isGrabbed) && (scanPosition))
        {
            if (other.CompareTag(otroTag))
            {
                scanPosition = false;
                referenciaOtro = null;
            }
        }
    }
}
