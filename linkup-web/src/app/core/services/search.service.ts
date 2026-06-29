import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';

@Injectable({ providedIn: 'root' })
export class SearchService {
  private base = `${environment.apiUrl}/search`;

  constructor(private http: HttpClient) {}

  globalSearch(query: string): Observable<ApiResponse<{ users: unknown[]; posts: unknown[]; groups: unknown[] }>> {
    return this.http.get<ApiResponse<{ users: unknown[]; posts: unknown[]; groups: unknown[] }>>(this.base, {
      params: new HttpParams().set('q', query)
    });
  }

  searchUsers(query: string, page = 1, pageSize = 20): Observable<ApiResponse<unknown>> {
    return this.http.get<ApiResponse<unknown>>(`${this.base}/users`, {
      params: new HttpParams().set('q', query).set('page', page).set('pageSize', pageSize)
    });
  }
}
