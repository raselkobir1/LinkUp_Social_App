import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../models/api-response.model';
import { DashboardStats, AdminUser, AdminPost, AdminReport } from '../models/admin.model';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private base = `${environment.apiUrl}/admin`;

  constructor(private http: HttpClient) {}

  getDashboard(): Observable<ApiResponse<DashboardStats>> {
    return this.http.get<ApiResponse<DashboardStats>>(`${this.base}/dashboard`);
  }

  getUsers(opts: { search?: string; isSuspended?: boolean; isActive?: boolean; page?: number; pageSize?: number } = {}): Observable<ApiResponse<PagedResult<AdminUser>>> {
    let params = new HttpParams()
      .set('pageNumber', opts.page ?? 1)
      .set('pageSize', opts.pageSize ?? 20);
    if (opts.search) params = params.set('search', opts.search);
    if (opts.isSuspended != null) params = params.set('isSuspended', opts.isSuspended);
    if (opts.isActive != null) params = params.set('isActive', opts.isActive);
    return this.http.get<ApiResponse<PagedResult<AdminUser>>>(`${this.base}/users`, { params });
  }

  getUser(userId: string): Observable<ApiResponse<AdminUser>> {
    return this.http.get<ApiResponse<AdminUser>>(`${this.base}/users/${userId}`);
  }

  suspendUser(userId: string, reason: string): Observable<ApiResponse<object>> {
    return this.http.put<ApiResponse<object>>(`${this.base}/users/${userId}/suspend`, { reason });
  }

  unsuspendUser(userId: string): Observable<ApiResponse<object>> {
    return this.http.put<ApiResponse<object>>(`${this.base}/users/${userId}/unsuspend`, {});
  }

  deleteUser(userId: string): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.base}/users/${userId}`);
  }

  getPosts(page = 1, pageSize = 20): Observable<ApiResponse<PagedResult<AdminPost>>> {
    return this.http.get<ApiResponse<PagedResult<AdminPost>>>(`${this.base}/posts`, {
      params: new HttpParams().set('page', page).set('pageSize', pageSize)
    });
  }

  deletePost(postId: string): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.base}/posts/${postId}`);
  }

  getReports(page = 1, pageSize = 20, includeResolved = false): Observable<ApiResponse<PagedResult<AdminReport>>> {
    return this.http.get<ApiResponse<PagedResult<AdminReport>>>(`${this.base}/reports`, {
      params: new HttpParams().set('page', page).set('pageSize', pageSize).set('includeResolved', includeResolved)
    });
  }

  resolveReport(reportId: string): Observable<ApiResponse<object>> {
    return this.http.put<ApiResponse<object>>(`${this.base}/reports/${reportId}/resolve`, {});
  }
}
