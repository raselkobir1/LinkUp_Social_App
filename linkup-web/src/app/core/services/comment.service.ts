import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../models/api-response.model';
import { CommentDto, CreateCommentDto, UpdateCommentDto } from '../models/comment.model';

@Injectable({ providedIn: 'root' })
export class CommentService {
  private base = `${environment.apiUrl}/comments`;

  constructor(private http: HttpClient) {}

  getPostComments(postId: string, page = 1, pageSize = 10): Observable<ApiResponse<PagedResult<CommentDto>>> {
    return this.http.get<ApiResponse<PagedResult<CommentDto>>>(`${this.base}/post/${postId}`, {
      params: new HttpParams().set('page', page).set('pageSize', pageSize)
    });
  }

  getReplies(commentId: string, page = 1): Observable<ApiResponse<PagedResult<CommentDto>>> {
    return this.http.get<ApiResponse<PagedResult<CommentDto>>>(`${this.base}/${commentId}/replies`, {
      params: new HttpParams().set('page', page).set('pageSize', '5')
    });
  }

  addComment(dto: CreateCommentDto): Observable<ApiResponse<CommentDto>> {
    return this.http.post<ApiResponse<CommentDto>>(this.base, dto);
  }

  updateComment(id: string, dto: UpdateCommentDto): Observable<ApiResponse<CommentDto>> {
    return this.http.put<ApiResponse<CommentDto>>(`${this.base}/${id}`, dto);
  }

  deleteComment(id: string): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.base}/${id}`);
  }

  likeComment(id: string): Observable<ApiResponse<object>> {
    return this.http.post<ApiResponse<object>>(`${this.base}/${id}/like`, {});
  }

  unlikeComment(id: string): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.base}/${id}/like`);
  }
}
