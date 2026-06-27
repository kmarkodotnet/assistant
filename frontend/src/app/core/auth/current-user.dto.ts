export interface UserPreferencesDto {
  emailEnabled: boolean;
  quietHoursStart: string | null;
  quietHoursEnd: string | null;
}

export interface CurrentUserDto {
  userAccountId: string;
  familyMemberId: string | null;
  displayName: string;
  email: string;
  role: 'Admin' | 'Adult' | 'Child';
  preferences: UserPreferencesDto | null;
}
