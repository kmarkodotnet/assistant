import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import type {
  TaskListItemDto,
  TaskDto,
  TaskListParams,
  CreateTaskRequest,
  PatchTaskRequest,
} from '../models/task.dto';

@Injectable({ providedIn: 'root' })
export class TasksApiService {
  private http = inject(HttpClient);
  private base = '/api/v1/tasks';

  list(params?: TaskListParams): Observable<TaskListItemDto[]> {
    const query: Record<string, string> = {};
    if (params?.status) query['status'] = params.status;
    if (params?.assignedToFamilyMemberId) query['assignedToFamilyMemberId'] = params.assignedToFamilyMemberId;
    if (params?.priority) query['priority'] = params.priority;
    if (params?.origin) query['origin'] = params.origin;
    if (params?.page != null) query['page'] = String(params.page);
    if (params?.pageSize != null) query['pageSize'] = String(params.pageSize);
    return this.http.get<TaskListItemDto[]>(this.base, { params: query });
  }

  get(id: string): Observable<TaskDto> {
    return this.http.get<TaskDto>(`${this.base}/${id}`);
  }

  create(req: CreateTaskRequest): Observable<TaskDto> {
    return this.http.post<TaskDto>(this.base, req);
  }

  patch(id: string, req: PatchTaskRequest): Observable<TaskDto> {
    return this.http.patch<TaskDto>(`${this.base}/${id}`, req);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  approve(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/approve`, {});
  }

  reject(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/reject`, {});
  }

  start(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/start`, {});
  }

  complete(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/complete`, {});
  }

  cancel(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/cancel`, {});
  }
}
