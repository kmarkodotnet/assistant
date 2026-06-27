import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import type { FamilyMemberDto, CreateFamilyMemberDto, UpdateFamilyMemberDto } from '../models/family-member.dto';

@Injectable({ providedIn: 'root' })
export class FamilyApiService {
  private http = inject(HttpClient);
  private base = '/api/v1/family-members';

  list(relation?: string): Observable<FamilyMemberDto[]> {
    const params = relation ? { relation } : {};
    return this.http.get<FamilyMemberDto[]>(this.base, { params });
  }

  get(id: string): Observable<FamilyMemberDto> {
    return this.http.get<FamilyMemberDto>(`${this.base}/${id}`);
  }

  create(dto: CreateFamilyMemberDto): Observable<FamilyMemberDto> {
    return this.http.post<FamilyMemberDto>(this.base, dto);
  }

  update(id: string, dto: UpdateFamilyMemberDto): Observable<FamilyMemberDto> {
    return this.http.patch<FamilyMemberDto>(`${this.base}/${id}`, dto);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
