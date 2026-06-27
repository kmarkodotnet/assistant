export type Relation = 'Self' | 'Spouse' | 'Child' | 'Parent' | 'Other';

export interface FamilyMemberDto {
  id: string;
  displayName: string;
  fullName: string | null;
  relation: Relation;
  birthDate: string | null;
  notes: string | null;
  hasUserAccount: boolean;
  rowVersion: string | null;
  deletedUtc: string | null;
}

export interface CreateFamilyMemberDto {
  displayName: string;
  fullName?: string;
  relation: Relation;
  birthDate?: string;
}

export interface UpdateFamilyMemberDto {
  displayName?: string;
  fullName?: string;
  relation?: Relation;
  birthDate?: string;
  rowVersion: string;
}
