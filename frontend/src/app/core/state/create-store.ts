import { signal, computed } from '@angular/core';

export function createStore<T extends object>(initial: T) {
  const _state = signal<T>({ ...initial });
  return {
    state: _state.asReadonly(),
    update(patch: Partial<T> | ((s: T) => Partial<T>)) {
      _state.update(s => ({ ...s, ...(typeof patch === 'function' ? patch(s) : patch) }));
    },
    set(next: T) {
      _state.set(next);
    },
    select<R>(fn: (s: T) => R) {
      return computed(() => fn(_state()));
    },
  };
}
