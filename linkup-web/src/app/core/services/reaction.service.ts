import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { ReactionType } from '../models/post.model';

@Injectable({ providedIn: 'root' })
export class ReactionService {
  private base = `${environment.apiUrl}/reactions`;

  constructor(private http: HttpClient) {}

  react(targetId: string, targetType: 'Post' | 'Comment', type: ReactionType): Observable<ApiResponse<object>> {
    return this.http.post<ApiResponse<object>>(this.base, { targetId, targetType, type });
  }

  removeReaction(targetId: string, targetType: 'Post' | 'Comment'): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.base}/${targetType}/${targetId}`);
  }

  getCounts(targetId: string, targetType: 'Post' | 'Comment'): Observable<ApiResponse<Record<ReactionType, number>>> {
    return this.http.get<ApiResponse<Record<ReactionType, number>>>(`${this.base}/${targetType}/${targetId}`);
  }
}
