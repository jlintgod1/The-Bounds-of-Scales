using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

public class Crate : MonoBehaviour
{
    new Collider2D collider2D;
    public VisualEffectAsset fireEffect;
    public VisualEffectAsset destroyEffect;
    public GameObject fireEffectCPU;
    public GameObject destroyEffectCPU;
    public GameObject fireEffectCPUInstance;
    public bool onFire { get; private set; }
    VisualEffect fireEffectComponent;

    public Sound IgniteSound;
    public Sound ExplodeSound;

    // Start is called before the first frame update
    void Start()
    {
        collider2D = GetComponent<Collider2D>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public virtual int CanWrapAround()
    {
        if (transform.position.x < (GameManager.CONST_ScreenDimensions.x / -32.0) - 0.625) return 1;
        if (transform.position.x > (GameManager.CONST_ScreenDimensions.x / 32.0) + 0.625) return -1;

        return 0;
    }

    void OnIgnite()
    {
        if (onFire) return;
        onFire = true;

        if (Application.platform == RuntimePlatform.WebGLPlayer)
            fireEffectCPUInstance = Instantiate(fireEffectCPU, transform.position, Quaternion.identity);
        else
            fireEffectComponent = GameManager.Instance.SpawnParticleSystem(fireEffect, transform.position);
        GameManager.PlaySoundAtPoint(IgniteSound, transform.position);

        Collider2D[] surroundingColliders = Physics2D.OverlapBoxAll(transform.position, new(3, 3), 0);
        foreach (var item in surroundingColliders)
        {
            if (item.gameObject.GetComponent<Crate>() == null) continue;
            item.gameObject.GetComponent<Crate>().Invoke("OnIgnite", 0.33f);
        }

        int WrapAroundStatus = CanWrapAround();
        if (WrapAroundStatus != 0)
        {
            Collider2D[] loopedColliders = Physics2D.OverlapBoxAll(new Vector3((GameManager.CONST_ScreenDimensions.x / 32.0f + 0.5f) * WrapAroundStatus, transform.position.y, transform.position.z), new(2, 2), 0);
            foreach (var item in loopedColliders)
            {
                if (item.gameObject.GetComponent<Crate>() == null) continue;
                item.gameObject.GetComponent<Crate>().Invoke("OnIgnite", 0.5f);
            }
        }

        Destroy(this, 2);
    }

    private void OnDestroy()
    {
        if (onFire)
        {
            if (fireEffectComponent != null)
            {
                fireEffectComponent.Stop();
                Destroy(fireEffectComponent.gameObject, 3);
            }
            if (fireEffectCPUInstance != null)
            {
                fireEffectCPUInstance.GetComponent<ParticleSystem>().Stop();
            }

            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                Instantiate(destroyEffectCPU, transform.position, Quaternion.identity);
            }
            else
            {
                VisualEffect destroyEffectComponent = GameManager.Instance.SpawnParticleSystem(destroyEffect, transform.position);
                Destroy(destroyEffectComponent.gameObject, 5);
            }

            GameManager.PlaySoundAtPoint(ExplodeSound, transform.position);
        }

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
