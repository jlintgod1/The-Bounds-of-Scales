using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class FallingPlatform : MonoBehaviour
{
    public float FallDelay;
    bool Triggered;
    Vector3 OriginalPosition;

    public Sound FallSound;
    public Sound PreFallSound;
    // Start is called before the first frame update
    void Start()
    {
        OriginalPosition = transform.position;
        FallDelay /= (1 + GameManager.Instance.GlobalDifficulty / 2);
    }

    // Update is called once per frame
    void Update()
    {
        if (Triggered && GetComponent<Rigidbody2D>().constraints == RigidbodyConstraints2D.FreezeAll)
        {
            transform.position = OriginalPosition + new Vector3(Random.Range(-0.0625f, 0.0625f), Random.Range(-0.0625f, 0.0625f), 0);
        }
    }

    void Fall()
    {
        GameManager.PlaySoundAtPoint(FallSound, transform.position);
        GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeRotation;
        GetComponent<Rigidbody2D>().gravityScale = 1;
        Destroy(gameObject, 5);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (Triggered) return;

        if (collision.gameObject.CompareTag("Player") && collision.gameObject.transform.position.y > transform.position.y)
        {
            Triggered = true;
            GameManager.PlaySoundAtPoint(PreFallSound, transform.position);
            Invoke("Fall", FallDelay);
        }
    }
}
