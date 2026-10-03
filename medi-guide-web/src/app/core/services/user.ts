import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  UserProfile,
  UpdateUserProfile,
  AdminUser,
  AdminCreateUser,
  AdminUpdateUser,
  UserQueryParams,
} from '../models/user.model';
import { PagedResult } from '../models/booking.model';

@Injectable({
  providedIn: 'root',
})
export class UserService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/users`;

  // Profile endpoints
  getMe(): Observable<UserProfile> {
    return this.http.get<UserProfile>(`${this.baseUrl}/me`);
  }

  updateMe(dto: UpdateUserProfile): Observable<UserProfile> {
    return this.http.patch<UserProfile>(`${this.baseUrl}/me`, dto);
  }

  // Admin User Management endpoints
  getAll(params?: UserQueryParams): Observable<PagedResult<AdminUser>> {
    let httpParams = new HttpParams();
    if (params?.page) httpParams = httpParams.set('page', params.page.toString());
    if (params?.pageSize) httpParams = httpParams.set('pageSize', params.pageSize.toString());
    if (params?.search) httpParams = httpParams.set('search', params.search);
    if (params?.role) httpParams = httpParams.set('role', params.role);

    return this.http.get<PagedResult<AdminUser>>(this.baseUrl, { params: httpParams });
  }

  getById(id: string): Observable<AdminUser> {
    return this.http.get<AdminUser>(`${this.baseUrl}/${id}`);
  }

  create(dto: AdminCreateUser): Observable<AdminUser> {
    return this.http.post<AdminUser>(this.baseUrl, dto);
  }

  update(id: string, dto: AdminUpdateUser): Observable<AdminUser> {
    return this.http.patch<AdminUser>(`${this.baseUrl}/${id}`, dto);
  }

  deactivate(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
