using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class Map : MonoBehaviour
{
    public PrefabClass prefabs;
    public int currentLevel;
    public AnimationCurve moveCurve;
    public float moveTime;
    int[,] grids;

    readonly List<int[,]> allLevels = new List<int[,]>();
    readonly List<int[,]> allPaabox = new List<int[,]>();

    int[,] paraboxPreset;
    int[,] mapState;

    Vector2Int playerPos;
    Vector2Int boxPos;
    Vector2Int playerEndPos;
    Vector2Int boxEndPos;
    Transform playerObj;
    Transform boxObj;
    public Parabox parabox;
    bool _isMoving;

    bool _boxIn;
    bool _PlayerIn;
    private void Awake()
    {

        AddLevels();

        grids = allLevels[currentLevel - 1];

        paraboxPreset = allPaabox[currentLevel - 1];

        GetComponent<PlayerInput>().onInput = HandleInput;

        mapState = new int[grids.GetLength(0), grids.GetLength(1)];

        parabox = new Parabox();

        parabox.inner = paraboxPreset;

        for (int i = 0; i < grids.GetLength(0); i++)
            for (int j = 0; j < grids.GetLength(1); j++)
            {
                mapState[i, j] = grids[i, j];
                if (mapState[i, j] == 2)
                    playerPos = new Vector2Int(i, j);
                if (mapState[i, j] == 3)
                    boxPos = new Vector2Int(i, j);
                if (mapState[i, j] == 4)
                    playerEndPos = new Vector2Int(i, j);
                if (mapState[i, j] == 5)
                    boxEndPos = new Vector2Int(i, j);
                if (mapState[i, j] == 6)
                    parabox.pos = new Vector2Int(i, j);
            }

    }
    void CreateParabox()
    {
        GameObject box = new GameObject();
        box.transform.parent = transform;
        float length = 1 / (float)parabox.inner.GetLength(0);
        parabox.obj = box.transform;
        parabox.obj.position = new Vector2(parabox.pos.x, parabox.pos.y) - new Vector2(grids.GetLength(0) / 2, grids.GetLength(1) / 2);
        for (int i = 0; i < parabox.inner.GetLength(0); i++)
            for (int j = 0; j < parabox.inner.GetLength(1); j++)
            {
                if (parabox.inner[i, j] == 0)
                {
                    var v = Instantiate(prefabs.wallPrefab, parabox.obj);
                    v.GetComponent<SpriteRenderer>().color = Color.yellow;
                    v.transform.localScale = Vector3.one * length;
                    v.transform.localPosition = new Vector2(i, j) * length - Vector2.one * 0.5f + Vector2.one * length / 2;
                }
            }

        for (int i = 0; i < parabox.inner.GetLength(0); i++)
            for (int j = 0; j < parabox.inner.GetLength(1); j++)
            {
                if (parabox.inner[i, j] == 1)
                {
                    if (i == 0)
                        parabox.leftPos = new Vector2Int(i, j);
                    else if (i == parabox.inner.GetLength(0) - 1)
                        parabox.rightPos = new Vector2Int(i, j);
                    else if (j == 0)
                        parabox.downPos = new Vector2Int(i, j);
                    else if (j == parabox.inner.GetLength(1) - 1)
                        parabox.upPos = new Vector2Int(i, j);
                }
            }
    }
    private void Start()
    {
        InitMap();
    }
    void InitMap()
    {
        for (int i = 0; i < grids.GetLength(0); i++)
            for (int j = 0; j < grids.GetLength(1); j++)
            {
                GameObject obj = null;
                switch (grids[i, j])
                {
                    case 0: obj = prefabs.wallPrefab; break;
                    case 1: obj = null; break;
                    case 2: obj = prefabs.playerPrefab; break;
                    case 3: obj = prefabs.boxPrefab; break;
                    case 4: obj = prefabs.playerEndPrefab; break;
                    case 5: obj = prefabs.boxEndPrefab; break;
                    case 6: CreateParabox(); break;
                }
                if (obj == null)
                    continue;
                var v = Instantiate(obj, transform);
                if (obj == prefabs.playerPrefab)
                    playerObj = v.transform;
                if (obj == prefabs.boxPrefab)
                    boxObj = v.transform;
                v.transform.position = new Vector2(i, j) - new Vector2(grids.GetLength(0) / 2, grids.GetLength(1) / 2);
            }
    }
    public void NextLevel()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(currentLevel % 3);
    }
    bool IsWin()
    {
        return playerPos == playerEndPos && boxPos == boxEndPos;
    }
    void HandleInput(Vector2Int move)
    {
        if (_isMoving)
            return;
        Vector2Int newPos = playerPos + move;

        if (IsTransborder(newPos))
            return;
        if (_PlayerIn)
        {
            Vector2Int newInPos = parabox.playerPos + move;
            if (newInPos == parabox.boxPos && _boxIn)
            {
                Vector2Int boxInNewPos = newInPos + move;
                if (parabox.IsTransborder(boxInNewPos) && !IsWalkable(parabox.pos +move))
                    return;
                if (!parabox.IsTransborder(boxInNewPos) && parabox.IsWalkable(boxInNewPos))
                {
                    StartCoroutine(MoveIE(boxObj, parabox.GetWorldPos(boxInNewPos)));
                    StartCoroutine(MoveIE(playerObj, parabox.GetWorldPos(newInPos)));
                    parabox.inner[boxInNewPos.x, boxInNewPos.y] = (int)GridType.BOX;
                    parabox.inner[newInPos.x, newInPos.y] = (int)GridType.PLAYER;
                    parabox.inner[parabox.playerPos.x, parabox.playerPos.y] = (int)GridType.ROAD;

                    parabox.boxPos = boxInNewPos;
                    parabox.playerPos = newInPos;
                }
                else if (parabox.IsTransborder(boxInNewPos))
                {
                    StartCoroutine(MoveIE(boxObj, ToMapCor(parabox.pos + move)));
                    boxPos = parabox.pos + move;
                    StartCoroutine(ScaleIE(boxObj, 1));
                    mapState[boxPos.x, boxPos.y] = (int)GridType.BOX;
                    boxObj.SetParent(transform);
                    _boxIn = false;
                    parabox.inner[parabox.boxPos.x, parabox.boxPos.y] = (int)GridType.PLAYER;
                    StartCoroutine(MoveIE(playerObj, parabox.GetWorldPos(parabox.boxPos)));
                    parabox.inner[parabox.playerPos.x, parabox.playerPos.y] = (int)GridType.ROAD;
                    parabox.playerPos = parabox.boxPos;
                }
            }
            else if (!parabox.IsTransborder(newInPos) && parabox.IsWalkable(newInPos))
            {
                parabox.inner[newInPos.x, newInPos.y] = (int)GridType.PLAYER;
                parabox.inner[parabox.playerPos.x, parabox.playerPos.y] = (int)GridType.ROAD;
                parabox.playerPos = newInPos;
                StartCoroutine(MoveIE(playerObj, parabox.GetWorldPos(parabox.playerPos)));
            }

            else if (parabox.IsTransborder(newInPos))
            {
                _PlayerIn = false;
                CameraFocus.Instance.FocusObj(null);
                StartCoroutine(MoveIE(playerObj, ToMapCor(parabox.pos + move)));
                StartCoroutine(ScaleIE(playerObj, 1));
                playerPos = parabox.pos + move;
                mapState[playerPos.x, playerPos.y] = (int)GridType.PLAYER;
                parabox.inner[parabox.playerPos.x, parabox.playerPos.y] = (int)GridType.ROAD;
                if (boxPos == playerPos)
                {
                    Vector2Int boxNew = boxPos + move;
                    StartCoroutine(MoveIE(boxObj, ToMapCor(boxNew)));
                    boxPos = boxNew;
                    mapState[boxPos.x, boxPos.y] = (int)GridType.BOX;

                }
            }
        }

        else
        {
            if (parabox.pos == newPos)
            {
                Vector2Int paraNext = newPos + move;
                if (IsWalkable(paraNext))
                {
                    mapState[paraNext.x, paraNext.y] = (int)GridType.PARABOX;
                    mapState[newPos.x, newPos.y] = (int)GridType.PLAYER;
                    mapState[playerPos.x, playerPos.y] = (int)GridType.ROAD;

                    parabox.pos = paraNext;
                    playerPos = newPos;
                    StartCoroutine(MoveIE(parabox.obj, ToMapCor(paraNext)));
                    StartCoroutine(MoveIE(playerObj, ToMapCor(newPos)));
                }
                else
                {
                    if (parabox.leftPos != null && move.x > 0)
                    {
                        PlayerEnter(parabox.leftPos.Value);
                    }
                    else if (parabox.rightPos != null && move.x < 0)
                    {
                        PlayerEnter(parabox.rightPos.Value);
                    }
                    else if (parabox.downPos != null && move.y > 0)
                    {
                        PlayerEnter(parabox.downPos.Value);
                    }
                    else if (parabox.upPos != null && move.y < 0)
                    {
                        PlayerEnter(parabox.upPos.Value);
                    }
                    void PlayerEnter(Vector2Int dir)
                    {
                        StartCoroutine(MoveIE(playerObj, parabox.GetWorldPos(dir)));
                        StartCoroutine(ScaleIE(playerObj, 1 / (float)parabox.inner.GetLength(0)));
                        mapState[playerPos.x, playerPos.y] = (int)GridType.ROAD;
                        playerPos = parabox.pos;
                        parabox.playerPos = dir;
                        parabox.inner[parabox.playerPos.x, parabox.playerPos.y] = (int)GridType.PLAYER;
                        CameraFocus.Instance.FocusObj(parabox.obj);
                        if (boxPos == parabox.pos && parabox.boxPos == dir)
                        {
                            Vector2Int boxNew = parabox.boxPos + move;
                            StartCoroutine(MoveIE(boxObj, parabox.GetWorldPos(boxNew)));
                            parabox.inner[boxNew.x, boxNew.y] = (int)GridType.BOX;
                            parabox.boxPos = boxNew;
                        }
                        _PlayerIn = true;
                    }
                }
            }
            else if (boxPos == newPos)
            {
                Vector2Int boxNewPos = boxPos + move;
                if (IsTransborder(boxNewPos))
                    return;
                if (boxNewPos == parabox.pos)
                {
                    Vector2Int paraboxNewPos = boxNewPos + move;
                    if (!IsWalkable(paraboxNewPos))
                    {
                        if (parabox.leftPos != null && move.x > 0)
                        {
                            BoxEnter(parabox.leftPos.Value);
                        }
                        else if (parabox.rightPos != null && move.x < 0)
                        {
                            BoxEnter(parabox.rightPos.Value);
                        }
                        else if (parabox.downPos != null && move.y > 0)
                        {
                            BoxEnter(parabox.downPos.Value);
                        }
                        else if (parabox.upPos != null && move.y < 0)
                        {
                            BoxEnter(parabox.upPos.Value);
                        }

                        void BoxEnter(Vector2Int dir)
                        {
                            _boxIn = true;
                            StartCoroutine(MoveIE(boxObj, parabox.GetWorldPos(dir)));
                            mapState[boxPos.x, boxPos.y] = (int)GridType.PLAYER;
                            mapState[playerPos.x, playerPos.y] = (int)GridType.ROAD;

                            playerPos = boxPos;
                            boxPos = parabox.pos;
                            StartCoroutine(MoveIE(playerObj, ToMapCor(playerPos)));
                            StartCoroutine(ScaleIE(boxObj, 1 / (float)parabox.inner.GetLength(0)));

                            parabox.boxPos = dir;
                            parabox.inner[parabox.boxPos.x, parabox.boxPos.y] = (int)GridType.BOX;
                            boxObj.SetParent(parabox.obj);

                        }
                    }
                    else
                    {
                        mapState[playerPos.x, playerPos.y] = (int)GridType.ROAD;
                        StartCoroutine(MoveIE(parabox.obj, ToMapCor(paraboxNewPos)));
                        parabox.pos = paraboxNewPos;
                        StartCoroutine(MoveIE(boxObj, ToMapCor(boxNewPos)));
                        boxPos = boxNewPos;
                        StartCoroutine(MoveIE(playerObj, ToMapCor(newPos)));
                        playerPos = newPos;
                        mapState[paraboxNewPos.x, paraboxNewPos.y] = (int)GridType.PARABOX;
                        mapState[boxPos.x, boxPos.y] = (int)GridType.BOX;
                        mapState[playerPos.x, playerPos.y] = (int)GridType.PLAYER;
                    }
                }
                else if (IsWalkable(boxNewPos))
                {

                    mapState[playerPos.x, playerPos.y] = (int)GridType.ROAD;
                    boxPos = boxNewPos;
                    StartCoroutine(MoveIE(boxObj, ToMapCor(boxPos)));
                    playerPos = newPos;
                    StartCoroutine(MoveIE(playerObj, ToMapCor(playerPos)));
                    mapState[boxPos.x, boxPos.y] = (int)GridType.BOX;
                    mapState[playerPos.x, playerPos.y] = (int)GridType.PLAYER;

                }
            }
            else if (IsWalkable(newPos))
            {
                mapState[playerPos.x, playerPos.y] = (int)GridType.ROAD;
                mapState[newPos.x, newPos.y] = (int)GridType.PLAYER;
                playerPos = newPos;
                StartCoroutine(MoveIE(playerObj, ToMapCor(playerPos)));
            }
        }

        if (IsWin())
            Win.SetActive(true);

    }
    public GameObject Win;

    void AddLevels()
    {
        allLevels.Add(new int[,] {
     { 0,0,0,0,0,0,0},
     { 0,0,0,0,0,0,0},
     { 0,5,1,1,1,1,0},
     { 0,1,1,3,0,6,0},
     { 0,4,2,1,1,1,0},
     { 0,0,0,0,0,0,0},
     { 0,0,0,0,0,0,0},
    });
        allLevels.Add(new int[,] {
     { 0,0,0,0,0,0,0,0,0},
     { 0,0,0,0,0,0,5,0,0},
     { 0,0,0,0,0,0,1,0,0},
     { 0,1,1,6,1,1,1,0,0},
     { 0,1,3,0,0,0,1,0,0},
     { 0,0,1,0,0,0,1,0,0},
     { 0,1,1,0,4,1,1,0,0},
     { 0,2,1,0,0,0,0,0,0},
     { 0,0,0,0,0,0,0,0,0},
    });
        allLevels.Add(new int[,] {
     { 0,0,0,0,0,0,0,0,0},
     { 0,0,1,1,1,1,1,0,0},
     { 0,0,1,1,6,1,1,0,0},
     { 0,0,1,1,3,1,2,0,0},
     { 0,0,1,1,1,1,1,0,0},
     { 0,0,1,0,0,0,0,0,0},
     { 0,0,1,1,1,4,0,0,0},
     { 0,0,1,1,1,5,0,0,0},
     { 0,0,0,0,0,0,0,0,0},
    });

        allPaabox.Add(new int[,]
    {
        {0,0,0,0,0 },
        {0,1,1,1,0 },
        {1,1,1,1,0 },
        {0,1,1,1,0 },
        {0,0,0,0,0 },
    });

        allPaabox.Add(new int[,]
{
        {0,0,0,1,0 },
        {0,0,0,1,0 },
        {1,1,1,1,0 },
        {0,0,1,1,0 },
        {0,0,0,0,0 },
});
        allPaabox.Add(new int[,]
{
        {0,0,0,0,0 },
        {0,1,1,1,0 },
        {0,1,1,1,1 },
        {0,1,1,1,0 },
        {0,0,0,0,0 },
});
    }
    Vector2 ToMapCor(Vector2 cor)
    {
        return cor - new Vector2(grids.GetLength(0) / 2, grids.GetLength(1) / 2);
    }
    bool IsWalkable(Vector2Int pos)
    {
        return mapState[pos.x, pos.y] == (int)GridType.ROAD ||
                mapState[pos.x, pos.y] == (int)GridType.PLAYEREND ||
                mapState[pos.x, pos.y] == (int)GridType.BOXEND;
    }
    bool IsTransborder(Vector2Int i)
    {
        return i.x < 0 || i.x >= grids.GetLength(0) || i.y < 0 || i.y >= grids.GetLength(1);
    }
    IEnumerator ScaleIE(Transform t, float targetScale)
    {
        float _t = 0;
        float change = targetScale - t.localScale.x;
        float start = t.localScale.x;

        while (_t < moveTime)
        {
            t.localScale = (start + change * moveCurve.Evaluate(_t / moveTime)) * Vector3.one;
            _t += Time.deltaTime;
            yield return 0;
        }
        t.localScale = Vector3.one * targetScale;
    }
    IEnumerator MoveIE(Transform t, Vector3 targetPos)
    {

        _isMoving = true;
        float _t = 0;
        Vector3 move = targetPos - t.position;
        Vector3 start = t.position;

        while (_t < moveTime)
        {
            t.position = start + move * moveCurve.Evaluate(_t / moveTime);
            _t += Time.deltaTime;
            yield return 0;
        }
        t.position = targetPos;
        _isMoving = false;
    }
}


[System.Serializable]
public class PrefabClass
{
    public GameObject wallPrefab;
    public GameObject playerPrefab;
    public GameObject boxPrefab;
    public GameObject playerEndPrefab;
    public GameObject boxEndPrefab;
}
public enum GridType
{
    WALL = 0,
    ROAD,
    PLAYER,
    BOX,
    PLAYEREND,
    BOXEND,
    PARABOX,
}

public class Parabox
{
    public int[,] inner;
    public Vector2Int pos;
    public Transform obj;
    public Vector2Int? upPos = null;
    public Vector2Int? downPos = null;
    public Vector2Int? leftPos = null;
    public Vector2Int? rightPos = null;

    public Vector2Int boxPos;
    public Vector2Int playerPos;
    public bool IsWalkable(Vector2Int pos)
    {
        
        return inner[pos.x, pos.y] == (int)GridType.ROAD ||
                inner[pos.x, pos.y] == (int)GridType.PLAYEREND ||
                inner[pos.x, pos.y] == (int)GridType.BOXEND;
    }
    public bool IsTransborder(Vector2Int i)
    {
        return i.x < 0 || i.x >= inner.GetLength(0) || i.y < 0 || i.y >= inner.GetLength(1);
    }
    public Vector2 GetWorldPos(Vector2 v)
    {
        float length = 1 / (float)inner.GetLength(0);
        return (Vector2)obj.position + v * length - Vector2.one * 0.5f + Vector2.one * length * 0.5f;
    }

}