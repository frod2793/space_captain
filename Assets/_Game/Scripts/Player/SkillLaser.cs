using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class SkillLaser : MonoBehaviour
{
    [SerializeField] private float m_laserRange = 20f;
    [SerializeField] private float m_laserWidth = 3f;
    [SerializeField] private int m_damage = 100;
    [SerializeField] private float m_duration = 1.0f;
    [SerializeField] private Color m_laserColor = Color.cyan;
    [SerializeField] private SpriteRenderer m_laserSprite;

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

        RaycastHit2D[] hits = Physics2D.BoxCastAll(startPos, new Vector2(m_laserWidth, 0.1f), 0f, direction, m_laserRange);
        foreach (var hit in hits)
        {
            if (hit.collider.TryGetComponent<EnemyController>(out var enemy))
            {
                enemy.TakeDamage(m_damage);
            }
            else if (hit.collider.TryGetComponent<BossController>(out var boss))
            {
                boss.TakeDamage(m_damage);
            }
        }
    }
}
