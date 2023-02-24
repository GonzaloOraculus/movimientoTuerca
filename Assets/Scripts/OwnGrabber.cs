/**********************************************************************************
 * Basandose en el OVRGrabbable original permite ajustar tornillos
 * y manipular otros objetos mecanicos usando la mano virtual
 * sin que estos se rompan debido a que la mano no sigue la trayectoria adecuada
 * ********************************************************************************
*/

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

using System.Collections.Generic;
using UnityEngine;
using OVRTouchSample;

/// <summary>
/// Allows grabbing and throwing of objects with the OwnGrabbable component on them.
/// </summary>
[RequireComponent(typeof(Rigidbody))]

public class OwnGrabber : MonoBehaviour
{
    // Grip trigger thresholds for picking up objects, with some hysteresis.
    public float grabBegin = 0.55f;
    public float grabEnd = 0.35f;

    // Demonstrates parenting the held object to the hand's transform when grabbed.
    // When false, the grabbed object is moved every FixedUpdate using MovePosition.
    // Note that MovePosition is required for proper physics simulation. If you set this to true, you can
    // easily observe broken physics simulation by, for example, moving the bottom cube of a stacked
    // tower and noting a complete loss of friction.

    /***********************************************************************/
    //Se elimina para simplificar pues m_parentHeldObject siempre ser� true
    //[SerializeField]
    //protected bool m_parentHeldObject = false;
    /***********************************************************************/

    // If true, this script will move the hand to the transform specified by m_parentTransform, using MovePosition in
    // Update. This allows correct physics behavior, at the cost of some latency. In this usage scenario, you
    // should NOT parent the hand to the hand anchor.
    // (If m_moveHandPosition is false, this script will NOT update the game object's position.
    // The hand gameObject can simply be attached to the hand anchor, which updates position in LateUpdate,
    // gaining us a few ms of reduced latency.)

    /**************************************************************************
     * Se elimina para simplificar pues m_moveHandPosition ser� false siempre
    [SerializeField]
    protected bool m_moveHandPosition = false;
    ***************************************************************************/

    // Child/attached transforms of the grabber, indicating where to snap held objects to (if you snap them).
    // Also used for ranking grab targets in case of multiple candidates.
    [SerializeField]
    protected Transform m_gripTransform = null;
    // Child/attached Colliders to detect candidate grabbable objects.
    [SerializeField]
    protected Collider[] m_grabVolumes = null;

    // Should be OVRInput.Controller.LTouch or OVRInput.Controller.RTouch.
    [SerializeField]
    protected OVRInput.Controller m_controller;

    // You can set this explicitly in the inspector if you're using m_moveHandPosition.
    // Otherwise, you should typically leave this null and simply parent the hand to the hand anchor
    // in your scene, using Unity's inspector.


    [SerializeField]
    protected Transform m_parentTransform;

    [SerializeField]
    protected GameObject m_player;

    protected bool m_grabVolumeEnabled = true;
    protected Vector3 m_lastPos;
    protected Quaternion m_lastRot;
    protected Quaternion m_anchorOffsetRotation;
    protected Vector3 m_anchorOffsetPosition;
    protected float m_prevFlex;
    protected OwnGrabbable m_grabbedObj = null;
    protected Vector3 m_grabbedObjectPosOff;
    protected Quaternion m_grabbedObjectRotOff;
    protected Dictionary<OwnGrabbable, int> m_grabCandidates = new Dictionary<OwnGrabbable, int>();
    protected bool m_operatingWithoutOVRCameraRig = true;

    /*************************************************
     * Variables a�adidas
     * ***********************************************/
    [SerializeField]
    protected OwnHand asociatedHand;

    /***********************************************
     * Finn de variables a�adidas
     * ********************************************/

    /// <summary>
    /// The currently grabbed object.
    /// </summary>
    public OwnGrabbable grabbedObject
    {
        get { return m_grabbedObj; }
    }

    public void ForceRelease(OwnGrabbable grabbable)
    {
        //Este metodo revisa si se tiene un objeto agarrado y fuerza soltarlo
        bool canRelease = (
            (m_grabbedObj != null) &&
            (m_grabbedObj == grabbable)
        );
        if (canRelease)
        {
            GrabEnd();
        }
    }

    protected virtual void Awake()
    {
        //Este script se encuentra en las manos
        //por lo tanto la posici�n local corresponde con la posici�n del anchor de la mano
        m_anchorOffsetPosition = transform.localPosition;
        m_anchorOffsetRotation = transform.localRotation;

        /*************************************************************************
         * Se modifica este metodo pues m_moveHandPosition siempre es falso
        if (!m_moveHandPosition)
        {
            // If we are being used with an OVRCameraRig, let it drive input updates, which may come from Update or FixedUpdate.
            OVRCameraRig rig = transform.GetComponentInParent<OVRCameraRig>();
            if (rig != null)
            {
                rig.UpdatedAnchors += (r) => { OnUpdatedAnchors(); };
                m_operatingWithoutOVRCameraRig = false;
            }
        }
        ****************************************************************************/

        OVRCameraRig rig = transform.GetComponentInParent<OVRCameraRig>();
        if (rig != null)
        {
            rig.UpdatedAnchors += (r) => { OnUpdatedAnchors(); };
            m_operatingWithoutOVRCameraRig = false;
        }
    }

    protected virtual void Start()
    {
        m_lastPos = transform.position;
        m_lastRot = transform.rotation;

        /*********************************************************************************
         * Se modifica pues m_parentTransform siempre ser� null
        
        if (m_parentTransform == null)
        {
            m_parentTransform = gameObject.transform;
        }
        *********************************************************************************/
        m_parentTransform = gameObject.transform;


        // We're going to setup the player collision to ignore the hand collision.
        SetPlayerIgnoreCollision(gameObject, true);
    }

    // Using Update instead of FixedUpdate. Doing this in FixedUpdate causes visible judder even with
    // somewhat high tick rates, because variable numbers of ticks per frame will give hand poses of
    // varying recency. We want a single hand pose sampled at the same time each frame.
    // Note that this can lead to its own side effects. For example, if m_parentHeldObject is false, the
    // grabbed objects will be moved with MovePosition. If this is called in Update while the physics
    // tick rate is dramatically different from the application frame rate, other objects touched by
    // the held object will see an incorrect velocity (because the move will occur over the time of the
    // physics tick, not the render tick), and will respond to the incorrect velocity with potentially
    // visible artifacts.
    virtual public void Update()
    {
        if (m_operatingWithoutOVRCameraRig)
        {
            OnUpdatedAnchors();
        }
    }

    // Hands follow the touch anchors by calling MovePosition each frame to reach the anchor.
    // This is done instead of parenting to achieve workable physics. If you don't require physics on
    // your hands or held objects, you may wish to switch to parenting.


    void OnUpdatedAnchors()
    {
        /****************************************************************************
         *Se elimina toda esta parte ya que destPos y destRot no se utilizan
         *m_moveHandPosition siempre ser� false
         *m_parentHeldObject siempre ser� true
         *
         *Finalmente cada setup se actualiza la posici�n de m_lstPos y m_lastRot y se verifica si se solt� el objeto
         *
         *
        Vector3 destPos = m_parentTransform.TransformPoint(m_anchorOffsetPosition);
        Quaternion destRot = m_parentTransform.rotation * m_anchorOffsetRotation;


        if (m_moveHandPosition)
        {
            GetComponent<Rigidbody>().MovePosition(destPos);
            GetComponent<Rigidbody>().MoveRotation(destRot);
        }

        if (!m_parentHeldObject)
        {
            MoveGrabbedObject(destPos, destRot);
        }
        */

        m_lastPos = transform.position;
        m_lastRot = transform.rotation;

        float prevFlex = m_prevFlex;
        // Update values from inputs
        m_prevFlex = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, m_controller);

        CheckForGrabOrRelease(prevFlex);
    }

    void OnDestroy()
    {
        if (m_grabbedObj != null)
        {
            GrabEnd();
        }
    }

    void OnTriggerEnter(Collider otherCollider)
    {
        // Get the grab trigger
        OwnGrabbable grabbable = otherCollider.GetComponent<OwnGrabbable>() ?? otherCollider.GetComponentInParent<OwnGrabbable>();
        if (grabbable == null) return;

        // Add the grabbable
        int refCount = 0;
        m_grabCandidates.TryGetValue(grabbable, out refCount);
        m_grabCandidates[grabbable] = refCount + 1;
    }

    void OnTriggerExit(Collider otherCollider)
    {
        OwnGrabbable grabbable = otherCollider.GetComponent<OwnGrabbable>() ?? otherCollider.GetComponentInParent<OwnGrabbable>();
        if (grabbable == null) return;

        // Remove the grabbable
        int refCount = 0;
        bool found = m_grabCandidates.TryGetValue(grabbable, out refCount);
        if (!found)
        {
            return;
        }

        if (refCount > 1)
        {
            m_grabCandidates[grabbable] = refCount - 1;
        }
        else
        {
            m_grabCandidates.Remove(grabbable);
        }
    }

    protected void CheckForGrabOrRelease(float prevFlex)
    {
        if ((m_prevFlex >= grabBegin) && (prevFlex < grabBegin))
        {
            GrabBegin();
        }
        else if ((m_prevFlex <= grabEnd) && (prevFlex > grabEnd))
        {
            GrabEnd();
        }
    }

    protected virtual void GrabBegin()
    {
        float closestMagSq = float.MaxValue;
        OwnGrabbable closestGrabbable = null;
        Collider closestGrabbableCollider = null;

        // Iterate grab candidates and find the closest grabbable candidate
        foreach (OwnGrabbable grabbable in m_grabCandidates.Keys)
        {
            /****************************************************************************
             * Este if nunca se ejecutar� pues allowOffhandGrab siempre ser� true
             * *************************************************************************
            bool canGrab = !(grabbable.isGrabbed && !grabbable.allowOffhandGrab);
            if (!canGrab)
            {
                continue;
            }
            ******************************************************************************/

            /**********************************************************
             * Solo se tendr� un grabpoint por grabbable, no es necesario el for
             * ********************************************************

            for (int j = 0; j < grabbable.grabPoints.Length; ++j)
            {
                Collider grabbableCollider = grabbable.grabPoints[j];   //grabPoints es un arreglo de colliders. Por lo menos tiene uno, el collider del grababble
                // Store the closest grabbable
                Vector3 closestPointOnBounds = grabbableCollider.ClosestPointOnBounds(m_gripTransform.position);
                float grabbableMagSq = (m_gripTransform.position - closestPointOnBounds).sqrMagnitude;
                if (grabbableMagSq < closestMagSq)
                {
                    closestMagSq = grabbableMagSq;
                    closestGrabbable = grabbable;
                    closestGrabbableCollider = grabbableCollider;
                }
            }
            ***************************************************/

            //Modificado pues no es necesario el bucle for


            Collider grabbableCollider = grabbable.getGrabPoint;   //En lugar de grabPoints se usa getGrabPoint
                                                                   // Store the closest grabbable
            Vector3 closestPointOnBounds = grabbableCollider.ClosestPointOnBounds(m_gripTransform.position);
            float grabbableMagSq = (m_gripTransform.position - closestPointOnBounds).sqrMagnitude;
            if (grabbableMagSq < closestMagSq)
            {
                closestMagSq = grabbableMagSq;
                closestGrabbable = grabbable;
                closestGrabbableCollider = grabbableCollider;
            }
        }
        

        // Disable grab volumes to prevent overlaps
        GrabVolumeEnable(false);

        if (closestGrabbable != null)
        {
            if (closestGrabbable.isGrabbed)
            {
                closestGrabbable.grabbedBy.OffhandGrabbed(closestGrabbable);
            }

            m_grabbedObj = closestGrabbable;   //Es el script OwnGrabbable del grabbable m�s cercano
            m_grabbedObj.GrabBegin(this, closestGrabbableCollider); //Se indica al grabbable quien y por donde lo est�n agarrando

            m_lastPos = transform.position;    //Vector3
            m_lastRot = transform.rotation;    //Quaternion

            /***********************************************************
             * Simplificado pues no se usa offsets
             * ********************************************************
            // Set up offsets for grabbed object desired position relative to hand.
            if (m_grabbedObj.snapPosition)
            {
                m_grabbedObjectPosOff = m_gripTransform.localPosition;
                if (m_grabbedObj.snapOffset)
                {
                    Vector3 snapOffset = m_grabbedObj.snapOffset.position;
                    if (m_controller == OVRInput.Controller.LTouch) snapOffset.x = -snapOffset.x;
                    m_grabbedObjectPosOff += snapOffset;
                }
            }
            else
            {
                Vector3 relPos = m_grabbedObj.transform.position - transform.position;
                relPos = Quaternion.Inverse(transform.rotation) * relPos;
                m_grabbedObjectPosOff = relPos;
            }
            ******************************************************************/

            Vector3 relPos = m_grabbedObj.transform.position - transform.position;
            relPos = Quaternion.Inverse(transform.rotation) * relPos;
            m_grabbedObjectPosOff = relPos;


            /***********************************************************
             * Simplificado pues no se usa offset ni snap
             * *********************************************************
            if (m_grabbedObj.snapOrientation)
            {
                m_grabbedObjectRotOff = m_gripTransform.localRotation;
                if (m_grabbedObj.snapOffset)
                {
                    m_grabbedObjectRotOff = m_grabbedObj.snapOffset.rotation * m_grabbedObjectRotOff;
                }
            }
            else
            {
                Quaternion relOri = Quaternion.Inverse(transform.rotation) * m_grabbedObj.transform.rotation;
                m_grabbedObjectRotOff = relOri;
            }
            *********************************************************/
            Quaternion relOri = Quaternion.Inverse(transform.rotation) * m_grabbedObj.transform.rotation;
            m_grabbedObjectRotOff = relOri;

            /*******************************************************************************
             * Se mueve esta sentencia para juntarla con la otra que necesita saber si el objeto est� encajado
            // NOTE: force teleport on grab, to avoid high-speed travel to dest which hits a lot of other objects at high
            // speed and sends them flying. The grabbed object may still teleport inside of other objects, but fixing that
            // is beyond the scope of this demo.
            MoveGrabbedObject(m_lastPos, m_lastRot, true);
            *****************************************************************************/

            // NOTE: This is to get around having to setup collision layers, but in your own project you might
            // choose to remove this line in favor of your own collision layer setup.
            SetPlayerIgnoreCollision(m_grabbedObj.gameObject, true);

            /***************************************************************************
             * Se simplifica pues parentHeldObject siempre es true
             * 
            if (m_parentHeldObject)
            {
                m_grabbedObj.transform.parent = transform;
            }
            *****************************************************************************/
            if (!m_grabbedObj.isLocked)
            {
                //Sentencia movida de arriba
                MoveGrabbedObject(m_lastPos, m_lastRot, true);
                m_grabbedObj.transform.parent = transform;
            }
        }
    }

    protected virtual void MoveGrabbedObject(Vector3 pos, Quaternion rot, bool forceTeleport = false)
    {
        if (m_grabbedObj == null)
        {
            return;
        }

        Rigidbody grabbedRigidbody = m_grabbedObj.grabbedRigidbody;
        Vector3 grabbablePosition = pos + rot * m_grabbedObjectPosOff;
        Quaternion grabbableRotation = rot * m_grabbedObjectRotOff;

        if (forceTeleport)
        {
            grabbedRigidbody.transform.position = grabbablePosition;
            grabbedRigidbody.transform.rotation = grabbableRotation;
        }
        else
        {
            grabbedRigidbody.MovePosition(grabbablePosition);
            grabbedRigidbody.MoveRotation(grabbableRotation);
        }
    }

    protected void GrabEnd()
    {
        if (m_grabbedObj != null)
        {
            OVRPose localPose = new OVRPose { position = OVRInput.GetLocalControllerPosition(m_controller), orientation = OVRInput.GetLocalControllerRotation(m_controller) };
            OVRPose offsetPose = new OVRPose { position = m_anchorOffsetPosition, orientation = m_anchorOffsetRotation };
            localPose = localPose * offsetPose;

            OVRPose trackingSpace = transform.ToOVRPose() * localPose.Inverse();
            Vector3 linearVelocity = trackingSpace.orientation * OVRInput.GetLocalControllerVelocity(m_controller);
            Vector3 angularVelocity = trackingSpace.orientation * OVRInput.GetLocalControllerAngularVelocity(m_controller);

            GrabbableRelease(linearVelocity, angularVelocity);
        }

        // Re-enable grab volumes to allow overlap events
        GrabVolumeEnable(true);
    }

    protected void GrabbableRelease(Vector3 linearVelocity, Vector3 angularVelocity)
    {
        m_grabbedObj.GrabEnd(linearVelocity, angularVelocity);

        /***********************************************************
        Se simplifica pues parentHeldObject siempre es true
        *************************************************************
        if (m_parentHeldObject) m_grabbedObj.transform.parent = null;
        *************************************************************/

        m_grabbedObj.transform.parent = null;

        m_grabbedObj = null;
    }

    protected virtual void GrabVolumeEnable(bool enabled)
    {
        if (m_grabVolumeEnabled == enabled)
        {
            return;
        }

        m_grabVolumeEnabled = enabled;
        for (int i = 0; i < m_grabVolumes.Length; ++i)
        {
            Collider grabVolume = m_grabVolumes[i];
            grabVolume.enabled = m_grabVolumeEnabled;
        }

        if (!m_grabVolumeEnabled)
        {
            m_grabCandidates.Clear();
        }
    }

    protected virtual void OffhandGrabbed(OwnGrabbable grabbable)
    {
        if (m_grabbedObj == grabbable)
        {
            GrabbableRelease(Vector3.zero, Vector3.zero);
        }
    }

    protected void SetPlayerIgnoreCollision(GameObject grabbable, bool ignore)
    {
        if (m_player != null)
        {
            Collider[] playerColliders = m_player.GetComponentsInChildren<Collider>();
            foreach (Collider pc in playerColliders)
            {
                Collider[] colliders = grabbable.GetComponentsInChildren<Collider>();
                foreach (Collider c in colliders)
                {
                    if (!c.isTrigger && !pc.isTrigger)
                        Physics.IgnoreCollision(c, pc, ignore);
                }
            }
        }
    }

    /*************************************************
     * Metodos a�adidos
     * **********************************************/

    public void ForceOpenHand()
    {
        asociatedHand.ForceHandOpen();
    }
}
