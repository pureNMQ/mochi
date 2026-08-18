using System.Collections;
using System.Collections.Generic;

namespace Mochi
{

    [System.Serializable]
    public sealed class Observer<T>
    {
        public delegate void ValueChangedHandler(T lastValue, T nextValue);

        public T Value
        {
            get => GetValue();
            set => SetValue(value);
        }

        public event ValueChangedHandler OnValueChanged;
        private T value;

        public void SetValue(T value)
        {
            T lastValue = this.value;
            this.value = value;
            OnValueChanged?.Invoke(lastValue, value);
        }

        public T GetValue()
        {
            return this.value;
        }

    }
}
