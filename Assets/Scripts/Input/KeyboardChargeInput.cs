using System;
using MRKnobJump.Core;
using UnityEngine;

namespace MRKnobJump.Input
{
    [DisallowMultipleComponent]
    public sealed class KeyboardChargeInput : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float maxChargeTime = 2f;

        private bool inputEnabled = true;
        private bool isCharging;
        private float holdTime;

        public float HoldTime => holdTime;
        public float Compression => Mathf.Clamp01(holdTime / maxChargeTime);
        public bool IsCharging => isCharging;
        public event Action ChargeReleased;

        private void Update()
        {
            if (GameSettings.SelectedInputMode != InputMode.Keyboard || !inputEnabled)
            {
                return;
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Space))
            {
                BeginCharge();
            }

            if (isCharging && UnityEngine.Input.GetKey(KeyCode.Space))
            {
                AdvanceCharge(Time.deltaTime);
            }

            if (isCharging && UnityEngine.Input.GetKeyUp(KeyCode.Space))
            {
                ReleaseCharge();
            }
        }

        public void SetInputEnabled(bool value)
        {
            inputEnabled = value;
            if (!value)
            {
                ResetCharge();
            }
        }

        public void ResetCharge()
        {
            holdTime = 0f;
            isCharging = false;
        }

        private void BeginCharge()
        {
            if (!inputEnabled || GameSettings.SelectedInputMode != InputMode.Keyboard)
            {
                return;
            }

            holdTime = 0f;
            isCharging = true;
        }

        private void AdvanceCharge(float deltaTime)
        {
            if (!isCharging || !inputEnabled)
            {
                return;
            }

            holdTime = Mathf.Min(holdTime + Mathf.Max(0f, deltaTime), maxChargeTime);
        }

        private void ReleaseCharge()
        {
            if (!isCharging || !inputEnabled)
            {
                return;
            }

            ChargeReleased?.Invoke();
            ResetCharge();
        }

        private void OnDisable()
        {
            ResetCharge();
        }

        private void OnValidate()
        {
            maxChargeTime = Mathf.Max(0.01f, maxChargeTime);
        }
    }
}
