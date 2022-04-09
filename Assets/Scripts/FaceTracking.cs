using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARKit;
using UnityEngine.XR.ARSubsystems;
using VRM;

public class FaceTracking : MonoBehaviour
{
    [SerializeField]
    private ARFaceManager faceManager;

    [SerializeField]
    private GameObject avatarPrefab;

    private ARKitFaceSubsystem faceSubsystem;
    private GameObject avatar;
    private Transform neck;
    private Vector3 headOffset;
    private VRMBlendShapeProxy blendShapeProxy;

    private void Start()
    {
        faceSubsystem = (ARKitFaceSubsystem)faceManager.subsystem;

        avatar = Instantiate(avatarPrefab);
        avatar.transform.Rotate(new Vector3(0f, 180f, 0f));

        var animator = avatar.GetComponent<Animator>();
        neck = animator.GetBoneTransform(HumanBodyBones.Neck);

        var head = animator.GetBoneTransform(HumanBodyBones.Head);
        headOffset = new Vector3(0f, head.position.y, 0f);

        blendShapeProxy = avatar.GetComponent<VRMBlendShapeProxy>();
    }

    private void OnEnable()
    {
        faceManager.facesChanged += OnFaceChanged;
    }

    private void OnDisable()
    {
        faceManager.facesChanged -= OnFaceChanged;
    }

    private void OnFaceChanged(ARFacesChangedEventArgs eventArgs)
    {
        if (eventArgs.updated.Count == 0) { return; }

        var arFace = eventArgs.updated[0];
        if (arFace.trackingState == TrackingState.Tracking && ARSession.state > ARSessionState.Ready)
        {
            UpdateAvatarPosition(arFace);
            UpdateBlendShape(arFace);
        }
    }

    private void UpdateAvatarPosition(ARFace arFace)
    {
        avatar.transform.position = arFace.transform.position - headOffset;
        neck.localRotation = Quaternion.Inverse(arFace.transform.rotation);
    }

    private void UpdateBlendShape(ARFace arFace)
    {
        var blendShapesVRM = new Dictionary<BlendShapeKey, float>();
        var blendShapeARKit = faceSubsystem.GetBlendShapeCoefficients(arFace.trackableId, Allocator.Temp);

        foreach (var blendShapeCoefficient in blendShapeARKit)
        {
            switch (blendShapeCoefficient.blendShapeLocation)
            {
                case ARKitBlendShapeLocation.EyeBlinkLeft:
                    blendShapesVRM.Add(BlendShapeKey.CreateFromPreset(BlendShapePreset.Blink_L), blendShapeCoefficient.coefficient);
                    break;
                case ARKitBlendShapeLocation.EyeBlinkRight:
                    blendShapesVRM.Add(BlendShapeKey.CreateFromPreset(BlendShapePreset.Blink_R), blendShapeCoefficient.coefficient);
                    break;
                case ARKitBlendShapeLocation.JawOpen:
                    blendShapesVRM.Add(BlendShapeKey.CreateFromPreset(BlendShapePreset.O), blendShapeCoefficient.coefficient);
                    break;
            }
        }
        blendShapeProxy.SetValues(blendShapesVRM);
    }
}
