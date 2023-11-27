using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorMessenger : MonoBehaviour
{
    public void TransmitMessage(string message)
    {
        GameManager.Instance.OnRecieveAnimationEvent(message);
    }
}
