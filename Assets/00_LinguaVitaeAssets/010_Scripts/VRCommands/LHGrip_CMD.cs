﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class LHGrip_CMD : VRCommand
{
    GameManager gm;
    bool pressed = false;
    private void Start() {
        gm = GetComponent<GameManager>();
    }
    public override void RunCommand(TestResult tr, Bone val_bone = default, bool val_bool = false, Eyes val_eyes = default, float val_float = 0, Hand val_hand = default, InputTrackingState val_input_tracking_state = InputTrackingState.None, Quaternion val_quaternion = default, Vector2 val_vector2 = default, Vector3 val_vector3 = default)
    {
        
        if (tr == TestResult.TRUE && !pressed)
        {
            Debug.Log("VR Input 1 detected");
            gm.LH_Grip = true;
            pressed = true;
        }
        else
        {
            gm.LH_Grip = false;
        }
        if(tr == TestResult.FALSE)
        {
            pressed = false;
        }
    }

}
