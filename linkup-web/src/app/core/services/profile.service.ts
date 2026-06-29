import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import {
  UserProfileDto, UpdateProfileDto,
  EducationDto, ExperienceDto, SocialLinkDto, PrivacySettingsDto
} from '../models/profile.model';

@Injectable({ providedIn: 'root' })
export class ProfileService {
  private base = `${environment.apiUrl}/profile`;

  constructor(private http: HttpClient) {}

  getProfile(userId: string): Observable<ApiResponse<UserProfileDto>> {
    return this.http.get<ApiResponse<UserProfileDto>>(`${this.base}/${userId}`);
  }

  updateProfile(dto: UpdateProfileDto): Observable<ApiResponse<UserProfileDto>> {
    return this.http.put<ApiResponse<UserProfileDto>>(this.base, dto);
  }

  uploadProfilePicture(file: File): Observable<ApiResponse<{ url: string }>> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<ApiResponse<{ url: string }>>(`${this.base}/me/profile-picture`, form);
  }

  uploadCoverPhoto(file: File): Observable<ApiResponse<{ url: string }>> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<ApiResponse<{ url: string }>>(`${this.base}/me/cover-photo`, form);
  }

  addEducation(dto: EducationDto): Observable<ApiResponse<EducationDto>> {
    return this.http.post<ApiResponse<EducationDto>>(`${this.base}/education`, dto);
  }

  updateEducation(id: string, dto: EducationDto): Observable<ApiResponse<EducationDto>> {
    return this.http.put<ApiResponse<EducationDto>>(`${this.base}/education/${id}`, dto);
  }

  deleteEducation(id: string): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.base}/education/${id}`);
  }

  addExperience(dto: ExperienceDto): Observable<ApiResponse<ExperienceDto>> {
    return this.http.post<ApiResponse<ExperienceDto>>(`${this.base}/experience`, dto);
  }

  updateExperience(id: string, dto: ExperienceDto): Observable<ApiResponse<ExperienceDto>> {
    return this.http.put<ApiResponse<ExperienceDto>>(`${this.base}/experience/${id}`, dto);
  }

  deleteExperience(id: string): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.base}/experience/${id}`);
  }

  setSocialLinks(links: SocialLinkDto[]): Observable<ApiResponse<SocialLinkDto[]>> {
    return this.http.put<ApiResponse<SocialLinkDto[]>>(`${this.base}/social-links`, links);
  }

  getPrivacySettings(): Observable<ApiResponse<PrivacySettingsDto>> {
    return this.http.get<ApiResponse<PrivacySettingsDto>>(`${this.base}/privacy`);
  }

  updatePrivacySettings(dto: PrivacySettingsDto): Observable<ApiResponse<PrivacySettingsDto>> {
    return this.http.put<ApiResponse<PrivacySettingsDto>>(`${this.base}/privacy`, dto);
  }
}
