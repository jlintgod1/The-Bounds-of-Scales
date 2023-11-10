using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ScrollingSnake : MonoBehaviour
{
    public SpriteRenderer _spriteRenderer { get; protected set; }
    public Rigidbody2D _body2D { get; protected set; }
    public float Speed;
    public float EstimatedTimeToRise;
    protected bool IsOffscreen;
    public Vector3 InitialPostion { get; private set; }
    public GameObject[] SnakePanelPrefabs = new GameObject[3];

    private int PanelTier; // 0=None, 1=Jump, 2=Fire, 3=Scale
    private List<GameObject> SpawnedSnakePanels = new List<GameObject>();

    bool WithinScreenBounds(float Position)
    {
        if (Position < -GameManager.CONST_ScreenDimensions.x / 32 || Position > GameManager.CONST_ScreenDimensions.x / 32) return false;
        return true;
    }

    void UpdatePanels()
    {
        if (PanelTier <= 0) return;

        foreach (var item in SpawnedSnakePanels)
        {
            Destroy(item);
        }

        float separation = (_spriteRenderer.size.x - 4) / (PanelTier * 3);

        for (int i = 0; i < PanelTier * 3; i++)
        {
            GameObject panelClone = Instantiate(SnakePanelPrefabs[Random.Range(0,PanelTier)], transform.position - new Vector3(separation * (i + 1) * transform.localScale.x + Random.Range(-4, 4), -0.75f, 0.1f), Quaternion.identity, transform);
            SpawnedSnakePanels.Add(panelClone);
        }
    }

    public bool CanSnakeRise()
    {
        if (_body2D.IsTouchingLayers(LayerMask.GetMask("SnakeRisePause"))) return false;
        return true;
    }

    void OnSnakeRise()
    {
        GameObject Clone = Instantiate(gameObject, transform.position, Quaternion.identity);
        Clone.GetComponent<ScrollingSnake>().IsOffscreen = true;

        if (CanSnakeRise())
        {
            transform.position = new Vector3(transform.localScale.x * 12.125f, transform.position.y + 4, transform.position.z);
            transform.localScale = new Vector3(
                -1 * transform.localScale.x,
                1 * transform.localScale.y,
                1 * transform.localScale.z);

            EstimatedTimeToRise = (_spriteRenderer.size.x - 24) / Speed;
        }
        else
        {
            transform.position = new Vector3(transform.position.x - (_spriteRenderer.size.x + 0.5f) * transform.localScale.x, transform.position.y, transform.position.z);
            EstimatedTimeToRise = (transform.position.x - (_spriteRenderer.size.x - 24)) / Speed;
        }

        UpdatePanels();

        GameManager.Instance.OnSnakeRise();
    }

    // Start is called before the first frame update
    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _body2D = GetComponent<Rigidbody2D>();
        InitialPostion = transform.position;
        EstimatedTimeToRise = Mathf.Abs((transform.position.x - (_spriteRenderer.size.x - 24) * transform.localScale.x) - transform.position.x) / Speed;

        if (!IsOffscreen)
        {
            PanelTier = GameManager.Instance.GetUpgradeCount("JumpPanels")
                + GameManager.Instance.GetUpgradeCount("FirePanels")
                + GameManager.Instance.GetUpgradeCount("ScalePanels");
            UpdatePanels();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOffscreen && WithinScreenBounds(transform.position.x - (_spriteRenderer.size.x - 24) * transform.localScale.x))
        {
            OnSnakeRise();
        }
        else if (IsOffscreen 
            && !WithinScreenBounds(transform.position.x - (_spriteRenderer.size.x + 1) * transform.localScale.x)
            && Mathf.Sign(transform.position.x - (_spriteRenderer.size.x + 1) * transform.localScale.x) == transform.localScale.x)
        {
            Destroy(gameObject);
        }
    }

    private void FixedUpdate()
    {
        _body2D.MovePosition(_body2D.position + Vector2.right * transform.localScale.x * Speed * Time.fixedDeltaTime);
    }
}
