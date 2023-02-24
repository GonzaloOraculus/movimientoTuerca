/*********************************************************************************
 * Basado en el OVRGrabbable permite interaccion manual con herramientas que 
 * requieren corrección de la posición
 * ******************************************************************************/

/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using UnityEngine;

/// <summary>
/// An object that can be grabbed and thrown by OwnGrabber.
/// </summary>
public class OwnGrabbable : MonoBehaviour
{
    /*************************************************************
     * Simplificamos pues allowOffhandGrab siempre será true
     * ***********************************************************
    [SerializeField]
    protected bool m_allowOffhandGrab = true;
    **************************************************************/

    /*************************************************************
     * No se utilizará snap
     * ***********************************************************
    [SerializeField]
    protected bool m_snapPosition = false;
    [SerializeField]
    protected bool m_snapOrientation = false;
    [SerializeField]
    protected Transform m_snapOffset;
    [SerializeField]
    protected Collider[] m_grabPoints = null;
    *************************************************************/

    /*************************************************************
     * En lugar de grabPoints se utilizará un solo collider
     * como grabbPoint unico
     * ***********************************************************/

    protected Collider m_grabPoint;

    protected bool m_grabbedKinematic = false;
    protected Collider m_grabbedCollider = null;
    protected OwnGrabber m_grabbedBy = null;

    /************************************************************
     * Variables añadidas
     * **********************************************************/

    protected bool scanPosition;                   //Cuando true en cada ciclo update verifica si el grabbable se encuentra en posición.
                                                   //Para que sea true debe haber cercanía con el target
    public string tagBuscado;
    protected Transform transformTuerca;
    public Transform PosicionReferencia;
    public AudioSource audioPlayer;
    public float toleranciaPos;
    public float toleranciaRot;
    protected bool lockPosition;                   //Si es true significa que el objeto ha encajado en el target
    public float touchDuration;
    private float touchTime;
    private bool isVibratingRigth;
    private bool isVibratingLeft;
    public Transform handTracker;
    private bool trackHand;
    public float maxHandDist;
    private Transform handAnchor;
    private Vector3 handOriginalRotation;
    public float avancePorGrado;
    public float posAdentro;
    public float posAfuera;


    /************************************************************
     * Fin variables añadidas
     * **********************************************************/
    /// <summary>
    /// If true, the object can currently be grabbed.
    /// </summary>
    
    /********************************************************************
     * Este metodo no se utiliza pues allowOffhandGrab siempre será true
     * ******************************************************************
    public bool allowOffhandGrab
    {
        get { return m_allowOffhandGrab; }
    }
    ********************************************************************/

    ///<summary>
    ///si True el objeto se encuentra encajado en el tarjet
    /// </summary>
    public bool isLocked
    {
        get { return lockPosition; }
    }


    /// <summary>
    /// If true, the object is currently grabbed.
    /// </summary>
    public bool isGrabbed
    {
        get { return m_grabbedBy != null; }
    }

    /// <summary>
    /// If true, the object's position will snap to match snapOffset when grabbed.
    /// </summary>
    /// 
    /*********************************************************************
     * No se usa snap
     * *******************************************************************
    public bool snapPosition
    {
        get { return m_snapPosition; }
    }
    **********************************************************************/

    /// <summary>
    /// If true, the object's orientation will snap to match snapOffset when grabbed.
    /// </summary>
    /// 
    /*********************************************************************
     * No se usa snap
     * *******************************************************************
    public bool snapOrientation
    {
        get { return m_snapOrientation; }
    }
    **********************************************************************
    /// <summary>
    /// An offset relative to the OwnGrabber where this object can snap when grabbed.
    /// </summary>
    public Transform snapOffset
    {
        get { return m_snapOffset; }
    }
    **********************************************************************/
    /// <summary>
    /// Returns the OwnGrabber currently grabbing this object.
    /// </summary>
    public OwnGrabber grabbedBy
    {
        get { return m_grabbedBy; }
    }

    /// <summary>
    /// The transform at which this object was grabbed.
    /// </summary>
    public Transform grabbedTransform
    {
        get { return m_grabbedCollider.transform; }
    }

    /// <summary>
    /// The Rigidbody of the collider that was used to grab this object.
    /// </summary>
    public Rigidbody grabbedRigidbody
    {
        get { return m_grabbedCollider.attachedRigidbody; }
    }

    /// <summary>
    /// The contact point(s) where the object was grabbed.
    /// </summary>
    /// 
    /***************************************************************
     * No se usan grabPoints
     * *************************************************************
    public Collider[] grabPoints
    {
        get { return m_grabPoints; }
    }
    ***************************************************************/

    /*********************************************
     * En lugar de grabPoints utilizamos getGrabPoint
     * ************************************************/

    public Collider getGrabPoint
    {
        get { return m_grabPoint; }
    }


    /// <summary>
    /// Notifies the object that it has been grabbed.
    /// </summary>
    virtual public void GrabBegin(OwnGrabber hand, Collider grabPoint)
    {
        m_grabbedBy = hand;
        m_grabbedCollider = grabPoint;
        gameObject.GetComponent<Rigidbody>().isKinematic = true;

        if (isLocked)
        {
            handAnchor = m_grabbedBy.transform.parent.transform;
            handTracker.transform.parent = handAnchor;
            handOriginalRotation = m_grabbedBy.transform.localEulerAngles;
            m_grabbedBy.transform.parent = transform;
            transform.parent = transformTuerca.parent;     //acacaca
            trackHand = true;
            if (m_grabbedBy.CompareTag("Derecha"))
            {
                OVRInput.SetControllerVibration(1, 1, OVRInput.Controller.RTouch);
                touchTime = 0;
                isVibratingRigth = true;
            }
            else
            {
                if (m_grabbedBy.CompareTag("Izquierda"))
                {
                    OVRInput.SetControllerVibration(1, 1, OVRInput.Controller.LTouch);
                    touchTime = 0;
                    isVibratingLeft = true;
                }
            }
        }
    }

    /// <summary>
    /// Notifies the object that it has been released.
    /// </summary>
    virtual public void GrabEnd(Vector3 linearVelocity, Vector3 angularVelocity)
    {
        if (!lockPosition)
        {
            Rigidbody rb = gameObject.GetComponent<Rigidbody>();
            rb.isKinematic = m_grabbedKinematic;
            rb.velocity = linearVelocity;
            rb.angularVelocity = angularVelocity;
            m_grabbedBy = null;
            m_grabbedCollider = null;
        }
        else
        {
            trackHand = false;
            handTracker.parent = transform;
            handTracker.localEulerAngles = new Vector3(0, 0, 0);
            handTracker.localPosition = new Vector3(0, 0, 0);
            m_grabbedBy.transform.parent = handAnchor;
            m_grabbedBy.transform.localPosition = new Vector3(0, 0, 0);
            m_grabbedBy.transform.localEulerAngles = handOriginalRotation;
        }
    }

    void Awake()
    {
        /********************************************************************
         * Modificado pues se usa un solo collider como grabpoint unico
         * ******************************************************************
         * 
        if (m_grabPoints.Length == 0)
        {
            // Get the collider from the grabbable
            Collider collider = this.GetComponent<Collider>();
            if (collider == null)
            {
                throw new ArgumentException("Grabbables cannot have zero grab points and no collider -- please add a grab point or collider.");
            }

            // Create a default grab point
            m_grabPoints = new Collider[1] { collider };
        }
        ********************************************************************/

        m_grabPoint = this.GetComponent<Collider>();
        if (m_grabPoint == null)
        {
            throw new ArgumentException("Grabbables cannot have zero grab points and no collider -- please add a grab point or collider.");
        }
    }

    protected virtual void Start()
    {
        isVibratingLeft = false;
        isVibratingRigth = false;
        lockPosition = false;
        m_grabbedKinematic = GetComponent<Rigidbody>().isKinematic;
    }

    void OnDestroy()
    {
        if (m_grabbedBy != null)
        {
            // Notify the hand to release destroyed grabbables
            m_grabbedBy.ForceRelease(this);
        }
    }


    /***********************************************************
     * Metodos añadidos
     * *********************************************************/

    private float AdjustAngle(float angleGrados)
    {
        while (angleGrados >= 360) angleGrados -= 360;
        while (angleGrados <= -360) angleGrados += 360;
        if (angleGrados > 180) angleGrados = angleGrados - 360;
        if (angleGrados < -180) angleGrados = 360 + angleGrados;
        return angleGrados;
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((isGrabbed) && (!scanPosition) && (!trackHand))
        {
            if (other.CompareTag(tagBuscado))
            {
                scanPosition = true;
                transformTuerca = other.transform;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if ((isGrabbed) && (scanPosition))
        {
            if (other.CompareTag(tagBuscado))
            {
                scanPosition = false;
                transformTuerca = null;
            }
        }
    }

    private void Update()
    {
        if (trackHand)
        {
            float distance = (handAnchor.position - m_grabbedBy.transform.position).magnitude;
            if (distance > maxHandDist)
            {
                m_grabbedBy.ForceOpenHand();
                m_grabbedBy.ForceRelease(this);
            }
            else
            {
                handTracker.parent = transform;
                float newLocalYaw =  AdjustAngle(handTracker.localEulerAngles.y);
                handTracker.parent = handAnchor.transform;
                transform.Rotate(0, newLocalYaw, 0, Space.Self);
                float deltaPos = avancePorGrado * newLocalYaw;
                if ((transform.localPosition.y - deltaPos > posAdentro) && (transform.localPosition.y -deltaPos < posAfuera))
                {
                    transform.localPosition -= new Vector3(0, deltaPos, 0);
                }
                else
                {
                    if (transform.localPosition.y - deltaPos <= posAdentro)
                    {
                        m_grabbedBy.ForceOpenHand();
                        m_grabbedBy.ForceRelease(this);
                    }
                    else
                    {
                        transform.parent = m_grabbedBy.transform;
                        handTracker.parent = transform;
                        handTracker.localPosition = new Vector3(0, 0, 0);
                        handTracker.eulerAngles = new Vector3(0, 0, 0);
                        m_grabbedBy.transform.parent = handAnchor;
                        m_grabbedBy.transform.localPosition = new Vector3(0, 0, 0);
                        m_grabbedBy.transform.localEulerAngles = handOriginalRotation;
                        trackHand = false;
                        lockPosition = false;
                        transform.parent = m_grabbedBy.transform;
                    }
                }
            }
        }

        if (isVibratingLeft)
        {
            touchTime += Time.deltaTime;
            if (touchTime >= touchDuration)
            {
                OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.LTouch);
                isVibratingLeft = false;
            }
        }

        if (isVibratingRigth)
        {
            touchTime += Time.deltaTime;
            if (touchTime >= touchDuration)
            {
                OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);
                isVibratingRigth = false;
            }
        }

        if (scanPosition)
        {
            float errorPos = (transformTuerca.position - PosicionReferencia.position).magnitude;
            Vector3 direccionOtro = transformTuerca.TransformDirection(new Vector3(0, 10, 0));
            Vector3 direccionPerno = PosicionReferencia.TransformDirection(new Vector3(0, 10, 0));
            float errorRot = Vector3.Angle(direccionOtro, direccionPerno);
            if (errorPos < toleranciaPos)
            {
                
                if (errorRot < toleranciaRot)
                {
                    transform.parent = transformTuerca.parent;
                    transform.localPosition = new Vector3(0, transform.localPosition.y, 0);
                    transform.localEulerAngles = new Vector3(0, transform.localEulerAngles.y, 0);
                    audioPlayer.Play();
                    handAnchor = m_grabbedBy.transform.parent.transform;
                    handTracker.transform.parent = handAnchor;
                    handOriginalRotation = m_grabbedBy.transform.localEulerAngles;
                    m_grabbedBy.transform.parent = transform;
                    scanPosition = false;
                    lockPosition = true;
                    trackHand = true;
                    if (m_grabbedBy.CompareTag("Derecha"))
                    {
                        OVRInput.SetControllerVibration(1, 1, OVRInput.Controller.RTouch);
                        touchTime = 0;
                        isVibratingRigth = true;
                    }
                    else
                    {
                        if (m_grabbedBy.CompareTag("Izquierda"))
                        {
                            OVRInput.SetControllerVibration(1, 1, OVRInput.Controller.LTouch);
                            touchTime = 0;
                            isVibratingLeft = true;
                        }
                    }
                }
            }
        }
    }
}
