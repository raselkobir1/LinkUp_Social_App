import { Injectable } from '@angular/core';
import { HttpClient, HttpParams, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../models/api-response.model';
import { ChatListDto, MessageDto, SendMessageDto, GroupChatDto } from '../models/chat.model';

@Injectable({ providedIn: 'root' })
export class ChatService {
  private base = `${environment.apiUrl}/chats`;

  constructor(private http: HttpClient) {}

  getChatList(): Observable<ApiResponse<ChatListDto[]>> {
    return this.http.get<ApiResponse<ChatListDto[]>>(this.base);
  }

  getOrCreateDirectChat(otherUserId: string): Observable<ApiResponse<ChatListDto>> {
    return this.http.post<ApiResponse<ChatListDto>>(`${this.base}/direct`, { targetUserId: otherUserId });
  }

  getMessages(chatId: string, page = 1, pageSize = 30): Observable<ApiResponse<PagedResult<MessageDto>>> {
    return this.http.get<ApiResponse<PagedResult<MessageDto>>>(`${this.base}/${chatId}/messages`, {
      params: new HttpParams().set('page', page).set('pageSize', pageSize)
    });
  }

  sendMessage(dto: SendMessageDto): Observable<ApiResponse<MessageDto>> {
    return this.http.post<ApiResponse<MessageDto>>(`${this.base}/messages`, dto);
  }

  editMessage(messageId: string, content: string): Observable<ApiResponse<MessageDto>> {
    return this.http.put<ApiResponse<MessageDto>>(`${this.base}/messages/${messageId}`, { content });
  }

  deleteForMe(messageId: string): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.base}/messages/${messageId}/me`);
  }

  deleteForEveryone(messageId: string): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.base}/messages/${messageId}/everyone`);
  }

  markRead(messageId: string): Observable<ApiResponse<object>> {
    return this.http.post<ApiResponse<object>>(`${this.base}/messages/${messageId}/mark-read`, {});
  }

  searchMessages(chatId: string, query: string): Observable<ApiResponse<PagedResult<MessageDto>>> {
    return this.http.get<ApiResponse<PagedResult<MessageDto>>>(`${this.base}/${chatId}/messages/search`, {
      params: new HttpParams().set('query', query)
    });
  }

  createGroup(dto: { name: string; description?: string; memberIds: string[] }): Observable<ApiResponse<GroupChatDto>> {
    return this.http.post<ApiResponse<GroupChatDto>>(`${environment.apiUrl}/groups`, dto);
  }

  getGroupInfo(groupId: string): Observable<ApiResponse<GroupChatDto>> {
    return this.http.get<ApiResponse<GroupChatDto>>(`${environment.apiUrl}/groups/${groupId}`);
  }

  updateGroup(chatId: string, dto: { name: string; description?: string }): Observable<ApiResponse<GroupChatDto>> {
    return this.http.put<ApiResponse<GroupChatDto>>(`${environment.apiUrl}/groups/${chatId}`, dto);
  }

  addGroupMembers(chatId: string, userIds: string[]): Observable<ApiResponse<GroupChatDto>> {
    return this.http.post<ApiResponse<GroupChatDto>>(`${environment.apiUrl}/groups/${chatId}/members`, { userIds });
  }

  removeGroupMember(chatId: string, memberId: string): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${environment.apiUrl}/groups/${chatId}/members/${memberId}`);
  }

  makeGroupAdmin(chatId: string, memberId: string): Observable<ApiResponse<object>> {
    return this.http.post<ApiResponse<object>>(`${environment.apiUrl}/groups/${chatId}/members/${memberId}/make-admin`, {});
  }

  leaveGroup(chatId: string): Observable<ApiResponse<object>> {
    return this.http.post<ApiResponse<object>>(`${environment.apiUrl}/groups/${chatId}/leave`, {});
  }

  changeGroupPhoto(chatId: string, photoUrl: string): Observable<ApiResponse<object>> {
    // Backend action binds [FromBody] string, so the body must be a JSON-encoded string.
    return this.http.put<ApiResponse<object>>(`${environment.apiUrl}/groups/${chatId}/photo`,
      JSON.stringify(photoUrl), { headers: new HttpHeaders({ 'Content-Type': 'application/json' }) });
  }
}
