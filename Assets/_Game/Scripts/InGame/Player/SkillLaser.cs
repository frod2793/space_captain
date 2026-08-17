using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class SkillLaser : MonoBehaviour, IPoolable
{
    [SerializeField] private float m_laserRange = 20f;
    [SerializeField] private float m_laserWidth = 3f;
    [SerializeField] private int m_damage = 100;
    [SerializeField] private float m_duration = 1.0f;
    [SerializeField] private Color m_laserColor = Color.cyan;
    [SerializeField] private SpriteRenderer m_laserSprite;
    private LineRenderer m_weaponLine;
    private ObjectPoolManager m_weaponPool;
    private bool m_isWeaponVisual;
    private static Material s_weaponVisualMaterial;

    public static GameObject SpawnWeaponVisual(
        ObjectPoolManager pool,
        GameObject prefab,
        Vector3 startPosition,
        Vector3 direction,
        float range,
        float width,
        Color color,
        float duration = 0.15f)
    {
        if (pool == null || prefab == null)
        {
            return null;
        }

        GameObject visualObject = pool.GetFromPool(prefab, startPosition, Quaternion.identity);
        if (visualObject == null || !visualObject.TryGetComponent<SkillLaser>(out var effect))
        {
            return null;
        }

        effect.PlayWeaponVisual(pool, startPosition, direction, range, width, color, duration);
        return visualObject;
    }

    private void PlayWeaponVisual(
        ObjectPoolManager pool,
        Vector3 startPosition,
        Vector3 direction,
        float range,
        float width,
        Color color,
        float duration)
    {
        m_weaponPool = pool;
        m_isWeaponVisual = true;
        if (m_laserSprite != null)
        {
            m_laserSprite.enabled = false;
        }
        m_weaponLine = m_weaponLine != null ? m_weaponLine : gameObject.GetComponent<LineRenderer>();
        if (m_weaponLine == null)
        {
            m_weaponLine = gameObject.AddComponent<LineRenderer>();
        }

        var line = m_weaponLine;
        line.enabled = true;
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.SetPosition(0, startPosition);
        line.SetPosition(1, startPosition + direction.normalized * Mathf.Max(0f, range));
        line.startWidth = Mathf.Max(0.01f, width);
        line.endWidth = line.startWidth;
        line.startColor = color;
        line.endColor = color;
        line.sortingOrder = 100;
        line.sharedMaterial = GetWeaponVisualMaterial();

        Color transparent = color;
        transparent.a = 0f;
        DOTween.To(() => line.startColor, value =>
            {
                if (line != null)
                {
                    line.startColor = value;
                    line.endColor = value;
                }
            }, transparent, duration)
            .SetUpdate(true)
            .SetId(gameObject)
            .SetEase(Ease.InExpo)
            .OnComplete(ReleaseWeaponVisual);
    }

    private static Material GetWeaponVisualMaterial()
    {
        if (s_weaponVisualMaterial == null)
        {
            s_weaponVisualMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        return s_weaponVisualMaterial;
    }

    private void ReleaseWeaponVisual()
    {
        if (!m_isWeaponVisual)
        {
            return;
        }

        if (m_weaponPool != null)
        {
            m_weaponPool.ReturnToPool(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void OnSpawn()
    {
        DOTween.Kill(gameObject);
        m_isWeaponVisual = false;
        m_weaponPool = null;
        if (m_weaponLine != null)
        {
            m_weaponLine.enabled = false;
        }
    }

    public void OnDespawn()
    {
        DOTween.Kill(gameObject);
        m_isWeaponVisual = false;
        m_weaponPool = null;
        if (m_weaponLine != null)
        {
            m_weaponLine.enabled = false;
        }
    }

    private void OnDestroy()
    {
        DOTween.Kill(gameObject);
    }

    public void Trigger(PlayerCharacterController owner)
    {
        Vector3 startPos = transform.position;
        Vector3 direction = transform.up;

        m_laserSprite.enabled = true;
        
        Color baseColor = m_laserColor;
        baseColor.a = 1f;
        m_laserSprite.color = baseColor;
        m_laserSprite.sortingOrder = 100;

        m_laserSprite.transform.position = startPos + direction * (m_laserRange * 0.5f);
        m_laserSprite.transform.up = direction;

        Vector3 worldScale = new Vector3(m_laserWidth, m_laserRange, 1f);
        Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        m_laserSprite.transform.localScale = new Vector3(worldScale.x / parentScale.x, worldScale.y / parentScale.y, 1f);

        m_laserSprite.DOKill();
        m_laserSprite.DOFade(0f, m_duration).SetUpdate(true).SetEase(Ease.InExpo).OnComplete(() =>
        {
            Destroy(gameObject);
        });

        int hitCount = WeaponTargetQuery.BoxCast(
            startPos,
            new Vector2(m_laserWidth, 0.1f),
            0f,
            direction,
            m_laserRange,
            out RaycastHit2D[] hits);
        for (int i = 0; i < hitCount; i++)
        {
            if (WeaponTargetQuery.TryGetEnemyTarget(hits[i].collider, out IAttackTarget target))
            {
                target.TakeDamage(m_damage, owner.CharacterID);
            }
        }
    }
}
