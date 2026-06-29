using UnityEngine;
using UnityEngine.InputSystem;

public class CameraLook : MonoBehaviour
{
    public float lookOffsetY = 2f;
    public float lookSpeed = 2f;
    public float holdDelay = 0.8f;
    public string lookTargetName = "CameraTarget";

    private Transform cameraTarget;
    private float holdTime = 0f;
    private bool isLooking = false;

    void Update()
    {
        if (cameraTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                cameraTarget = player.transform.Find(lookTargetName);
                if (cameraTarget != null)
                    Debug.Log("CameraLook: CameraTarget ENCONTRADO!");
            }
            if (cameraTarget == null) return;
        }

        if (Keyboard.current == null) return;

        bool holdingUp = Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed;
        bool holdingDown = Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed;

        if (holdingUp || holdingDown)
        {
            holdTime += Time.deltaTime;
            if (holdTime >= holdDelay) isLooking = true;
        }
        else
        {
            holdTime = 0f;
            isLooking = false;
        }

        float targetY = 0f;
        if (isLooking && holdingUp)
            targetY = lookOffsetY;
        else if (isLooking && holdingDown)
            targetY = -lookOffsetY;

        Vector3 pos = cameraTarget.localPosition;
        pos.y = Mathf.Lerp(pos.y, targetY, Time.deltaTime * lookSpeed);
        cameraTarget.localPosition = pos;
    }
}
