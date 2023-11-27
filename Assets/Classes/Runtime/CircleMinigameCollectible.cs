using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleMinigameCollectible : SnakeVenom
{
    public List<SpriteRenderer> circles;
    public List<Color> potentialColors;
    List<Vector3> initalPositions = new List<Vector3>();
    // Start is called before the first frame update
    protected void Start()
    {
        base.Start();
        int startPosition = Random.Range(0, circles.Count / 2);
        for (int i = 0; i < circles.Count; i++)
        {
            circles[i].color = potentialColors[(startPosition + i) % circles.Count];
            initalPositions.Add(new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(-0.25f, 0.25f), i * 0.01f));
            circles[i].transform.localPosition = initalPositions[i];
        }
    }

    // Update is called once per frame
    protected void Update()
    {
        base.Update();

        for (int i = 0; i < circles.Count; i++)
        {
            circles[i].transform.localPosition = initalPositions[i] + new Vector3(0, Mathf.Cos(Mathf.PI * Time.time + initalPositions[i].x + initalPositions[i].y) / 16, 0);
        }
    }
}
