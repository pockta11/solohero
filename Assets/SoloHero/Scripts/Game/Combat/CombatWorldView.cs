using System;
using SoloHero.Core.Combat;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Stage;
using UnityEngine;

namespace SoloHero.Game.Combat
{
    public sealed class CombatWorldView : MonoBehaviour
    {
        private const int EnemySlotVisualCount = 4;

        [SerializeField] private CombatSession _session;
        [SerializeField] private Camera _camera;
        [SerializeField] private SpriteRenderer _heroRenderer;
        [SerializeField] private SpriteRenderer[] _enemyRenderers = new SpriteRenderer[EnemySlotVisualCount];

        private BalanceValues _balance;

        private void Awake()
        {
            if (_session == null)
                _session = GetComponent<CombatSession>();
            if (_camera == null)
                _camera = Camera.main;

            try
            {
                _balance = Services.Get<BalanceValues>();
            }
            catch (Exception)
            {
                _balance = new BalanceValues();
            }
        }

        private void LateUpdate()
        {
            StageRunner runner = _session != null ? _session.Runner : null;
            CombatWorld world = runner != null ? runner.World : null;
            if (runner == null || world == null)
            {
                HideAll();
                return;
            }

            HeroBrain hero = runner.Hero;
            float heroX = (float)hero.X;

            if (_heroRenderer != null)
            {
                _heroRenderer.enabled = true;
                _heroRenderer.transform.position = new Vector3(heroX, 0f, 0f);
                _heroRenderer.color = Color.white;
            }

            FollowCamera(heroX);

            int slotCount = world.SlotCount;
            for (int i = 0; i < EnemySlotVisualCount; i++)
            {
                SpriteRenderer renderer = i < _enemyRenderers.Length ? _enemyRenderers[i] : null;
                if (renderer == null) continue;

                if (i >= slotCount || !world.GetSlot(i).IsActive)
                {
                    renderer.enabled = false;
                    continue;
                }

                EnemyBrain enemy = world.GetSlot(i);
                renderer.enabled = true;
                renderer.transform.position = new Vector3((float)enemy.X, 0f, 0f);
                renderer.color = Color.red;
                float scale = enemy.IsBoss ? 1.5f : 1f;
                renderer.transform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        private void FollowCamera(float heroX)
        {
            if (_camera == null || _balance == null) return;

            float viewWidth = (float)_balance.PIXEL_REF_WIDTH / _balance.PPU;
            float aspect = _camera.aspect;
            if (aspect == 0f)
                aspect = (float)_balance.PIXEL_REF_WIDTH / _balance.PIXEL_REF_HEIGHT;
            float halfWidth = _camera.orthographicSize * aspect;
            _ = halfWidth;

            float cameraX = heroX + (0.5f - _balance.HERO_SCREEN_X / 100f) * viewWidth;
            Vector3 pos = _camera.transform.position;
            pos.x = cameraX;
            _camera.transform.position = pos;
        }

        private void HideAll()
        {
            if (_heroRenderer != null)
                _heroRenderer.enabled = false;

            if (_enemyRenderers == null) return;
            for (int i = 0; i < _enemyRenderers.Length; i++)
            {
                if (_enemyRenderers[i] != null)
                    _enemyRenderers[i].enabled = false;
            }
        }
    }
}
