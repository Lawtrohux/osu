// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using System.Collections.Generic;

namespace osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Utils
{
    public class AccessorEnumerator<T> : IEnumerator<T>
    {
        private T start;

        public T Current { get; private set; }

        object? IEnumerator.Current => Current;

        private readonly Func<T, T?> accessor;

        public AccessorEnumerator(T start, Func<T, T?> accessor)
        {
            this.start = start;
            Current = start;
            this.accessor = accessor;
        }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            Current = accessor(Current);
            return Current != null;
        }

        public void Reset()
        {
            Current = start;
        }
    }

    public class AccessorEnumerable<T> : IEnumerable<T>
    {
        private T start;

        private readonly Func<T, T?> accessor;

        public AccessorEnumerable(T start, Func<T, T?> accessor)
        {
            this.start = start;
            this.accessor = accessor;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new AccessorEnumerator<T>(start, accessor);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new AccessorEnumerator<T>(start, accessor);
        }
    }
}
