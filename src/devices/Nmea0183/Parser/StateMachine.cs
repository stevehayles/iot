using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace Nmea.Parser
{
    public class StateMachine<T>
    {
        public delegate T StateMachineAction(ref SequenceReader<byte> reader);

        private readonly Dictionary<T, StateMachineAction> _actions = new Dictionary<T, StateMachineAction>();

        public T State { get; set; }

        public StateMachineAction this[T state]
        {
            get => _actions[state];
            set => _actions[state] = value;
        }

        public void Update(ref SequenceReader<byte> reader) => State = _actions[State](ref reader);
    }
}
