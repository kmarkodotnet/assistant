import { describe, it, expect } from 'vitest';
import { createStore } from './create-store';

describe('createStore', () => {
  it('should initialize with given state', () => {
    const store = createStore({ count: 0 });
    expect(store.state().count).toBe(0);
  });

  it('should update partial state', () => {
    const store = createStore({ count: 0, name: 'test' });
    store.update({ count: 5 });
    expect(store.state().count).toBe(5);
    expect(store.state().name).toBe('test');
  });

  it('should update with function', () => {
    const store = createStore({ count: 0 });
    store.update(s => ({ count: s.count + 1 }));
    expect(store.state().count).toBe(1);
  });

  it('should select derived state reactively', () => {
    const store = createStore({ count: 0 });
    const doubled = store.select(s => s.count * 2);
    expect(doubled()).toBe(0);
    store.update({ count: 3 });
    expect(doubled()).toBe(6);
  });
});
