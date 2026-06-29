import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
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

  deleteMessage(messageId: string): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.base}/messages/${messageId}`);
  }

  markRead(messageId: string): Observable<ApiResponse<object>> {
    return this.http.post<ApiResponse<object>>(`${this.base}/messages/${messageId}/mark-read`, {});
  }

  searchMessages(chatId: string, query: string): Observable<ApiResponse<MessageDto[]>> {
    return this.http.get<ApiResponse<MessageDto[]>>(`${this.base}/messages/search`, {
      params: new HttpParams().set('chatId', chatId).set('query', query)
    });
  }

  createGroup(dto: { name: string; description?: string; memberIds: string[] }): Observable<ApiResponse<GroupChatDto>> {
    return this.http.post<ApiResponse<GroupChatDto>>(`${environment.apiUrl}/groups`, dto);
  }

  getGroupInfo(groupId: string): Observable<ApiResponse<GroupChatDto>> {
    return this.http.get<ApiResponse<GroupChatDto>>(`${environment.apiUrl}/groups/${groupId}`);
  }
}
