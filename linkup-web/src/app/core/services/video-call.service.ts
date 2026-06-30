import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../models/api-response.model';
import { CallHistory } from '../models/call.model';

@Injectable({ providedIn: 'root' })
export class VideoCallService {
  private base = `${environment.apiUrl}/video-calls`;

  constructor(private http: HttpClient) {}

  getHistory(page = 1, pageSize = 30): Observable<ApiResponse<PagedResult<CallHistory>>> {
    return this.http.get<ApiResponse<PagedResult<CallHistory>>>(`${this.base}/history`, {
      params: new HttpParams().set('pageNumber', page).set('pageSize', pageSize)
    });
  }

  getActive(): Observable<ApiResponse<CallHistory | null>> {
    return this.http.get<ApiResponse<CallHistory | null>>(`${this.base}/active`);
  }
}
