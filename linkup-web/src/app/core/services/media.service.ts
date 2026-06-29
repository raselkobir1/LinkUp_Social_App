import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';

export interface MediaUploadResult {
  id: string;
  url: string;
  thumbnailUrl?: string;
  publicId: string;
  fileType: string;
  format: string;
  sizeInBytes: number;
  width?: number;
  height?: number;
  duration?: number;
}

@Injectable({ providedIn: 'root' })
export class MediaService {
  private base = `${environment.apiUrl}/media`;

  constructor(private http: HttpClient) {}

  uploadImage(file: File): Observable<ApiResponse<MediaUploadResult>> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<ApiResponse<MediaUploadResult>>(`${this.base}/upload/image`, form);
  }

  uploadVideo(file: File): Observable<ApiResponse<MediaUploadResult>> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<ApiResponse<MediaUploadResult>>(`${this.base}/upload/video`, form);
  }

  delete(publicId: string): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.base}/${publicId}`);
  }
}
