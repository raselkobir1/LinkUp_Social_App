import { Injectable, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../models/api-response.model';
import { NotificationDto, NotificationSettingsDto } from '../models/notification.model';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  unreadCount = signal(0);

  private base = `${environment.apiUrl}/notifications`;

  constructor(private http: HttpClient) {}

  getNotifications(page = 1, pageSize = 20): Observable<ApiResponse<PagedResult<NotificationDto>>> {
    return this.http.get<ApiResponse<PagedResult<NotificationDto>>>(this.base, {
      params: new HttpParams().set('page', page).set('pageSize', pageSize)
    });
  }

  markAsRead(id: string): Observable<ApiResponse<object>> {
    return this.http.put<ApiResponse<object>>(`${this.base}/${id}/read`, {});
  }

  markAllAsRead(): Observable<ApiResponse<object>> {
    return this.http.put<ApiResponse<object>>(`${this.base}/read-all`, {});
  }

  getUnreadCount(): Observable<ApiResponse<{ unreadCount: number }>> {
    return this.http.get<ApiResponse<{ unreadCount: number }>>(`${this.base}/unread-count`);
  }

  delete(id: string): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.base}/${id}`);
  }

  getSettings(): Observable<ApiResponse<NotificationSettingsDto>> {
    return this.http.get<ApiResponse<NotificationSettingsDto>>(`${this.base}/settings`);
  }

  updateSettings(dto: NotificationSettingsDto): Observable<ApiResponse<NotificationSettingsDto>> {
    return this.http.put<ApiResponse<NotificationSettingsDto>>(`${this.base}/settings`, dto);
  }
}
