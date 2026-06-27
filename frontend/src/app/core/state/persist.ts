import { effect } from '@angular/core';
import { createStore } from './create-store';

export function withPersistence<T extends object>(
  store: ReturnType<typeof createStore<T>>,
  key: string,
): void {
  const saved = localStorage.getItem(key);
  if (saved) {
    try {
      store.set(JSON.parse(saved) as T);
    } catch {
      // ignore corrupt storage
    }
  }
  effect(() => {
    localStorage.setItem(key, JSON.stringify(store.state()));
  });
}
