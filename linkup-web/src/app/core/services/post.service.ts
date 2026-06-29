import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../models/api-response.model';
import { PostDto, CreatePostDto, UpdatePostDto, SharePostDto } from '../models/post.model';

@Injectable({ providedIn: 'root' })
export class PostService {
  private base = `${environment.apiUrl}/posts`;

  constructor(private http: HttpClient) {}

  getFeed(page = 1, pageSize = 10): Observable<ApiResponse<PagedResult<PostDto>>> {
    return this.http.get<ApiResponse<PagedResult<PostDto>>>(`${this.base}/feed`, {
      params: new HttpParams().set('page', page).set('pageSize', pageSize)
    });
  }

  getWallPosts(userId: string, page = 1, pageSize = 10): Observable<ApiResponse<PagedResult<PostDto>>> {
    return this.http.get<ApiResponse<PagedResult<PostDto>>>(`${this.base}/wall/${userId}`, {
      params: new HttpParams().set('page', page).set('pageSize', pageSize)
    });
  }

  getById(id: string): Observable<ApiResponse<PostDto>> {
    return this.http.get<ApiResponse<PostDto>>(`${this.base}/${id}`);
  }

  create(dto: CreatePostDto): Observable<ApiResponse<PostDto>> {
    return this.http.post<ApiResponse<PostDto>>(this.base, dto);
  }

  update(id: string, dto: UpdatePostDto): Observable<ApiResponse<PostDto>> {
    return this.http.put<ApiResponse<PostDto>>(`${this.base}/${id}`, dto);
  }

  delete(id: string): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.base}/${id}`);
  }

  pin(id: string, pin: boolean): Observable<ApiResponse<object>> {
    return this.http.patch<ApiResponse<object>>(`${this.base}/${id}/pin`, {}, {
      params: new HttpParams().set('pin', pin)
    });
  }

  share(dto: SharePostDto): Observable<ApiResponse<PostDto>> {
    return this.http.post<ApiResponse<PostDto>>(`${this.base}/${dto.originalPostId}/share`, dto);
  }
}
