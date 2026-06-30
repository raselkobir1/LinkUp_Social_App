import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../models/api-response.model';
import { FriendDto, FriendRequestDto, MutualFriendDto, BlockedUserDto } from '../models/friend.model';

@Injectable({ providedIn: 'root' })
export class FriendService {
  private base = `${environment.apiUrl}/friends`;

  constructor(private http: HttpClient) {}

  sendRequest(receiverId: string): Observable<ApiResponse<FriendRequestDto>> {
    return this.http.post<ApiResponse<FriendRequestDto>>(`${this.base}/request`, { receiverId });
  }

  acceptRequest(requestId: string): Observable<ApiResponse<object>> {
    return this.http.put<ApiResponse<object>>(`${this.base}/request/${requestId}/accept`, {});
  }

  rejectRequest(requestId: string): Observable<ApiResponse<object>> {
    return this.http.put<ApiResponse<object>>(`${this.base}/request/${requestId}/reject`, {});
  }

  cancelRequest(requestId: string): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.base}/request/${requestId}`);
  }

  unfriend(userId: string): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.base}/${userId}`);
  }

  getFriends(page = 1, pageSize = 20): Observable<ApiResponse<PagedResult<FriendDto>>> {
    return this.http.get<ApiResponse<PagedResult<FriendDto>>>(`${this.base}`, {
      params: new HttpParams().set('page', page).set('pageSize', pageSize)
    });
  }

  getPendingRequests(page = 1, pageSize = 20): Observable<ApiResponse<PagedResult<FriendRequestDto>>> {
    return this.http.get<ApiResponse<PagedResult<FriendRequestDto>>>(`${this.base}/requests/pending`, {
      params: new HttpParams().set('page', page).set('pageSize', pageSize)
    });
  }

  getSentRequests(page = 1, pageSize = 20): Observable<ApiResponse<PagedResult<FriendRequestDto>>> {
    return this.http.get<ApiResponse<PagedResult<FriendRequestDto>>>(`${this.base}/requests/sent`, {
      params: new HttpParams().set('page', page).set('pageSize', pageSize)
    });
  }

  getMutualFriends(userId: string): Observable<ApiResponse<MutualFriendDto[]>> {
    return this.http.get<ApiResponse<MutualFriendDto[]>>(`${this.base}/mutual/${userId}`);
  }

  getSuggestions(count = 50): Observable<ApiResponse<FriendDto[]>> {
    return this.http.get<ApiResponse<FriendDto[]>>(`${this.base}/suggestions`, {
      params: new HttpParams().set('count', count)
    });
  }

  blockUser(userId: string): Observable<ApiResponse<object>> {
    return this.http.post<ApiResponse<object>>(`${this.base}/block/${userId}`, {});
  }

  unblockUser(userId: string): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.base}/block/${userId}`);
  }

  getBlocked(): Observable<ApiResponse<BlockedUserDto[]>> {
    return this.http.get<ApiResponse<BlockedUserDto[]>>(`${this.base}/blocked`);
  }
}
