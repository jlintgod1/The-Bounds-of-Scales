using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnakeVenom : MonoBehaviour
{
    Vector3 InitalPosition;
    bool Collected;
    public bool GivesHealth;
    // Start is called before the first frame update
    protected void Start()
    {
        InitalPosition = transform.position;
    }

    // Update is called once per frame
    protected void Update()
    {
        transform.position = InitalPosition + new Vector3(0, Mathf.Sin(Mathf.PI * Time.time + InitalPosition.x + InitalPosition.y) / 16, 0);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (Collected) { return; }

        if (collision.gameObject.CompareTag("Player"))
        {
            Collected = true;
            // TO-DO: Collect sound
            // TO-DO: Collect particles

            GameManager.Instance.OnCollectCollectible(this);
            Destroy(gameObject);
        }
    }
}
