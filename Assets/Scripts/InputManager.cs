using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    //[Header("References")]
    //public PlayerMovement playerMovement; // Drag Player here
    //public TorchlightManager torchManager; // Drag Player here

    //// This handles the "Right Stick" to move the torch/aim
    //public void OnLook(InputValue value)
    //{
    //    Vector2 lookInput = value.Get<Vector2>();
    //    // We pass the stick data to the Torchlight script
    //    torchManager.SetGamepadLookInput(lookInput);
    //}

    //// This handles the "Left Stick" for WASD movement
    //public void OnMove(InputValue value)
    //{
    //    Vector2 moveInput = value.Get<Vector2>();
    //    playerMovement.SetGamepadMoveInput(moveInput);
    //}

    //// This handles the Triggers (Sprint/Sneak)
    //public void OnSprint(InputValue value) => playerMovement.SetGamepadSprint(value.Get<float>() > 0.5f);
    //public void OnSneak(InputValue value) => playerMovement.SetGamepadSneak(value.Get<float>() > 0.5f);

    //// This handles the RB Button for the Torch
    //public void OnTorchToggle(InputValue value)
    //{
    //    if (value.isPressed)
    //    {
    //        torchManager.ToggleTorchFromGamepad();
    //    }
    //}
}
