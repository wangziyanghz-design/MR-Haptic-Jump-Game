using System;
using MRKnobJump.Core;
using UnityEngine;

namespace MRKnobJump.Input
{
    [DisallowMultipleComponent]
    public sealed class ReleaseInput : MonoBehaviour
    {
        public event Action ReleasePressed;

        private void Update()
        {
            if (GameSettings.SelectedInputMode == InputMode.Knob &&
                UnityEngine.Input.GetKeyDown(KeyCode.Space))
            {
                ReleasePressed?.Invoke();
            }
        }
    }
}
