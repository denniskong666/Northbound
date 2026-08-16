using System;
using System.Collections.Generic;
using UnityEngine;

namespace Northbound.Core
{
    public sealed class InputGate : MonoBehaviour
    {
        private readonly HashSet<int> activeLeases = new HashSet<int>();
        private int nextLeaseId;

        public bool IsBlocked => activeLeases.Count > 0;

        public IDisposable Acquire(object owner)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            var leaseId = ++nextLeaseId;
            activeLeases.Add(leaseId);
            return new InputLease(this, leaseId);
        }

        private void Release(int leaseId)
        {
            activeLeases.Remove(leaseId);
        }

        private sealed class InputLease : IDisposable
        {
            private InputGate gate;
            private readonly int leaseId;

            public InputLease(InputGate gate, int leaseId)
            {
                this.gate = gate;
                this.leaseId = leaseId;
            }

            public void Dispose()
            {
                if (gate == null)
                {
                    return;
                }

                gate.Release(leaseId);
                gate = null;
            }
        }
    }
}
