using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterDroplet : MonoBehaviour
{
    public float TerminalVelocity;
    public List<GameObject> EnemyPrefabs;
    public SpriteRenderer fallWarning;

    void Start()
    {
        Instantiate(EnemyPrefabs[Random.Range(0, EnemyPrefabs.Count)], transform.position, Quaternion.identity);
        TerminalVelocity *= 1 + GameManager.Instance.GlobalDifficulty / 2;
    }

    // Update is called once per frame
    void Update()
    {
        if (Mathf.Abs(transform.position.y - Camera.main.transform.position.y) > 15)
        {
            GetComponent<Rigidbody2D>().velocity = new(0, 0);
            fallWarning.gameObject.transform.localScale = new(0, 1, 1);
            return;
        }

        GetComponent<Rigidbody2D>().velocity = new(0, Mathf.Max(GetComponent<Rigidbody2D>().velocity.y, -TerminalVelocity));

        if (transform.position.y + 24 < Camera.main.transform.position.y)
            Destroy(gameObject);

        fallWarning.gameObject.transform.localScale = new(Mathf.MoveTowards(fallWarning.gameObject.transform.localScale.x, 1, 2 * Time.deltaTime), 1, 1);
    }
}
