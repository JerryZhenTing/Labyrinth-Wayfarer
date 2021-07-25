using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
public class PlayerInput : MonoBehaviour
{
    public UnityAction<Vector2Int> onInput;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
            onInput?.Invoke(new Vector2Int(-1, 0));
        if (Input.GetKeyDown(KeyCode.D))
            onInput?.Invoke(new Vector2Int(1, 0));


        if (Input.GetKeyDown(KeyCode.W))
            onInput?.Invoke(new Vector2Int(0,1));

        if (Input.GetKeyDown(KeyCode.S))
            onInput?.Invoke(new Vector2Int(0,-1));
    }
}
