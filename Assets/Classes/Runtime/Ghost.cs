using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ghost : MonoBehaviour
{
    public float RiseSpeed;
    Vector2 OriginalPosition;
    // Start is called before the first frame update
    void Start()
    {
        OriginalPosition = transform.position;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector2 CurrentVelocity = new(0,0);
        Vector2 TargetPosition = Mathf.Abs(OriginalPosition.y - GameManager.Instance.Player.transform.position.y) < 24 ? GameManager.Instance.Player.transform.position : OriginalPosition;
        GetComponent<Rigidbody2D>().MovePosition(transform.position * Vector2.right + Vector2.MoveTowards(transform.position, TargetPosition, RiseSpeed * Time.fixedDeltaTime) * Vector2.up);
        GetComponent<SpriteRenderer>().flipX = transform.position.x - GameManager.Instance.Player.transform.position.x > 0;
        
        
    }
}
