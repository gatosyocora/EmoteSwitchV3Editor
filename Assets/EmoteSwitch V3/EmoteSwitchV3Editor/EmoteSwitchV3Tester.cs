using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRCSDK2;

// EmoteSwitchV3Tester v1.0
// Copyright (c) 2020 gatosyocora

namespace Gatosyocora.EmoteSwitchV3Editor
{
    public class EmoteSwitchV3Tester : MonoBehaviour
    {
        [NonSerialized]
        public Animator animator;

        [NonSerialized]
        public AnimatorOverrideController controller;

        private VRC_AvatarDescriptor avatar;

        public void Start()
        {
            avatar = transform.parent.GetComponent<VRC_AvatarDescriptor>();
            animator = transform.parent.GetComponent<Animator>();

            animator.runtimeAnimatorController = avatar.CustomStandingAnims;
            controller = avatar.CustomStandingAnims;
        }
    }
}