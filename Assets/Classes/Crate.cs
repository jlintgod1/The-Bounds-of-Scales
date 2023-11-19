using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class Crate : MonoBehaviour
{
    new Collider2D collider2D;
    public VisualEffectAsset fireEffect;
    public VisualEffectAsset destroyEffect;
    public bool onFire { get; private set; }
    VisualEffect fireEffectComponent;

    // Start is called before the first frame update
    void Start()
    {
        collider2D = GetComponent<Collider2D>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnIgnite()
    {
        if (onFire) return;
        onFire = true;

        fireEffectComponent = GameManager.Instance.SpawnParticleSystem(fireEffect, transform.position);

        Collider2D[] surroundingColliders = Physics2D.OverlapBoxAll(transform.position, new(3, 3), 0);
        foreach (var item in surroundingColliders)
        {
            if (item.gameObject.GetComponent<Crate>() == null) continue;
            item.gameObject.GetComponent<Crate>().Invoke("OnIgnite", 0.33f);
        }

        Invoke("PostIgnite", 2);
    }

    void PostIgnite()
    {
        fireEffectComponent.Stop();
        Destroy(fireEffectComponent.gameObject, 3);
        VisualEffect destroyEffectComponent = GameManager.Instance.SpawnParticleSystem(destroyEffect, transform.position);
        Destroy(destroyEffectComponent.gameObject, 5);
        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && 
            collision.gameObject.GetComponent<PlayerController_Logic>().FireTimer > 0)
        {
            OnIgnite();
        }
    }
}
