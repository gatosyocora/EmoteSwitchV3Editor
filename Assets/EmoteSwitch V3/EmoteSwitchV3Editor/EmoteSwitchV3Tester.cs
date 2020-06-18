using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRCSDK2;

public class EmoteSwitchV3Tester : MonoBehaviour
{
    [NonSerialized]
    public Animator animator;

    private VRC_AvatarDescriptor avatar;

    public void Start()
    {
        avatar = transform.parent.GetComponent<VRC_AvatarDescriptor>();
        animator = transform.parent.GetComponent<Animator>();

        animator.runtimeAnimatorController = avatar.CustomStandingAnims;
    }
}
