import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { GlobalSearchResult } from '../models/search.model';

@Injectable({ providedIn: 'root' })
export class SearchService {
  private base = `${environment.apiUrl}/search`;

  constructor(private http: HttpClient) {}

  globalSearch(query: string): Observable<ApiResponse<GlobalSearchResult>> {
    return this.http.get<ApiResponse<GlobalSearchResult>>(this.base, {
      params: new HttpParams().set('q', query)
    });
  }
}
