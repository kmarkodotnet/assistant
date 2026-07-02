export interface MockUserPreferences {
  emailEnabled: boolean;
  quietHoursStart: string | null;
  quietHoursEnd: string | null;
}

export interface MockCurrentUser {
  userAccountId: string;
  familyMemberId: string | null;
  displayName: string;
  email: string;
  role: 'Admin' | 'Adult' | 'Child';
  preferences: MockUserPreferences | null;
}

const defaultPreferences: MockUserPreferences = {
  emailEnabled: false,
  quietHoursStart: '22:00',
  quietHoursEnd: '07:00',
};

export const adminUser: MockCurrentUser = {
  userAccountId: '11111111-1111-1111-1111-111111111111',
  familyMemberId: '11111111-1111-1111-1111-111111111112',
  displayName: 'Teszt Admin',
  email: 'admin@example.com',
  role: 'Admin',
  preferences: defaultPreferences,
};

export const adultUser: MockCurrentUser = {
  userAccountId: '22222222-2222-2222-2222-222222222221',
  familyMemberId: '22222222-2222-2222-2222-222222222222',
  displayName: 'Teszt Szülő',
  email: 'adult@example.com',
  role: 'Adult',
  preferences: defaultPreferences,
};

export const childUser: MockCurrentUser = {
  userAccountId: '33333333-3333-3333-3333-333333333331',
  familyMemberId: '33333333-3333-3333-3333-333333333332',
  displayName: 'Teszt Gyerek',
  email: 'child@example.com',
  role: 'Child',
  preferences: defaultPreferences,
};
