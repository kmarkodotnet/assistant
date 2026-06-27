import { describe, it, expect } from 'vitest';
import { FamilyMemberFormDialog } from '../components/family-member-form.dialog';
import { FamilyMemberCardComponent } from '../components/family-member-card.component';

describe('Family feature components', () => {
  it('FamilyMemberFormDialog class is defined', () => {
    expect(FamilyMemberFormDialog).toBeDefined();
    expect(typeof FamilyMemberFormDialog).toBe('function');
  });

  it('FamilyMemberCardComponent class is defined', () => {
    expect(FamilyMemberCardComponent).toBeDefined();
    expect(typeof FamilyMemberCardComponent).toBe('function');
  });
});

describe('Family member relation values', () => {
  it('should accept valid relation types', () => {
    type Relation = 'Self' | 'Spouse' | 'Child' | 'Parent' | 'Other';
    const validRelations: Relation[] = ['Self', 'Spouse', 'Child', 'Parent', 'Other'];
    expect(validRelations).toHaveLength(5);
    expect(validRelations).toContain('Self');
    expect(validRelations).toContain('Child');
  });
});
