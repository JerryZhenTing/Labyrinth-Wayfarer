using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFocus : MonoBehaviour
{
    public static CameraFocus Instance { get; private set; }
    public AnimationCurve moveCurve;
    public float time;
    private void Awake()
    {
        Instance = this;
    }

    public void FocusObj(Transform t)
    {
        StartCoroutine(FocusOn(t));
    }

    IEnumerator FocusOn(Transform t)
    {
        float targetSize;
        Vector3 targetPos;
        Vector3 startPos = transform.position;
        float startSize = GetComponent<Camera>().orthographicSize;
        if (t == null)
        {
            targetPos = new Vector3(0, 0, -10);
            targetSize = 5;
        }
        else
        {
            targetPos = new Vector3(t.position.x, t.position.y, -10);
            targetSize = 2;
        }
        Vector3 move = targetPos - transform.position;
        float change = targetSize - GetComponent<Camera>().orthographicSize;
        float _t = 0;
        while(_t <time)
        {
            transform.position = startPos + move * moveCurve.Evaluate(_t / time);
            GetComponent<Camera>().orthographicSize = startSize + change * moveCurve.Evaluate(_t / time);
            _t += Time.deltaTime;
            yield return 0;
        }
        GetComponent<Camera>().orthographicSize = targetSize;
        transform.position = targetPos;
    }
}
