using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class CircleMinigame : MonoBehaviour
{
    public List<SpriteRenderer> playerSprites;
    public List<SpriteRenderer> circles;
    public SpriteRenderer backgroundRenderer;
    public SpriteMask mask;
    public SpriteRenderer selectionCircle;
    public List<Color> potentialColors;
    InputActions inputActions;

    public bool active { get; private set; }
    public int CurrentLevel { get; private set; }
    float AimAngle;
    SpriteRenderer selectedCircle;
    float HitAlpha;
    
    void Awake()
    {
        inputActions = new InputActions();
        inputActions.Enable();
    }
    IEnumerator SpawnAnimation() 
    {
        for (float i = 0; i <= 1; i+=0.01f)
        {
            backgroundRenderer.material.SetFloat("_Alpha", i);
            mask.alphaCutoff = 1 - i;
            yield return new WaitForSecondsRealtime(0.01f);
        }

        active = true;
    }
    IEnumerator DisappearAnimation()
    {
        active = false;
        for (float i = 1; i >= 0; i -= 0.01f)
        {
            backgroundRenderer.material.SetFloat("_Alpha", i);
            mask.alphaCutoff = 1 - i;
            yield return new WaitForSecondsRealtime(0.01f);
        }
        Destroy(gameObject);
    }
    public void OnFinishGame()
    {
        StartCoroutine(DisappearAnimation());
    }
    // Start is called before the first frame update
    void Start()
    {
        IncreaseLevel();
        StartCoroutine(SpawnAnimation());
    }

    // Update is called once per frame
    void Update()
    {
        if (!active) return;
        HitAlpha = Mathf.Max(HitAlpha - Time.unscaledDeltaTime * 2.5f, 0);

        transform.Rotate(new(0, 0, CurrentLevel * 5 * Time.unscaledDeltaTime));

        Vector3 relative;
        if (Gamepad.current != null)
        {
            relative = inputActions.Gameplay.Move.ReadValue<Vector3>();
        }
        else
        {
            relative = transform.InverseTransformPoint(Camera.main.ScreenToWorldPoint(new(Mouse.current.position.x.value, Mouse.current.position.y.value, 10)));
        }
        AimAngle = Mathf.Atan2(relative.x, relative.y);
        AimAngle = (AimAngle < 0 ? Mathf.PI - Mathf.Abs(AimAngle) + Mathf.PI : AimAngle);
        playerSprites[0].transform.localPosition = new Vector3(Mathf.Cos(-AimAngle + Mathf.PI / 2), Mathf.Sin(-AimAngle + Mathf.PI / 2), -3.9f) / 32 * (Mathf.Sin(HitAlpha * Mathf.PI) + 1);

        if (HitAlpha <= 0)
        {
            //Debug.Log(Mathf.RoundToInt(AimAngle * Mathf.Rad2Deg / (360f / circles.Count)) % circles.Count);
            selectedCircle = circles[Mathf.FloorToInt(AimAngle * Mathf.Rad2Deg / (360f / circles.Count)) % circles.Count];
            selectionCircle.transform.position = selectedCircle.transform.position;
            selectionCircle.color = selectedCircle.color;
        }

        if (HitAlpha < 0.4f && (inputActions.Gameplay.Jump.WasPressedThisFrame() || Mouse.current.leftButton.wasPressedThisFrame))
        {
            StartCoroutine(AttemptHit());
        }
    }

    void IncreaseLevel()
    {
        CurrentLevel++;

        List<Color> shuffledColors = new List<Color>();
        foreach (var item in potentialColors)
        {
            shuffledColors.Insert(Random.Range(0, shuffledColors.Count), item);
        }
        for (int i = 0; i < shuffledColors.Count; i++)
        {
            circles[i].color = shuffledColors[i];
        }
        Color chosenColor = potentialColors[Random.Range(0, potentialColors.Count)];
        for (int i = 0; i < playerSprites.Count; i++)
        {
            if (i == 2) continue;
            playerSprites[i].color = chosenColor;
        }

    }

    IEnumerator AttemptHit()
    {
        HitAlpha = 1;
        yield return new WaitForSecondsRealtime(0.2f);
        if (selectedCircle.color == playerSprites[0].color)
        {
            IncreaseLevel();
        }
        else
        {

        }

    }
}
